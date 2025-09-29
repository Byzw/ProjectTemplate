using Dapper;
using GoodSleepEIP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoodSleepEIP.Controllers
{
    [Authorize]
    [Route("api/web")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private IDapperHelper dapper;
        private readonly TokenService tokenService;
        private readonly PermissionService permissionService;
        private readonly UserClaims userClaims;

        public DashboardController(IConfiguration _config, TokenService _tokenService, IDapperHelper _dapper, PermissionService _permissionService)
        {
            configuration = _config;
            dapper = _dapper;
            tokenService = _tokenService;
            permissionService = _permissionService;
            userClaims = tokenService.GetUserClaims();
        }

    }
}