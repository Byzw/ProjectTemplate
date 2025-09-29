using System.Data;

namespace GoodSleepEIP.Models
{
    public class GroupPermissions
    {
        public required int GroupId { get; set; } // 群組 ID
        public required int PermissionId { get; set; } // 權限 ID
    }

    public class Groups
    {
        public required int GroupId { get; set; } // 主鍵
        public required string GroupName { get; set; } = string.Empty; // 群組名稱
        public string? GroupDescription { get; set; } // 群組描述，可為空
        public required string PermissionGroupType { get; set; } = string.Empty; // 群組類型 (如 "Department", "Team")
        public required bool IsAdmin { get; set; } = false; // 是否為系統管理群組
        public required bool IsSystemReserved { get; set; } = false; // 是否為系統內保留不可刪除
        public required DateTime CreationTime { get; set; } = DateTime.Now; // 建立時間，預設為現在
    }

    public class GroupsWithPermissionsList : Groups
    {
        public List<int> GroupPermissions { get; set; } = new();
    }

    public class Permissions
    {
        public required int PermissionId { get; set; } // 主鍵
        public required string ModuleName { get; set; } = string.Empty; // 模組權限名稱(非唯一標識)，使用者若無定義，則該模組完全無法路由(看到)
        public required string PermissionModuleSubType { get; set; } = string.Empty; // 模組內的次類別，比如有的只能讀寫、有的能讀刪；ModuleName+PermissionModuleSubType 為唯一標識(DB UNIQUE)
        public string? PermissionDescription { get; set; } // 權限描述，可為空
        public required bool IsPublic { get; set; } = false; // 是否所有使用者都加上這條權限
        public required bool CanRead { get; set; } = false; // 是否可讀取自己的資料
        public required bool CanReadAll { get; set; } = false; // 是否可讀取所有資料
        public required bool CanCreate { get; set; } = false; // 是否可新增資料
        public required bool CanUpdate { get; set; } = false; // 是否可修改資料
        public required bool CanDelete { get; set; } = false; // 是否可刪除資料
        public required bool CanManage { get; set; } = false; // 是否有管理權限
        public required bool IsSystemReserved { get; set; } = false; // 是否為系統內保留不可刪除
        public required DateTime CreationTime { get; set; } = DateTime.Now; // 建立時間，預設為現在
    }

    public class RolePermissions
    {
        public required int RoleId { get; set; } // 角色 ID
        public required int PermissionId { get; set; } // 權限 ID
    }

    public class Roles
    {
        public required int RoleId { get; set; } // 主鍵
        public required string RoleName { get; set; } = string.Empty; // 角色名稱
        public string? RoleDescription { get; set; } // 角色描述，可為空
        public required bool IsAdmin { get; set; } = false; // 是否為系統管理角色
        public required bool IsSystemReserved { get; set; } = false; // 是否為系統內保留不可刪除
        public required DateTime CreationTime { get; set; } = DateTime.Now; // 建立時間，預設為現在
    }

    public class RolesWithPermissionsList : Roles
    {
        public List<int> RolePermissions { get; set; } = new();
    }

    public class UserGroups
    {
        public required int UserId { get; set; } // 對應的使用者ID
        public required int GroupId { get; set; } // 對應的群組ID
    }

    public class UserRoles
    {
        public required int UserId { get; set; } // 對應使用者ID
        public required int RoleId { get; set; } // 對應角色ID
    }
}
