using GoodSleepEIP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GoodSleepEIP
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class PermissionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _moduleName;
        private readonly string _action;

        public PermissionAttribute(string moduleName, string action)
        {
            _moduleName = moduleName;
            _action = action;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            try
            {
                // 不能直接使用建構函式注入 DI 服務，因為它的生命週期與 ASP.NET Core 容器不同
                // 這裡改成使用 IServiceProvider 來取得 PermissionService
                var permissionService = context.HttpContext.RequestServices.GetRequiredService<PermissionService>();
                if (!permissionService.HasActionPermission(_moduleName, _action))
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }
            catch (Exception ex)
            {
                context.Result = new ObjectResult(new
                {
                    Status = 500,
                    Message = "驗證發生內部錯誤",
                    Error = ex.Message
                })
                {
                    StatusCode = 500 // 設定 HTTP 狀態碼
                };
                return;
            }
        }
    }
}
