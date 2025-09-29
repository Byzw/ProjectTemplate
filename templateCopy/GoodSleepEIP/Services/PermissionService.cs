using GoodSleepEIP.Models;
using Dapper;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Memory;

namespace GoodSleepEIP
{
    public class PermissionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration configuration;
        private readonly IDapperHelper dapper;
        private readonly IMemoryCache cache;
        private readonly TimeSpan cacheExpiration;

        public PermissionService(IHttpContextAccessor httpContextAccessor, IConfiguration _config, IDapperHelper _dapper, IMemoryCache _cache)
        {
            _httpContextAccessor = httpContextAccessor;
            configuration = _config;
            dapper = _dapper;
            cache = _cache;

            cacheExpiration = TimeSpan.FromMinutes(Convert.ToDouble(configuration["MemoryCache:CacheExpiration"] ?? "30"));
        }

        public bool HasActionPermission(string moduleName, string action)
        {
            // 從 Token 解出 UserId
            var userId = _httpContextAccessor.HttpContext?.User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId)) return false;

            var permissions = GetUserPermissions(int.Parse(userId));    // 取權限列表
            var matchedPermissions = permissions?.Where(p => p.ModuleName == moduleName).ToList();  // 取得所有符合 ModuleName 的權限記錄

            if (matchedPermissions == null || matchedPermissions.Count == 0) return false; // 如果找不到該模組的權限記錄，拒絕
            if (matchedPermissions.Any(p => p.CanManage)) return true;  // 如果 CanManage = true，直接通過，下面不用看了

            // 檢查 action 欄位的權限
            return matchedPermissions.Any(permission =>
            {
                var propertyInfo = typeof(Permissions).GetProperty(action);
                if (propertyInfo == null) return false; // 如果權限物件中不存在該屬性，直接拒絕

                var value = propertyInfo.GetValue(permission);
                return value is bool boolValue && boolValue;
            });
        }

        public List<Permissions> GetUserPermissions(int userId)
        {
            string cacheKey = $"UserPermissions:{userId}";
            if (cache.TryGetValue(cacheKey, out List<Permissions>? permissions)) return permissions ?? [];

            permissions = GetPermissionsFromDatabase(userId);
            cache.Set(cacheKey, permissions, cacheExpiration);
            return permissions;
        }

        public List<Permissions> GetPermissionsFromDatabase(int UserId)
        {
            string sql = $@"
                            -- 取得群組權限
                            SELECT DISTINCT p.*
                            FROM {DBName.Main}.GroupPermissions gp
                            INNER JOIN {DBName.Main}.Groups g ON gp.GroupId = g.GroupId AND g.IsDeleted = 0
                            INNER JOIN {DBName.Main}.Permissions p ON gp.PermissionId = p.PermissionId AND p.IsDeleted = 0
                            WHERE g.GroupId IN (
                                SELECT ug.GroupId
                                FROM {DBName.Main}.UserGroups ug
                                WHERE ug.UserId = @UserId
                            )
                            UNION
                            -- 取得角色權限
                            SELECT DISTINCT p.*
                            FROM {DBName.Main}.RolePermissions rp
                            INNER JOIN {DBName.Main}.Roles r ON rp.RoleId = r.RoleId AND r.IsDeleted = 0
                            INNER JOIN {DBName.Main}.Permissions p ON rp.PermissionId = p.PermissionId AND p.IsDeleted = 0
                            WHERE r.RoleId IN (
                                SELECT ur.RoleId
                                FROM {DBName.Main}.UserRoles ur
                                WHERE ur.UserId = @UserId
                            )
                            UNION
                            -- 取得所有公共權限
                            SELECT DISTINCT p.*
                            FROM {DBName.Main}.Permissions p
                            WHERE p.IsPublic = 1 AND p.IsDeleted = 0
                            UNION
                            -- 若角色為管理員角色 (IsAdmin = 1)，則該角色獲取所有權限
                            SELECT DISTINCT p.*
                            FROM {DBName.Main}.Permissions p
                            WHERE p.IsDeleted = 0
                            AND EXISTS (
                                SELECT 1
                                FROM {DBName.Main}.UserRoles ur
                                INNER JOIN {DBName.Main}.Roles r ON ur.RoleId = r.RoleId
                                WHERE ur.UserId = @UserId AND r.IsAdmin = 1 AND r.IsDeleted = 0)
                            UNION
                            -- 若群組為管理員群組 (IsAdmin = 1)，則該群組獲取所有權限
                            SELECT DISTINCT p.*
                            FROM {DBName.Main}.Permissions p
                            WHERE p.IsDeleted = 0
                            AND EXISTS (
                                SELECT 1
                                FROM {DBName.Main}.UserGroups ug
                                INNER JOIN {DBName.Main}.Groups g ON ug.GroupId = g.GroupId
                                WHERE ug.UserId = @UserId AND g.IsAdmin = 1 AND g.IsDeleted = 0);";
            try
            {
                var permissions = dapper.Query<Permissions>(sql, new { UserId });
                return permissions.ToList();
            }
            catch { return new List<Permissions>(); }
        }

    }
}

