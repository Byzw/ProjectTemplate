using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GoodSleepEIP.Models;
using AutoMapper;
using System.Data;

namespace GoodSleepEIP.Controllers;

[Authorize]
[Route("api/web")]
[ApiController]
public class SalePerformanceController : ControllerBase
{
    private readonly IConfiguration configuration;
    private readonly IDapperHelper dapper;
    private readonly TokenService tokenService;
    private readonly PermissionService permissionService;
    private readonly SequenceService sequenceService;
    private readonly UserClaims userClaims;
    private readonly IMapper mapper;
    private readonly ILogService logService;
    private const string OrgId = "1000"; // T8 預設設定 OrgId 為 "1000"
    public SalePerformanceController(
        IConfiguration _config,
        TokenService _tokenService,
        IDapperHelper _dapper,
        PermissionService _permissionService,
        SequenceService _sequenceService,
        IMapper _mapper,
        ILogService _logService)
        {
        configuration = _config;
        dapper = _dapper;
        tokenService = _tokenService;
        permissionService = _permissionService;
        sequenceService = _sequenceService;
        userClaims = tokenService.GetUserClaims();
        mapper = _mapper;
        logService = _logService;
    }

    /// <summary>
    /// 客戶管理主檔查詢
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [Permission("SalePerformance", "CanRead")]
    [HttpPost("SalePerformanceList")]
    public IActionResult SalePerformanceList([FromBody] AgGridRequest request)
    {
        try
        {
            string db = "CHIcomp01";
            string sql_select = @$"select 
                                    '' Schedule,
                                    a.FundBillNo OrderNo,
                                    a.BillDate OrderDate,
                                    a.CustID CustomerID,
                                    (select top 1 FullName from {db}.dbo.comCustomer c where a.CustID = c.ID) FullName,
                                    (select top 1 PersonName from {db}.dbo.comPerson p where a.SalesMan = p.PersonID) PersonName,
                                    format(isnull(a.Total, 0), '0.####') Sale,
                                    format(isnull(b.LocalTotal, 0), '0.####') OrderMoney,
                                    format(round(isnull(b.LocalTotal * 100/nullif(a.Total,0), 0), 2), '0.##') DepositPercent,
                                    format(isnull(a.Total - b.LocalTotal, 0), '0.####') DepositLack,
                                    format(isnull(round(b.LocalTotal * 0.7, 0), 0), '0.####') Deposit70Percent,
                                    format(isnull(round(b.LocalTotal * 0.3, 0), 0), '0.####') Deposit30Percent,
                                    b.EndacaseDate ActShipmentDate,
                                    (select STRING_AGG( PreInDate, ',') from (select distinct a.BillNO, b.PreInDate
                                    from {db}.dbo.ordBillMain a left join {db}.dbo.ordBillSub b on a.BillNO = b.BillNO where a.BillNO = b.BillNO group by a.BillNO,b.PreInDate) t where t.BillNO = a.FundBillNo
                                    ) ExpectedDeliveryDate,
                                    a.UDef1 DeliveryDriver,
                                    (select case when sum(us.salePercent) is null then '否' else '是' end  from {DBName.Main}.ERPUserSale us where us.BillNO = a.FundBillNo) IsExistSale";
            string sql_from = @$" from {db}.dbo.comBillAccounts a 
                                left join (select comPR.BillNO, ordBM.LocalTotal, ordBM.EndacaseDate from {db}.dbo.ordBillMain ordBM 
                                inner join {db}.dbo.comProdRec comPR  on ordBM.BillNO = comPR.FromNO and comPR.Flag = 500
                                group by comPR.BillNO, ordBM.LocalTotal, ordBM.EndacaseDate ) b on a.FundBillNo = b.BillNO ";
            string default_where = " a.flag = 500 ";
            var fieldChangeMap = new List<AgGridFieldChangeMap>
            {
                new() { OriginalFieldName = "BizPartnerId", NewFieldName = "comCust.BizPartnerId" },
                new() { OriginalFieldName = "SexType", NewFieldName = "mainCust.SexType", AgGridValueChangeMap = { new() { OriginalValue = "未指定", NewValue = "-1" }, new() { OriginalValue = "男", NewValue = "0" }, new() { OriginalValue = "女", NewValue = "1" }} },
                new() { OriginalFieldName = "WorkTelNo", StripString = new List<string> { "-" } },          // 去掉聯絡電話資料中的"-"來搜尋
                new() { OriginalFieldName = "FaxNo", StripString = new List<string> { "-" } },              // 去掉住家電話資料中的"-"來搜尋
                new() { OriginalFieldName = "QQNo", StripString = new List<string> { "-" } },               // 去掉手機號碼資料中的"-"來搜尋
                new() { OriginalFieldName = "BirthDateTime", StripString = new List<string> { "-" } },      // 去掉生日資料中的"-"來搜尋
            };

            var gridResult = AgGridHelper.HandleAgGridRequest<dynamic>(dapper, request, sql_select, sql_from, default_where, fieldChangeMap, "a.FundBillNo");
            return Ok(gridResult);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"表格產生錯誤: {ex.Message}" });
        }
    }

    
}

