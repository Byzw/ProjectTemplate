using Dapper;
using GoodSleepEIP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using isRock.LineBot;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace GoodSleepEIP.Controllers
{
    [Authorize]
    [Route("api/line")]
    [ApiController]
    public class LineBotController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IDapperHelper _dapper;
        private readonly TokenService _tokenService;
        private readonly LineService _lineService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _memoryCache; // 從原始程式碼補上

        public LineBotController(IConfiguration config, TokenService tokenService, IDapperHelper dapper, IMemoryCache cache, LineService lineService, IHttpClientFactory httpClientFactory)
        {
            _configuration = config;
            _tokenService = tokenService;
            _dapper = dapper;
            _memoryCache = cache; // 從原始程式碼補上
            _lineService = lineService;
            _httpClientFactory = httpClientFactory;
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromHeader(Name = "X-Line-Signature")] string signature)
        {
            using var reader = new StreamReader(Request.Body);
            string requestBody = await reader.ReadToEndAsync();

            if (!_lineService.ValidateSignature(requestBody, signature))
            {
                return Unauthorized("Invalid signature.");
            }

            var receivedMessage = JsonConvert.DeserializeObject<ReceivedMessage>(requestBody);
            if (receivedMessage == null || receivedMessage.events == null)
            {
                return BadRequest();
            }

            foreach (var evt in receivedMessage.events)
            {
                _lineService.HandleEvent(evt);
            }

            return Ok();
        }

        /// <summary>
        /// 檢查 Line 帳號是否已綁定。
        /// </summary>
        /// <param name="LineIdToken">從前端 LIFF liff.getIDToken() 取得的 ID Token。</param>
        [AllowAnonymous]
        [HttpGet("CheckLineBind")]
        public async Task<IActionResult> CheckLineBind(string LineIdToken)
        {
            try
            {
                // 步驟 1: 驗證 ID Token 並取得可信的 LineUserId
                var lineProfile = await VerifyAndGetLineProfileAsync(LineIdToken);
                if (lineProfile == null)
                {
                    return ResponseMsg.Ok(false, "無效的 ID Token。");
                }
                var verifiedUserId = lineProfile.Sub;

                // 步驟 2: 使用經過驗證的 LineUserId 執行原本的業務邏輯
                var user = _lineService.GetUserDataOnlyByLineId(verifiedUserId);
                if (user == null)
                {
                    return ResponseMsg.Ok(false, "您還沒有綁定 LINE 帳號喔");
                }
                else
                {
                    return ResponseMsg.Ok(true, "LINE 帳號已經綁定過囉");
                }
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, "錯誤: " + ex.Message);
            }
        }

        /// <summary>
        /// LIFF 登入，驗證 ID Token 並回傳系統自訂的 JWT。
        /// </summary>
        /// <param name="LineIdToken">從前端 LIFF liff.getIDToken() 取得的 ID Token。</param>
        [AllowAnonymous]
        [HttpGet("LiffRouteLogin")]
        public async Task<IActionResult> LiffRouteLogin(string LineIdToken)
        {
            try
            {
                // 步驟 1: 驗證 ID Token 並取得可信的 LineUserId
                var lineProfile = await VerifyAndGetLineProfileAsync(LineIdToken);
                if (lineProfile == null)
                {
                    return ResponseMsg.Ok(false, "無效的 ID Token。");
                }
                var verifiedUserId = lineProfile.Sub;


                // 步驟 2: 使用經過驗證的 LineUserId 執行原本的業務邏輯
                var userData = await _tokenService.GetUserByLineId(verifiedUserId);
                return ResponseMsg.Ok(true, "Please use the token to access the API", userData);
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, ex.Message);
            }
        }

        /// <summary>
        /// 驗證使用者輸入的帳號密碼後，驗證 Line ID Token，若皆正確則綁定 LINE 使用者並登入。
        /// </summary>
        [AllowAnonymous]
        [HttpPost("LineBindLogin")]
        public async Task<IActionResult> LineBindLogin([FromBody] LineBindRequest loginRequest)
        {
            // Captcha 驗證邏輯
            if (Convert.ToBoolean(_configuration["Jwt:IsUseCaptcha"]))
            {
                if (string.IsNullOrWhiteSpace(loginRequest.CaptchaId) || string.IsNullOrWhiteSpace(loginRequest.CaptchaAnswer) ||
                    !_memoryCache.TryGetValue(loginRequest.CaptchaId, out string? correctCaptcha) ||
                    !string.Equals(correctCaptcha, loginRequest.CaptchaAnswer, StringComparison.OrdinalIgnoreCase))
                {
                    return ResponseMsg.Ok(false, "驗證碼錯誤");
                }
                _memoryCache.Remove(loginRequest.CaptchaId);
            }

            try
            {
                // 步驟 1: 驗證 ID Token
                var lineProfile = await VerifyAndGetLineProfileAsync(loginRequest.LineIdToken);
                if (lineProfile == null)
                {
                    return ResponseMsg.Ok(false, "無效的 ID Token，無法進行綁定。");
                }
                var verifiedUserId = lineProfile.Sub;

                // 步驟 2: 驗證使用者帳號密碼
                var userData = await _tokenService.GetUser(loginRequest.Username, loginRequest.Password);

                // 步驟 3: 使用經過驗證的 LineUserId 進行綁定
                if (!BindLineUser(userData.UserId, verifiedUserId))
                {
                    return ResponseMsg.Ok(false, "綁定 LINE 使用者失敗");
                }

                return ResponseMsg.Ok(true, "Please use the token to access the API", userData);
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, ex.Message);
            }
        }

        private bool BindLineUser(int UserId, string LineUserId)
        {
            try
            {
                string sql = $"UPDATE {DBName.Main}.Users SET LineUserId = @LineUserId WHERE UserId = @UserId";
                int affectedRows = _dapper.Execute(sql, new { UserId, LineUserId });
                return affectedRows == 1;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// [您提供的方法] 驗證 LINE ID Token 並回傳完整的個人資料
        /// </summary>
        /// <param name="LineIdToken">從 LIFF 取得的 ID Token</param>
        /// <returns>驗證成功則回傳 LineVerifyIdTokenResponse，失敗則回傳 null</returns>
        private async Task<LineVerifyIdTokenResponse?> VerifyAndGetLineProfileAsync(string LineIdToken)
        {
            if (string.IsNullOrWhiteSpace(LineIdToken)) return null;

            try
            {
                var channelId = _configuration["LineBot:LineLoginChannelId"];
                if (string.IsNullOrEmpty(channelId)) throw new InvalidOperationException("LINE Channel ID 未設定。");

                var client = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.line.me/oauth2/v2.1/verify")
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "id_token", LineIdToken },
                        { "client_id", channelId }
                    })
                };

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;

                var jsonString = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<LineVerifyIdTokenResponse>(jsonString);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}