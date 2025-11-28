namespace JingyuEIP.Models;

#region 外掛系統 DB資料表對應

public class Base
{
    public DateTime? CreationTime { get; set; } = DateTime.Now; // 創建時間
    public DateTime? UpdateTime { get; set; } = DateTime.Now; // 最後更新時間
    public int CreateUserId { get; set; } // 創建人id
    public string? CreateUserName { get; set; } // 創建人
    public int UpdateUserId { get; set; } // 最後更新人id
    public string? UpdateUserName { get; set; } // 最後更新人
    public string? CreationPerson { get; set; } // 創建人id
    public string? CreationPersonName { get; set; } // 創建人
    public string? UpdatePerson { get; set; } // 最後更新人id
    public string? UpdatePersonName { get; set; } // 最後更新人
}

/// <summary>
/// 下拉選單項目，託運單用
/// </summary>
public class SelectListItemDto
{
    public required string Label { get; set; }
    public required string Value { get; set; }
}

public class LogisticsProviderOptions
{
    public int OptionId { get; set; }
    public string LogisticsProviderCode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string OptionCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

#endregion

public static class ServiceType
{
    public const string EcpayInvoice = "01"; // 綠界發票
    public const string TcatLogistics = "11";     // 黑貓物流
    public const string HctLogistics = "12"; // 新竹物流
    public const string ConvenienceStore = "21"; // 超商地圖
}

#region 系統資料表對應

public class CompanyServiceParam
{
    public Guid Id { get; set; }                             // 主鍵
    public Guid IntegrationId { get; set; }                  // FK → CompanyServiceIntegration
    public string ParamKey { get; set; } = null!;            // 鍵
    public string ParamValue { get; set; } = null!;          // 值
    public string? Memo { get; set; }                        // 備註
    public DateTime CreationTime { get; set; }               // 建立時間
    public DateTime UpdateTime { get; set; }                 // 更新時間
}

public class CompanyServiceIntegrationDto : CompanyServiceIntegration
{
    public List<CompanyServiceParam> Params { get; set; } = new();
}

public class CompanyServiceIntegration
{
    public Guid Id { get; set; }                             // 主鍵
    public Guid CompanyId { get; set; }                      // FK → Company
    public string ServiceType { get; set; } = null!;         // 平台代碼
    public string EndpointName { get; set; } = null!;        // 功能點
    public string EnvType { get; set; } = null!;             // 環境別
    public string? ApiBaseUrl { get; set; }                  // API base URL
    public bool IsActive { get; set; }                       // 是否啟用
    public DateTime CreationTime { get; set; }               // 建立時間
    public DateTime UpdateTime { get; set; }                 // 更新時間
}

public class Company
{
    public Guid CompanyId { get; set; }                     // 主鍵
    public string DeptId { get; set; } = null!;             // 對應 ERP basDept
    public string TaxId { get; set; } = null!;              // 統一編號
    public string CompanyName { get; set; } = null!;        // 公司名稱
    public bool IsActive { get; set; }                      // 是否啟用
    public DateTime CreationTime { get; set; }              // 建立時間
    public DateTime UpdateTime { get; set; }                // 更新時間
}

public class T8UserDto : Users
{
    public string PersonId { get; set; } = string.Empty;
    public string PersonName { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? CaptchaId { get; set; }
    public string? CaptchaAnswer { get; set; }
}

/// <summary>
/// 使用者資料，對應 DB Table: Users
/// </summary>
public class Users
{
    public required int UserId { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; } = string.Empty;
    public required string UserDescription { get; set; } = string.Empty;
    public int DepartmentId { get; set; } = 0; //所屬部門
    public string UserEmail { get; set; } = string.Empty;
    public string LineUserId { get; set; } = string.Empty;
    public DateTime CreationTime { get; set; }
    public DateTime UpdateTime { get; set; }
    public string? Token { get; set; } = string.Empty;  // 前端用登入 Token，非DB欄位
}

public class UsersDTO : Users
{
    public List<int> Groups { get; set; } = new List<int>();
    public List<int> Roles { get; set; } = new List<int>();
}

// 給前端 pinia localStorage 的使用者資料
public class UserSessionData
{
    public required int UserId { get; set; }
    public required string Username { get; set; }
    public required string UserDescription { get; set; } = string.Empty;
    public int DepartmentId { get; set; } = 0;
    public string UserEmail { get; set; } = string.Empty;
    public string LineUserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public List<Permissions> Permissions { get; set; } = [];
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public string PersonId { get; set; } = string.Empty;
    public string PersonName { get; set; } = string.Empty;
}

public class UserChangePasswordDTO
{
    public required string NewPassword { get; set; } = string.Empty;
    public required string ConfirmPassword { get; set; } = string.Empty;
}

public class UserClaims
{
    public required int UserId { get; set; }
    public required string Username { get; set; }
    public string UserDescription { get; set; } = string.Empty;
    public List<Permissions> Permission { get; set; } = new List<Permissions>();
    public string PersonId { get; set; } = string.Empty;
    public string PersonName { get; set; } = string.Empty;
}

public class Parameter
{
    public required Guid ParameterId { get; set; }
    public required string Category { get; set; }
    public required string Code { get; set; }
    public required string Description { get; set; }
    public string Memo { get; set; } = string.Empty;
    public required bool IsSystemReserved { get; set; } = true; // 是否為系統內保留除權限允許不得修改刪除
}

public class Departments
{
    public required int DepartmentId { get; set; }
    public required string DepartmentName { get; set; } = string.Empty; // 部門代號
    public required string DepartmentDescription { get; set; } = string.Empty; // 部門名(描述)
    public required int ParentDepartmentId { get; set; } // 上級部門ID（可為NULL）
    public string? ParentDepartmentName { get; set; } // 上級部門名-資料庫沒有僅為前端顯示用
    public required string DepartmentLevel { get; set; } = string.Empty; // 部門層級（01-處，02-部，03-組...）
    public required DateTime CreationTime { get; set; } = DateTime.Now;
    public required DateTime UpdateTime { get; set; } = DateTime.Now;
}

public class DepartmentsDTO : Departments
{
    public List<int> Managers { get; set; } = new List<int>();
}

public class DepartmentManagers
{
    public required int DepartmentId { get; set; }
    public required int UserId { get; set; }
    public required int ManagerOrder { get; set; } // 順序，1 = 正主管，2以上為副主管
}

public class Notifications
{
    public required Guid NotificationId { get; set; } = Guid.Empty; // 通知ID
    public int? UserId { get; set; }  // 訊息接收者ID (若為群發，為 NULL)
    public required string NotificationMessageContent { get; set; } = string.Empty;  // 訊息內容
    public string? NotificationErrorMessageContent { get; set; }  // 錯誤訊息內容，並列顯示，只是加上顏色區隔
    public required int NotificationPriority { get; set; }  // 訊息優先級
    public required string NotificationType { get; set; } = string.Empty; // 訊息類型 (例如: '01' 系統通知)
    public string? NotificationLink { get; set; }  // 訊息連結
    public bool IsLinkNewWindow { get; set; }  // 連結是否開新窗
    public bool IsInternalLink { get; set; }  // 是否為內部連結(使否使用路由)
    public bool IsBlob { get; set; }  // 是否為二進位(要下載)
    public required int ProductionPercentage { get; set; } = 100; // 訊息生成中的百分比
    public DateTime? ReadTime { get; set; }  // 讀取時間 (NULL 代表未讀)        
    public required DateTime CreationTime { get; set; } = DateTime.Now;  // 訊息建立時間
    public DateTime? NotificationExpiresTime { get; set; }  // 訊息過期時間
}

public class UserRefreshToken
{
    public Guid TokenId { get; set; }
    public int UserId { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime CreationTime { get; set; }
    public DateTime ExpiryTime { get; set; }
    public bool IsRevoked { get; set; }
    public string? RevokedReason { get; set; }
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }
}

public class RefreshTokenRequest
{
    public required string RefreshToken { get; set; }
}

/// <summary>
/// 系統日誌，對應 DB Table: Logs
/// </summary>
public class Logs
{
    public required Guid Id { get; set; }  // 主鍵，預設為 NEWID()
    public string? LogType { get; set; } = string.Empty;  // 日誌類型 (例如：info, warning, error)
    public string? ActionType { get; set; } = string.Empty;  // 操作類型 (例如：create, read, update, delete)
    public string? ModuleName { get; set; } = string.Empty;  // Log的模組名稱 (例如：Sale, Customer, ...)
    public required string LogData { get; set; } = string.Empty;  // 用來儲存 JSON 資料的欄位
    public required DateTime Timestamp { get; set; } = DateTime.Now;  // 日誌時間戳，預設為當前時間
    public required string Username { get; set; }  // 執行操作的使用者 ID (如果需要的話)
}
#endregion