namespace GoodSleepEIP.Models
{
    /// <summary>
    /// 物流對帳資料項目
    /// </summary>
    public class LogisticsReconciliationItem
    {
        public string ShipmentDate { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public string OrderNo { get; set; } = string.Empty;
        public decimal CodAmount { get; set; }
        public decimal Fee { get; set; }
        public string Remark { get; set; } = string.Empty;
    }

    /// <summary>
    /// 作為物流對帳背景任務參數傳遞的模型
    /// </summary>
    public class ImportReconciliationParameters
    {
        public string PersonId { get; set; } = string.Empty;
        public List<LogisticsReconciliationItem> Data { get; set; } = [];
        public byte[] OriginalFileContent { get; set; } = [];
    }

    public class ReportOkrQueryParameters
    {
        public string ReportType { get; set; } = "";
        public int StartDate { get; set; }  // 20250101
        public int EndDate { get; set; }
        public int? ComparisonYear1 { get; set; }   // 2025
        public int? ComparisonYear2 { get; set; }
    }

    public class ReportOthersQueryParameters
    {
        public Guid CompanyId { get; set; }
        public string ReportType { get; set; } = "";
        public int StartDate { get; set; }
        public int EndDate { get; set; }
    }
}
