using Dapper;
using GoodSleepEIP.Models;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;

namespace GoodSleepEIP
{
    public class ReportTaskProcessor : ITaskProcessor
    {
        private readonly ILogger<ReportTaskProcessor> logger;
        private readonly IConfiguration configuration;
        private readonly IDapperHelper dapper;
        private readonly IFileService fileService;
        private readonly NotificationService notificationService;

        private const string fileType = ".xlsx";
        private readonly int perSheetCount = 900000; //每個excel sheet要保存的條數
        public Dictionary<string, string> okrReportType = [];

        public string TaskType => "Report"; // 需於 program.cs 中註冊該 taskType

        public ReportTaskProcessor(ILogger<ReportTaskProcessor> _logger, IConfiguration _configuration, IDapperHelper _dapper, IFileService _fileService, NotificationService _notificationService)
        {
            logger = _logger;
            configuration = _configuration;
            dapper = _dapper;
            fileService = _fileService;
            notificationService = _notificationService;
        }

        public async Task<object?> ProcessAsync(Guid taskId, object parameters, CancellationToken token)
        {
            try
            {
                // 嘗試將傳入的 parameters 轉換為 ReportInputParameters 類別
                if (parameters is not ReportOkrQueryParameters reportParams)
                {
                    throw new ArgumentException("無效的參數模型");
                }

                logger.LogInformation("[ReportTaskProcessor] 開始執行報表任務: {taskId}", taskId);

                // 參數型態轉換: 這裡告訴我們一個故事，切莫因為偷懶，而讓自己的程式碼變得難以維護
                // 變數含意應該是 DateTime 型別，那從資料庫到資料模型到前端到後端，都應該是 DateTime 型別，而不是因為
                // 一時懶得轉換，造成大家的不便與系統穩定性的問題!
                int startDate = reportParams.StartDate;
                int endDate = reportParams.EndDate;
                int comparisonYear1 = reportParams.ComparisonYear1.HasValue ? reportParams.ComparisonYear1.Value : 0;
                int comparisonYear2 = reportParams.ComparisonYear2.HasValue ? reportParams.ComparisonYear2.Value : 0;

                // 根據 ReportType 選擇執行不同的報表方法
                switch (reportParams.ReportType)
                {
                    case "01":
                        await Task.Run(() =>
                        {
                            // ReportAllDataSourceCustomerTargetRate(taskId, startDate, endDate, comparisonYear1, comparisonYear2);
                        });
                        break;
                    case "02":
                        await Task.Run(() =>
                        {
                            // ReportAllDataSourceCustomerTotalSpendingRate(taskId, startDate, endDate, comparisonYear1, comparisonYear2);
                        });
                        break;
                    case "03":
                        await Task.Run(() =>
                        {
                            // ReportAllDataSourceTotalTransactionAmount(taskId, startDate, endDate, comparisonYear1, comparisonYear2);
                        });
                        break;
                    case "04":
                        await Task.Run(() =>
                        {
                            // ReportAllDataSourceTotalPaymentAmount(taskId, startDate, endDate, comparisonYear1, comparisonYear2);
                        });
                        break;
                    case "31":
                        await Task.Run(() =>
                        {
                            // ReportAllDataSourceCustomerTrade(taskId, startDate, endDate, comparisonYear1, comparisonYear2);
                        });
                        break;
                    case "32":
                        await Task.Run(() =>
                        {
                            // ReportAllDataSourceCustomerRepurchase(taskId, startDate, endDate, comparisonYear1);
                        });
                        break;
                    case "91":
                        await Task.Run(() =>
                        {
                            // // 呼叫六個報表方法，全部回傳 DataTable
                            // var dt1 = ReportAllDataSourceCustomerTargetRate(taskId, startDate, endDate, comparisonYear1, comparisonYear2, true);
                            // var dt2 = ReportAllDataSourceCustomerTotalSpendingRate(taskId, startDate, endDate, comparisonYear1, comparisonYear2, true);
                            // var dt3 = ReportAllDataSourceTotalTransactionAmount(taskId, startDate, endDate, comparisonYear1, comparisonYear2, true);
                            // var dt4 = ReportAllDataSourceTotalPaymentAmount(taskId, startDate, endDate, comparisonYear1, comparisonYear2, true);
                            // var dt5 = ReportAllDataSourceCustomerTrade(taskId, startDate, endDate, comparisonYear1, comparisonYear2, true);
                            // var dt6 = ReportAllDataSourceCustomerRepurchase(taskId, startDate, endDate, comparisonYear1, true);
                            // var tables = new List<DataTable?> { dt1, dt2, dt3, dt4, dt5, dt6 };
                            // var validTables = tables.Where(dt => dt != null).Cast<DataTable>().ToList();
                            // var titles = new List<string> { "到客數目標達成率", "會員數目標達成率", "團績目標達成率", "消耗目標達成率", "新客成交率", "會員回購率" };
                            // var helper = new NPOIHelper();
                            // var bytes = helper.ExportTablesMatrixToOneSheetWithTitles(validTables, titles, ".xlsx", "OKR彙總報表", 3);
                            // using var excelFileStream = new MemoryStream(bytes);
                            // Guid uploadResult = fileService.Upload(
                            //     excelFileStream,
                            //     taskId,
                            //     $"OKR彙總報表_{DateTime.Now:yyyyMMdd-HHmm}.xlsx",
                            //     "system",
                            //     "Report",
                            //     "OKR彙總報表",
                            //     DateTime.Now.AddHours(24)
                            // );
                            // notificationService.UpdateProductionPercentage(taskId, 100);
                            // logger.LogInformation($"* OKR彙總 Excel 檔案 {uploadResult} 已建立");
                        });
                        break;

                    default:
                        throw new ArgumentException($"不支援的報表類型: {reportParams.ReportType}");
                }

                logger.LogInformation("[ReportTaskProcessor] 報表 {ReportType} 生成完成", reportParams.ReportType);

                return null;
            }
            catch (Exception ex)
            {
                logger.LogError("[ReportTaskProcessor] Task失敗: {Message}", ex.Message);
                return null;
            }
        }
        
    }
}
