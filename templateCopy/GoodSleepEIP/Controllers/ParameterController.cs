using Dapper;
using GoodSleepEIP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GoodSleepEIP.Controllers
{
    [Authorize]
    [Route("api/web")]
    [ApiController]
    public class ParameterController : ControllerBase
    {
        private IDapperHelper dapper;
        private readonly IParameterService parameterService;

        public ParameterController(IDapperHelper _dapper, IParameterService _parameterService)
        {
            dapper = _dapper;
            parameterService = _parameterService;
        }

        // 以下為參數管理 CRUD API /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Permission("SysAdmin", "CanManage")]
        [HttpPost("ParameterList")]
        public IActionResult ParameterList([FromBody] AgGridRequest request)
        {
            // 下面是範例程式碼，用來解析 AgGridRequest 的 ExtraParams，為了可以動態判別要查詢的資料
            //if (request.ExtraParams is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object && jsonElement.EnumerateObject().Any())
            //{
            //    try
            //    {
            //        var extraParams = jsonElement.Deserialize<ReportOkrQueryParameters>(); // 反序列化成某個 DTO
            //        if (extraParams != null)
            //        {
            //            string reportType = extraParams.ReportType; // 成功了!
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        return BadRequest(new { success = false, message = $"解析 ExtraParams 失敗: {ex.Message}" });
            //    }
            //}

            try
            {
                string sql_select = @"SELECT * ";
                string sql_from = @$"FROM {DBName.Main}.Parameter ";
                string default_where = "";
                var fieldChangeMap = new List<AgGridFieldChangeMap>{};

                var gridResult = AgGridHelper.HandleAgGridRequest<dynamic>(dapper, request, sql_select, sql_from, default_where, fieldChangeMap, "Category");
                return Ok(gridResult);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"表格產生錯誤: {ex.Message}" });
            }
        }

        [Permission("SysAdmin", "CanManage")]
        [HttpGet("FetchParameterRecord")]
        public IActionResult FetchParameterRecord(Guid ParameterId)
        {
            try
            {
                string sqlstr = $"SELECT * FROM {DBName.Main}.Parameter WHERE ParameterId = @ParameterId";
                var Record_list = (List<Parameter>)dapper.Query<Parameter>(sqlstr, new { ParameterId });
                if (Record_list.Count == 0) return ResponseMsg.Ok(false, "找不到 Parameter 資料");

                return ResponseMsg.Ok(true, "Parameter Record.", Record_list[0]);
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"取 Parameter 資料錯誤: {ex.Message}");
            }
        }

        // 新增
        [Permission("SysAdmin", "CanManage")]
        [HttpPost("ParameterAdd")]
        public IActionResult ParameterAdd([FromBody] Parameter RequestData)
        {
            try
            {
                // 新增紀錄
                int ex_count = dapper.Execute(@$"INSERT INTO {DBName.Main}.Parameter (Category, Code, Description, Memo, IsSystemReserved) 
                                                VALUES (@Category, @Code, @Description, @Memo, @IsSystemReserved)", RequestData);
                if (ex_count != 1) return ResponseMsg.Ok(false, "ParameterAdd 檢查錯誤，請告知系統管理員");

                return ResponseMsg.Ok(true, "");
            }
            catch (Exception ex) { return ResponseMsg.Ok(false, $"ParameterAdd 錯誤: {ex.Message}"); }
        }

        // 修改
        [Permission("SysAdmin", "CanManage")]
        [HttpPost("ParameterEdit")]
        public IActionResult ParameterEdit([FromBody] Parameter RequestData)
        {
            try
            {
                if (RequestData.ParameterId == Guid.Empty) return ResponseMsg.Ok(false, "ID 不可為空值");

                int ex_count = dapper.Execute(@$"UPDATE {DBName.Main}.Parameter SET Category = @Category, Code = @Code, Description = @Description, Memo = @Memo
                                                WHERE ParameterId = @ParameterId AND IsSystemReserved = '0'", RequestData);
                if (ex_count != 1) return ResponseMsg.Ok(false, "ParameterEdit 檢查錯誤，請告知系統管理員");

                return ResponseMsg.Ok(true, "");
            }
            catch (Exception ex) { return ResponseMsg.Ok(false, "ParameterEdit 錯誤:" + ex.Message); }
        }

        // 刪除
        [Permission("SysAdmin", "CanManage")]
        [HttpGet("ParameterDel")]
        public IActionResult ParameterDel(Guid ParameterId)
        {
            try
            {
                if (ParameterId == Guid.Empty) return ResponseMsg.Ok(false, "ID 不可為空值");
                int ex_count = dapper.Execute(@$"DELETE FROM {DBName.Main}.Parameter WHERE ParameterId = @ParameterId AND IsSystemReserved = '0'", new { ParameterId });
                if (ex_count != 1) return ResponseMsg.Ok(false, "ParameterDel 檢查錯誤，請告知系統管理員");
                return ResponseMsg.Ok(true, "");
            }
            catch { return ResponseMsg.Ok(false, "ParameterDel 發生內部錯誤"); }
        }

        // 以下為個模組經常性取用參數之 API ////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Authorize]
        [HttpPost("GetParameters")]
        public IActionResult GetParameters(string category)
        {
            try
            {
                return ResponseMsg.Ok(true, "Parameters data.", parameterService.GetParameters(category));
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"發生內部錯誤: {ex.Message}");
            }
        }

        [Authorize]
        [HttpGet("GetPrefixParameters")]
        public IActionResult GetPrefixParameters(string CategoryPrefix)
        {
            try
            {
                return ResponseMsg.Ok(true, "Parameters data.", parameterService.GetPrefixParameters(CategoryPrefix));
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"發生內部錯誤: {ex.Message}");
            }
        }

        [Authorize]
        [HttpPost("GetListParameters")]
        public IActionResult GetListParameters(List<string> categories)
        {
            try
            {
                return ResponseMsg.Ok(true, "Parameters data.", parameterService.GetListParameters(categories));
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"發生內部錯誤: {ex.Message}");
            }
        }

        [Authorize]
        [HttpGet("GetAllParameters")]
        public IActionResult GetAllParameters()
        {
            try
            {
                return ResponseMsg.Ok(true, "All parameters data.", parameterService.GetAllParameters());
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"發生內部錯誤: {ex.Message}");
            }
        }
    }
}