using Dapper;
using GoodSleepEIP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GoodSleepEIP.Controllers
{
    [Authorize]
    [Route("api/web")]
    [ApiController]
    public class ThirdPartyConfigController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private IDapperHelper dapper;
        private readonly TokenService tokenService;
        private readonly ILogService logService;
        private readonly UserClaims userClaims;

        public ThirdPartyConfigController(IConfiguration _config, TokenService _tokenService, IDapperHelper _dapper, ILogService _logService)
        {
            configuration = _config;
            dapper = _dapper;
            tokenService = _tokenService;
            logService = _logService;
            userClaims = tokenService.GetUserClaims();
        }

        [Permission("ThirdPartyConfig", "CanRead")]
        [HttpPost("CompanyServiceIntegrationList")]
        public IActionResult CompanyServiceIntegrationList([FromBody] AgGridRequest request)
        {
            try
            {
                string sql_select = @$"SELECT 
                                        csi.*,
                                        c.CompanyName,
                                        p_st.Description as ServiceTypeDescription,
                                        p_et.Description as EnvTypeDescription
                                     ";
                string sql_from = @$"FROM {DBName.Main}.CompanyServiceIntegration csi
                                     LEFT JOIN {DBName.Main}.Company c ON csi.CompanyId = c.CompanyId
                                     LEFT JOIN {DBName.Main}.Parameter p_st ON csi.ServiceType = p_st.Code AND p_st.Category = 'ServiceType'
                                     LEFT JOIN {DBName.Main}.Parameter p_et ON csi.EnvType = p_et.Code AND p_et.Category = 'EnvType' ";
                string default_where = " 1=1 ";

                var fieldChangeMap = new List<AgGridFieldChangeMap>
                {
                    new() { OriginalFieldName = "Id", NewFieldName = "csi.Id" },
                    new() { OriginalFieldName = "CompanyName", NewFieldName = "c.CompanyName" },
                    new() { OriginalFieldName = "ServiceTypeDescription", NewFieldName = "p_st.Description" },
                    new() { OriginalFieldName = "EnvTypeDescription", NewFieldName = "p_et.Description" },
                    new() { OriginalFieldName = "CreationTime", StripString = new List<string> { "-" } },   // 去掉日期資料中的"-"來搜尋
                    new() { OriginalFieldName = "UpdateTime", NewFieldName = "csi.UpdateTime", StripString = new List<string> { "-" } },     // 去掉日期資料中的"-"來搜尋
                    new() { OriginalFieldName = "IsActive", NewFieldName = "csi.IsActive", AgGridValueChangeMap = { new() { OriginalValue = "否", NewValue = "0" }, new() { OriginalValue = "是", NewValue = "1" }} },
                };

                var gridResult = AgGridHelper.HandleAgGridRequest<dynamic>(dapper, request, sql_select, sql_from, default_where, fieldChangeMap, "csi.CreationTime DESC");
                return Ok(gridResult);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"表格產生錯誤: {ex.Message}" });
            }
        }

        [Permission("ThirdPartyConfig", "CanRead")]
        [HttpGet("FetchCompanyServiceIntegration")]
        public IActionResult FetchCompanyServiceIntegration(Guid integrationId)
        {
            try
            {
                string sqlstr = $"SELECT * FROM {DBName.Main}.CompanyServiceIntegration WHERE Id = @integrationId";
                var integration = dapper.Query<CompanyServiceIntegrationDto>(sqlstr, new { integrationId }).FirstOrDefault();

                if (integration == null)
                {
                    return ResponseMsg.Ok(false, "找不到指定的整合服務設定");
                }

                sqlstr = $"SELECT * FROM {DBName.Main}.CompanyServiceParam WHERE IntegrationId = @integrationId ORDER BY ParamKey";
                integration.Params = dapper.Query<CompanyServiceParam>(sqlstr, new { integrationId }).ToList();

                return ResponseMsg.Ok(true, "CompanyServiceIntegration Record.", integration);
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"讀取整合服務設定時發生錯誤: {ex.Message}");
            }
        }

        [Permission("ThirdPartyConfig", "CanCreate")]
        [HttpPost("CompanyServiceIntegrationAdd")]
        public IActionResult CompanyServiceIntegrationAdd([FromBody] CompanyServiceIntegrationDto requestData)
        {
            try
            {
                requestData.Id = Guid.NewGuid();

                string sql = @$"INSERT INTO {DBName.Main}.CompanyServiceIntegration (Id, CompanyId, ServiceType, EndpointName, EnvType, ApiBaseUrl, IsActive, CreationTime, UpdateTime)
                                VALUES (@Id, @CompanyId, @ServiceType, @EndpointName, @EnvType, @ApiBaseUrl, @IsActive, GETDATE(), GETDATE())";
                int integrationResult = dapper.Execute(sql, requestData);
                if (integrationResult != 1) return ResponseMsg.Ok(false, "新增整合服務設定失敗");

                if (requestData.Params.Count > 0)
                {
                    foreach (var param in requestData.Params)
                    {
                        param.Id = Guid.NewGuid();
                        param.IntegrationId = requestData.Id;
                    }
                    sql = @$"INSERT INTO {DBName.Main}.CompanyServiceParam (Id, IntegrationId, ParamKey, ParamValue, Memo, CreationTime, UpdateTime)
                             VALUES (@Id, @IntegrationId, @ParamKey, @ParamValue, @Memo, GETDATE(), GETDATE())";
                    int paramsResult = dapper.Execute(sql, requestData.Params);
                    if (paramsResult != requestData.Params.Count) return ResponseMsg.Ok(false, "新增整合服務參數失敗");
                }

                logService.CreateLog(logType: "info", actionType: "create", moduleName: "ThirdPartyConfig", username: userClaims.Username, logData: new { requestData });
                return ResponseMsg.Ok(true, "新增成功");
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"新增整合服務設定時發生錯誤: {ex.Message}");
            }
        }

        [Permission("ThirdPartyConfig", "CanUpdate")]
        [HttpPost("CompanyServiceIntegrationEdit")]
        public IActionResult CompanyServiceIntegrationEdit([FromBody] CompanyServiceIntegrationDto requestData)
        {
            try
            {
                if (requestData.Id == Guid.Empty) return ResponseMsg.Ok(false, "Id 不可為空");

                string sql = @$"UPDATE {DBName.Main}.CompanyServiceIntegration SET 
                                    CompanyId = @CompanyId,
                                    ServiceType = @ServiceType,
                                    EndpointName = @EndpointName,
                                    EnvType = @EnvType,
                                    ApiBaseUrl = @ApiBaseUrl,
                                    IsActive = @IsActive,
                                    UpdateTime = GETDATE()
                                WHERE Id = @Id";
                int integrationResult = dapper.Execute(sql, requestData);
                if (integrationResult != 1) return ResponseMsg.Ok(false, "更新整合服務設定失敗");

                // Params: delete and re-insert
                string deleteSql = $"DELETE FROM {DBName.Main}.CompanyServiceParam WHERE IntegrationId = @Id";
                dapper.Execute(deleteSql, new { requestData.Id });

                if (requestData.Params.Count > 0)
                {
                    foreach (var param in requestData.Params)
                    {
                        param.Id = Guid.NewGuid();
                        param.IntegrationId = requestData.Id;
                    }
                    string insertSql = @$"INSERT INTO {DBName.Main}.CompanyServiceParam (Id, IntegrationId, ParamKey, ParamValue, Memo, CreationTime, UpdateTime)
                                          VALUES (@Id, @IntegrationId, @ParamKey, @ParamValue, @Memo, GETDATE(), GETDATE())";
                    int paramsResult = dapper.Execute(insertSql, requestData.Params);
                    if (paramsResult != requestData.Params.Count) return ResponseMsg.Ok(false, "更新整合服務參數失敗");
                }

                logService.CreateLog(logType: "info", actionType: "update", moduleName: "ThirdPartyConfig", username: userClaims.Username, logData: new { requestData });
                return ResponseMsg.Ok(true, "更新成功");

            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"更新整合服務設定時發生錯誤: {ex.Message}");
            }
        }

        [Permission("ThirdPartyConfig", "CanDelete")]
        [HttpGet("CompanyServiceIntegrationDel")]
        public IActionResult CompanyServiceIntegrationDel(Guid integrationId)
        {
            try
            {
                if (integrationId == Guid.Empty) return ResponseMsg.Ok(false, "Id 不可為空");

                // First, delete parameters
                string deleteParamsSql = $"DELETE FROM {DBName.Main}.CompanyServiceParam WHERE IntegrationId = @integrationId";
                dapper.Execute(deleteParamsSql, new { integrationId });

                // Then, delete the integration
                string deleteIntegrationSql = $"DELETE FROM {DBName.Main}.CompanyServiceIntegration WHERE Id = @integrationId";
                int result = dapper.Execute(deleteIntegrationSql, new { integrationId });

                if (result != 1) return ResponseMsg.Ok(false, "刪除整合服務設定失敗");

                logService.CreateLog(logType: "info", actionType: "delete", moduleName: "ThirdPartyConfig", username: userClaims.Username, logData: new { integrationId });
                return ResponseMsg.Ok(true, "刪除成功");
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"刪除整合服務設定時發生錯誤: {ex.Message}");
            }
        }

        [Permission("ThirdPartyConfig", "CanRead")]
        [HttpGet("FetchCompanyServiceParams")]
        public IActionResult FetchCompanyServiceParams(Guid integrationId)
        {
            try
            {
                string sqlstr = $"SELECT * FROM {DBName.Main}.CompanyServiceParam WHERE IntegrationId = @integrationId ORDER BY ParamKey";
                var parameters = dapper.Query<CompanyServiceParam>(sqlstr, new { integrationId }).ToList();

                return ResponseMsg.Ok(true, "CompanyServiceParam Records.", parameters);
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"讀取整合服務參數時發生錯誤: {ex.Message}");
            }
        }
    }
}