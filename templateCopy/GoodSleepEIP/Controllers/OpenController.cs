using Dapper;
using GoodSleepEIP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;

namespace GoodSleepEIP.Controllers
{
    [AllowAnonymous]
    [Route("api/open")]
    [ApiController]
    public class OpenController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private IDapperHelper dapper;
        private readonly TokenService tokenService;
        private readonly SequenceService sequenceService;
        private readonly TaskService taskService;
        static string? AG_GRID_LICENSE_KEY = null;

        public OpenController(IConfiguration _config, TokenService _tokenService, IDapperHelper _dapper, SequenceService _sequenceService, TaskService _taskService)
        {
            configuration = _config;
            dapper = _dapper;
            tokenService = _tokenService;
            sequenceService = _sequenceService;
            taskService = _taskService;
        }

        // AG-Grid 優化步驟：
        // 1. 註冊碼格式: [v2|3][TRIAL|RELEASE][0102]_{RELEASE_INFORMATION-ReleaseDateTimeStampEncodeByBase64}{MD5_HASH}
        // 2. 在打包後的程式碼中搜尋：_LicenseManager.RELEASE_INFORMATION，比如: node_modules\.vite\deps\ag-grid-enterprise.js 中的 _LicenseManager.RELEASE_INFORMATION = "MTczNjc2MzczNzA3Mg==";
        //    (上述的 "MTczNjc2MzczNzA3Mg==" 其實 base64 解碼後就是 "1736763737072" 也就是 UnixTimeStamp: 2025-01-13 07:08:57 UTC，為他定義的ReleaseDate)
        // 3. 承上述兩點，將 '[v2|3][TRIAL|RELEASE][0102]_{RELEASE_INFORMATION}” 替換成 “[v3][Release][0102]_MTczNjc2MzczNzA3Mg=='
        // 4. 將'[v3][Release][0102]_MTczNjc2MzczNzA3Mg=='計算MD5 Hash，得到 '99e33de9f721f5a0f2cdc2e7b4a17abb'
        // 5. 用第1點的方法拼接，得到: '[v3][Release][0102]_MTczNjc2MzczNzA3Mg==99e33de9f721f5a0f2cdc2e7b4a17abb'，即為註冊碼。
        [HttpGet("GetAgl")]
        public IActionResult GetAgl()
        {
            try
            {
                if (!string.IsNullOrEmpty(AG_GRID_LICENSE_KEY)) return new JsonResult(new { AG_GRID_LICENSE_KEY });

                // AG-Grid License Key 計算
                string wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets");
                if (Directory.Exists(wwwRootPath))
                {
                    var jsFiles = Directory.GetFiles(wwwRootPath, "index-*.js");
                    string? aggridReleaseInformation = null;
                    foreach (var jsFile in jsFiles)
                    {
                        var content = System.IO.File.ReadAllText(jsFile);
                        var match = Regex.Match(content, @"\.RELEASE_INFORMATION\s*=\s*""([^""]+)""");
                        if (match.Success)
                        {
                            aggridReleaseInformation = match.Groups[1].Value;
                            break;  // 找到就跳出迴圈
                        }
                    }
                    if (!string.IsNullOrEmpty(aggridReleaseInformation))
                    {
                        string licenseKey = $"[v3][Release][0102]_{aggridReleaseInformation}";
                        licenseKey += BitConverter.ToString(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(licenseKey))).Replace("-", "").ToLower();
                        AG_GRID_LICENSE_KEY = licenseKey;

                        return new JsonResult(new { AG_GRID_LICENSE_KEY });
                    }
                }

                return ResponseMsg.Ok(false, "Calculator for key error.");
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"錯誤: {ex.Message}");
            }
        }
        
    }
}