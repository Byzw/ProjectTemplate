namespace GoodSleepEIP.Models;

public class SalePerformanceDto
{
    public string? Schedule { get; set; }
    public int? OrderDate { get; set; }
    public string? OrderNo { get; set; }
    public string? CustomerID { get; set; }
    public string? FullName { get; set; }
    public string? PersonName { get; set; }
    public double? Sale { get; set; }
    public double? OrderMoney { get; set; }
    public double? AddMoney { get; set; }
    public string? DepositPercent { get; set; }
    public double? DepositLack { get; set; }
    public double? Deposit70Percent { get; set; }
    public double? Deposit30Percent { get; set; }
    public string? ActShipmentDate { get; set; }
    public string? ExpectedDeliveryDate { get; set; }
    public string? DeliveryDriver { get; set; }
    public string? UserDescription { get; set; }
    public string? SalePercent { get; set; }
    public string? IsExistSale { get; set; }
}

