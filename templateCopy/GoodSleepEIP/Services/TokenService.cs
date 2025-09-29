using Dapper;
using GoodSleepEIP.Models;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GoodSleepEIP
{
    public class TokenService
    {
        private readonly IDapperHelper dapper;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IConfiguration configuration;
        private readonly PermissionService permissionService;

        public TokenService(IDapperHelper _dapper, IHttpContextAccessor _httpContextAccessor, IConfiguration _config, PermissionService _permissionService)
        {
            dapper = _dapper;
            httpContextAccessor = _httpContextAccessor;
            configuration = _config;
            permissionService = _permissionService;
        }

        private string GetDeviceInfo()
        {
            var context = httpContextAccessor.HttpContext;
            if (context == null) return "Unknown";

            var userAgent = context.Request.Headers["User-Agent"].ToString();
            return string.IsNullOrEmpty(userAgent) ? "Unknown" : userAgent;
        }

        private string GetClientIpAddress()
        {
            var context = httpContextAccessor.HttpContext;
            if (context == null) return "Unknown";

            var ip = context.Connection.RemoteIpAddress?.ToString();
            return string.IsNullOrEmpty(ip) ? "Unknown" : ip;
        }

        public string BuildToken(string key, string issuer, string audience, double tokenLifeMinutes, T8UserDto user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim("UserId", user.UserId.ToString()),
                new Claim("Username", user.Username),
                new Claim("UserDescription", user.UserDescription),
                new Claim("PersonId", user.PersonId ?? ""),
                new Claim("PersonName", user.PersonName ?? ""),
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(tokenLifeMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public UserClaims GetUserClaims()
        {
            var user = httpContextAccessor.HttpContext?.User;

            if (user == null || (user.Identity == null || !user.Identity.IsAuthenticated))
            {
                throw new UnauthorizedAccessException("使用者未經過驗證");
            }

            var userIdClaim = user.FindFirst("UserId")?.Value;
            var usernameClaim = user.FindFirst("Username")?.Value;
            var userDescriptionClaim = user.FindFirst("UserDescription")?.Value;
            var permissionsClaim = user.FindFirst("Permissions")?.Value;
            var personIdClaim = user.FindFirst("PersonId")?.Value;
            var personNameClaim = user.FindFirst("PersonName")?.Value;


            if (string.IsNullOrWhiteSpace(userIdClaim) || string.IsNullOrWhiteSpace(usernameClaim))
            {
                throw new InvalidOperationException("Token 中缺少必要的使用者資訊");
            }

            List<Permissions> permissions = new List<Permissions>();
            if (!string.IsNullOrWhiteSpace(permissionsClaim))
            {
                permissions = JsonConvert.DeserializeObject<List<Permissions>>(permissionsClaim) ?? new List<Permissions>();
            }

            return new UserClaims
            {
                UserId = int.Parse(userIdClaim),
                Username = usernameClaim,
                UserDescription = userDescriptionClaim ?? "",
                Permission = permissions,
                PersonId = personIdClaim ?? "",
                PersonName = personNameClaim ?? ""
            };
        }

        /// <summary>
        /// 本方法備用的，大多情況沒用，
        /// 如果使用 [Authorize] 之類的修飾子，ValidateToken 是多餘的，
        /// 因為中間件已經保證進入控制器的方法時 HttpContext.User 是可信的
        /// 只有:
        /// 1. 某些 API 不使用 [Authorize]，需要手動驗證 Token
        /// 2. 允許多來源的 Token，但需要根據不同的 Key 和 Issuer 驗證
        /// </summary>
        public (bool IsValid, ClaimsPrincipal? Principal, string? ErrorMessage) ValidateToken(string key, string issuer, string audience, string token)
        {
            var mySecret = Encoding.UTF8.GetBytes(key);
            var mySecurityKey = new SymmetricSecurityKey(mySecret);

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = mySecurityKey,
                }, out SecurityToken validatedToken);

                return (true, principal, null);
            }
            catch (SecurityTokenExpiredException)
            {
                return (false, null, "Token has expired");
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                return (false, null, "Invalid token signature");
            }
            catch (Exception ex)
            {
                return (false, null, $"Token validation failed: {ex.Message}");
            }
        }

        // 以使用者帳號密碼來取使用者資料，如果取不到代表驗證失敗: 主登入管制
        public async Task<UserSessionData> GetUser(string Username, string Password)
        {
            #if DEBUG // !!! 重要：確保此邏輯僅在 DEBUG 模式下編譯 !!!
            // 1. 首先，嘗試直接從本地 Users 表中獲取使用者
            string initialLocalUserSql = $@"SELECT * FROM {DBName.Main}.Users usr WHERE usr.Username = @Username AND usr.IsDeleted = 0";
            var localUserResults = (List<T8UserDto>)dapper.Query<T8UserDto>(initialLocalUserSql, new { Username });
            if (localUserResults.Count > 0)
            {
                var potentialDevUser = localUserResults[0];
                if (potentialDevUser.UserId == 1)
                {
                    // 直接對 UserId = 1 的本地帳號進行密碼驗證
                    if (BCrypt.Net.BCrypt.Verify(Password, potentialDevUser.Password))
                    {
                        potentialDevUser.PersonId = "0001";
                        potentialDevUser.PersonName = "開發用帳號";
                        System.Diagnostics.Debug.WriteLine($"警告：偵錯模式下，UserId = 1 (帳號名: '{Username}') 密碼驗證成功。已跳過ERP整合。此功能絕對不能部署到生產環境！");
                        return await GetLoginData(potentialDevUser); // 成功則返回，跳過後續所有邏輯
                    }
                    else
                    {
                        // 如果是 UserId = 1 但密碼錯誤，則拋出特定錯誤，不應繼續執行 ERP 邏輯
                        throw new Exception($"開發用帳號 (UserId 1 / Username '{Username}') 密碼錯誤。ERP 邏輯已跳過。");
                    }
                }
            }
            #endif // !!! DEBUG 區塊結束 !!!
            
            // T8 帳號整合：登入時檢查系統是否已經有該帳號，如果有則直接登入，如果沒有則建立新帳號
            string sqlstr = $@"SELECT u.Username, p.PersonId, p.PersonName
                                FROM {DBName.ERP}.comPerson AS p
                                LEFT JOIN {DBName.Main}.Users AS u ON p.PersonId = u.Username
                                WHERE p.PersonId = @PersonId AND p.ContractState = '0' 
                                AND CONVERT(INT, FORMAT(GETDATE(), 'yyyyMMdd')) >= p.ValidityFromDate 
                                AND CONVERT(INT, FORMAT(GETDATE(), 'yyyyMMdd')) <= p.ValidityToDate ";
            var syncResult = dapper.Query<T8UserDto>(sqlstr, new { PersonId = Username }).ToList();
            if (syncResult.Count > 0)
            {
                var user = syncResult[0];
                if (string.IsNullOrEmpty(user.Username) && user.PersonId.Length > 2)  // Username 為 null或空字串 代表 Users table 裡面沒有這個帳號
                {
                    dapper.Execute($@"INSERT INTO {DBName.Main}.Users (Username, Password, UserDescription, DepartmentId, UserEmail, LineUserId, CreationTime, UpdateTime)
                    VALUES (@Username, @Password, @UserDescription, @DepartmentId, @UserEmail, @LineUserId, GETDATE(), GETDATE());", new 
                    {
                        Username = user.PersonId,
                        Password = BCrypt.Net.BCrypt.HashPassword(user.PersonId),
                        UserDescription = user.PersonName,
                        DepartmentId = 1,   // 預設值要注意，不然可能會有錯誤
                        UserEmail = string.Empty,
                        LineUserId = string.Empty
                    });
                }
            }

            sqlstr = $@"SELECT 
                            usr.* ,
                            p.PersonId,
                            p.PersonName,
                            p.ContractState
                        FROM {DBName.Main}.Users usr 
                        LEFT JOIN {DBName.ERP}.comPerson AS p ON usr.Username = p.PersonId
                        WHERE usr.Username = @Username AND usr.IsDeleted = 0
                        AND p.ContractState = '0' 
                        AND CONVERT(INT, FORMAT(GETDATE(), 'yyyyMMdd')) >= p.ValidityFromDate 
                        AND CONVERT(INT, FORMAT(GETDATE(), 'yyyyMMdd')) <= p.ValidityToDate";

            var qresult = (List<T8UserDto>)dapper.Query<T8UserDto>(sqlstr, new { Username });
            if (qresult.Count() >= 1 && BCrypt.Net.BCrypt.Verify(Password, qresult[0].Password)) return await GetLoginData(qresult[0]);
            else throw new Exception("找不到有效的使用者或密碼錯誤");
        }
        
        public async Task<UserSessionData> GetUserByLineId(string lineUserId)
        {
            string sqlstr = @$"SELECT * FROM {DBName.Main}.Users WHERE LineUserId = @LineUserId";
            var qresult = (List<T8UserDto>)dapper.Query<T8UserDto>(sqlstr, new { LineUserId = lineUserId });
            if (qresult.Count() >= 1) return await GetLoginData(qresult[0]);
            else throw new Exception("您還沒有綁定 LINE 帳號喔");
        }

        private async Task<UserSessionData> GetLoginData(T8UserDto userData)
        {
            if (!(userData.UserId > 0)) throw new Exception("帳號資料不完整");

            // 建立 Token
            string generatedToken = BuildToken(
                configuration["Jwt:Key"]?.ToString() ?? "",
                configuration["Jwt:Issuer"]?.ToString() ?? "",
                configuration["Jwt:Audience"]?.ToString() ?? "",
                Convert.ToDouble(configuration["Jwt:TokenLifeMinutes"]),
                userData
            );

            if (string.IsNullOrEmpty(generatedToken)) throw new Exception("Generating token error");

            // 生成 Refresh Token
            string refreshToken = Guid.NewGuid().ToString("N");
            var savedRefreshToken = await SaveRefreshTokenAsync(userData.UserId, refreshToken);

            // 從資料庫取得使用者的權限
            var permissions = permissionService.GetPermissionsFromDatabase(userData.UserId);

            var userDTO = new UserSessionData
            {
                UserId = userData.UserId,
                Username = userData.Username,
                UserDescription = userData.UserDescription,
                DepartmentId = userData.DepartmentId,
                UserEmail = userData.UserEmail,
                LineUserId = userData.LineUserId,
                Token = generatedToken,
                RefreshToken = refreshToken,
                RefreshTokenExpiry = savedRefreshToken.ExpiryTime,
                Permissions = permissions,
                PersonId = userData.PersonId,
                PersonName = userData.PersonName
            };

            return userDTO; // 成功回傳
        }

        private async Task<UserRefreshToken> SaveRefreshTokenAsync(int userId, string refreshToken)
        {
            try
            {
                // 檢查使用者的有效 token 數量
                var maxActiveTokens = Convert.ToInt32(configuration["Jwt:MaxActiveRefreshTokens"] ?? "5");
                var activeTokenCount = (await dapper.QueryAsync<int>(
                    $@"SELECT COUNT(*) 
                       FROM {DBName.Main}.UserRefreshTokens 
                       WHERE UserId = @UserId 
                       AND IsRevoked = 0 
                       AND ExpiryTime > @Now",
                    new { UserId = userId, Now = DateTime.Now })).FirstOrDefault();

                // 如果超過限制，撤銷最舊的 token
                if (activeTokenCount >= maxActiveTokens)
                {
                    await dapper.ExecuteAsync(
                        $@"UPDATE {DBName.Main}.UserRefreshTokens 
                           SET IsRevoked = 1, 
                               RevokedReason = 'Exceeded maximum allowed tokens'
                           WHERE TokenId = (
                               SELECT TOP 1 TokenId
                               FROM {DBName.Main}.UserRefreshTokens
                               WHERE UserId = @UserId
                               AND IsRevoked = 0
                               AND ExpiryTime > @Now
                               ORDER BY CreationTime ASC
                           )",
                        new { UserId = userId, Now = DateTime.Now });
                }

                var token = new UserRefreshToken
                {
                    TokenId = Guid.NewGuid(),
                    UserId = userId,
                    RefreshToken = refreshToken,
                    CreationTime = DateTime.Now,
                    ExpiryTime = DateTime.Now.AddDays(Convert.ToInt32(configuration["Jwt:RefreshTokenExpiryDays"] ?? "30")),
                    IsRevoked = false,
                    DeviceInfo = GetDeviceInfo(),
                    IpAddress = GetClientIpAddress()
                };

                await dapper.ExecuteAsync($@"
                    INSERT INTO {DBName.Main}.UserRefreshTokens 
                    (TokenId, UserId, RefreshToken, CreationTime, ExpiryTime, IsRevoked, DeviceInfo, IpAddress)
                    VALUES 
                    (@TokenId, @UserId, @RefreshToken, @CreationTime, @ExpiryTime, @IsRevoked, @DeviceInfo, @IpAddress)",
                    token);

                return token;
            }
            catch (Exception ex)
            {
                throw new Exception($"儲存 RefreshToken 時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 使用 Refresh Token 更新 Access Token
        /// </summary>
        public async Task<UserSessionData> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                // 從資料庫獲取 Refresh Token 記錄
                var tokenRecord = (await dapper.QueryAsync<UserRefreshToken>(
                    $@"SELECT * FROM {DBName.Main}.UserRefreshTokens 
                       WHERE RefreshToken = @RefreshToken 
                       AND IsRevoked = 0 
                       AND ExpiryTime > @Now",
                    new { RefreshToken = refreshToken, Now = DateTime.Now })).FirstOrDefault();

                if (tokenRecord == null)
                {
                    throw new Exception("無效或已過期的 Refresh Token");
                }

                // 獲取使用者資料
                var user = (await dapper.QueryAsync<T8UserDto>(
                    $@"SELECT 
                            usr.* ,
                            p.PersonId,
                            p.PersonName,
                            p.ContractState
                        FROM {DBName.Main}.Users usr 
                        LEFT JOIN {DBName.ERP}.comPerson AS p ON usr.Username = p.PersonId
                        WHERE usr.UserId = @UserId AND usr.IsDeleted = 0
                        AND p.ContractState = '0' 
                        AND CONVERT(INT, FORMAT(GETDATE(), 'yyyyMMdd')) >= p.ValidityFromDate 
                        AND CONVERT(INT, FORMAT(GETDATE(), 'yyyyMMdd')) <= p.ValidityToDate",
                    new { UserId = tokenRecord.UserId })).FirstOrDefault();

                if (user == null)
                {
                    throw new Exception("找不到對應的使用者");
                }

                // 生成新的 Access Token
                string newAccessToken = BuildToken(
                    configuration["Jwt:Key"]?.ToString() ?? "",
                    configuration["Jwt:Issuer"]?.ToString() ?? "",
                    configuration["Jwt:Audience"]?.ToString() ?? "",
                    Convert.ToDouble(configuration["Jwt:TokenLifeMinutes"]),
                    user
                );

                if (string.IsNullOrEmpty(newAccessToken))
                {
                    throw new Exception("生成 Access Token 失敗");
                }

                // 生成新的 Refresh Token
                string newRefreshToken = Guid.NewGuid().ToString("N");
                var newTokenRecord = await SaveRefreshTokenAsync(user.UserId, newRefreshToken);

                // 撤銷舊的 Refresh Token
                await RevokeRefreshToken(tokenRecord.TokenId, "Token refreshed");

                // 獲取使用者權限
                var permissions = permissionService.GetPermissionsFromDatabase(user.UserId);
                
                return new UserSessionData
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    UserDescription = user.UserDescription,
                    DepartmentId = user.DepartmentId,
                    UserEmail = user.UserEmail,
                    LineUserId = user.LineUserId,
                    Token = newAccessToken,
                    RefreshToken = newRefreshToken,
                    Permissions = permissions,
                    RefreshTokenExpiry = newTokenRecord.ExpiryTime
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"更新 Refresh Token 失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 撤銷指定的 Refresh Token
        /// </summary>
        public async Task RevokeRefreshToken(Guid tokenId, string reason = "Token revoked")
        {
            try
            {
                var result = await dapper.ExecuteAsync(
                    $@"UPDATE {DBName.Main}.UserRefreshTokens 
                       SET IsRevoked = 1, 
                           RevokedReason = @Reason
                       WHERE TokenId = @TokenId",
                    new { TokenId = tokenId, Reason = reason });

                if (result == 0)
                {
                    throw new Exception("找不到指定的 Refresh Token");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"撤銷 Refresh Token 失敗: {ex.Message}");
            }
        }

    }
}

