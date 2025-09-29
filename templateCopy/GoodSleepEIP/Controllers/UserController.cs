using GoodSleepEIP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Dapper;
using Microsoft.Extensions.Caching.Memory;

namespace GoodSleepEIP.Controllers
{
    [Route("api/web")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private IDapperHelper dapper;
        private readonly TokenService tokenService;
        private readonly IMemoryCache memoryCache;
        private readonly PermissionService permissionService;

        public AuthenticationController(IConfiguration _config, TokenService _tokenService, IDapperHelper _dapper, IMemoryCache _cache, PermissionService _permissionService)
        {
            configuration = _config;
            dapper = _dapper;
            tokenService = _tokenService;
            memoryCache = _cache;
            permissionService = _permissionService;
        }

        // 對外產生驗證碼介面
        [HttpGet("GenerateCaptcha")]
        public IActionResult GenerateCaptcha(bool isDarkTheme = false)
        {
            int width = 100, height = 36;
            var captchaCode = Captcha.GenerateCaptchaCode();
            var captchaImage = Captcha.GetImage(width, height, captchaCode, "Arial", isDarkTheme);

            // 建立一個唯一標識
            var captchaId = Guid.NewGuid().ToString();

            // 暫存驗證碼到記憶體或分散式快取中，保留 3 分鐘
            memoryCache.Set(captchaId, captchaCode, TimeSpan.FromMinutes(3));

            return ResponseMsg.Ok(true, "", new
            {
                CaptchaImage = $"data:image/png;base64,{Convert.ToBase64String(captchaImage.CaptchaByteData)}",
                CaptchaId = captchaId
            });
        }

        // 對外登入界面
        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginRequest loginRequest)
        {
            if (string.IsNullOrWhiteSpace(loginRequest.Username) || string.IsNullOrWhiteSpace(loginRequest.Password) || string.IsNullOrWhiteSpace(loginRequest.CaptchaId) || string.IsNullOrWhiteSpace(loginRequest.CaptchaAnswer))
            {
                return ResponseMsg.Ok(false, "Invalid login request data");
            }

            try
            {
                // 驗證驗證碼
                if (Convert.ToBoolean(configuration["Jwt:IsUseCaptcha"]))
                {
                    if (string.IsNullOrWhiteSpace(loginRequest.CaptchaId) || string.IsNullOrWhiteSpace(loginRequest.CaptchaAnswer))
                    {
                        return ResponseMsg.Ok(false, "Invalid captcha request data");
                    }

                    if (!memoryCache.TryGetValue(loginRequest.CaptchaId ?? "", out string? correctCaptcha) ||
                        !string.Equals(correctCaptcha, loginRequest.CaptchaAnswer, StringComparison.OrdinalIgnoreCase))
                    {
                        return ResponseMsg.Ok(false, "驗證碼錯誤");
                    }

                    // 驗證成功後，刪除快取中的驗證碼
                    if (loginRequest.CaptchaId != null) memoryCache.Remove(loginRequest.CaptchaId);
                }
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, "驗證錯誤: " + ex.Message);
            }
            try
            {
                // 驗證使用者、取得使用者資料(如果驗證成功)
                var userData = await tokenService.GetUser(loginRequest.Username, loginRequest.Password);
                return ResponseMsg.Ok(true, "Please use the token to access the API", userData); // 成功回傳
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, ex.Message);
            }
        }

        [Authorize]
        [HttpGet("GetUserIsDarkTheme")]
        public IActionResult GetUserIsDarkTheme(int UserId)
        {
            try
            {
                if (UserId <= 0) return ResponseMsg.Ok(false, "ID 不可為空值");
                bool IsDarkTheme = Convert.ToBoolean(dapper.QueryScalar($"SELECT IsDarkTheme FROM {DBName.Main}.Users WHERE UserId = @UserId", new { UserId }));
                return ResponseMsg.Ok(true, "User IsDarkTheme", new { IsDarkTheme });
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"GetUserIsDarkTheme 錯誤: {ex.Message}");
            }
        }

        [Authorize]
        [HttpGet("SetUserIsDarkTheme")]
        public IActionResult SetUserIsDarkTheme(int UserId, bool IsDarkTheme)
        {
            try
            {
                if (UserId <= 0) return ResponseMsg.Ok(false, "ID 不可為空值");
                int ex_count = dapper.Execute(@$"UPDATE {DBName.Main}.Users SET IsDarkTheme = @IsDarkTheme WHERE UserId = @UserId", new { UserId, IsDarkTheme });
                if (ex_count != 1) return ResponseMsg.Ok(false, "SetUserIsDarkTheme 檢查錯誤，請告知系統管理員");
                return ResponseMsg.Ok(true, "");
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"SetUserIsDarkTheme 錯誤: {ex.Message}");
            }
        }

        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var userSessionData = await tokenService.RefreshTokenAsync(request.RefreshToken);
                return ResponseMsg.Ok(true, "Token refreshed successfully", userSessionData);
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, ex.Message);
            }
        }

        [Authorize]
        [HttpPost("RevokeToken")]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var userClaims = tokenService.GetUserClaims();
                var tokenRecord = (await dapper.QueryAsync<UserRefreshToken>(
                    $@"SELECT * FROM {DBName.Main}.UserRefreshTokens 
                       WHERE RefreshToken = @RefreshToken 
                       AND IsRevoked = 0 
                       AND ExpiryTime > @Now",
                    new { RefreshToken = request.RefreshToken, Now = DateTime.UtcNow })).FirstOrDefault();

                if (tokenRecord == null)
                {
                    return ResponseMsg.Ok(false, "找不到指定的 Refresh Token");
                }

                await tokenService.RevokeRefreshToken(tokenRecord.TokenId, "User requested token revocation");
                return ResponseMsg.Ok(true, "Token 已成功撤銷");
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, ex.Message);
            }
        }
    }
}
