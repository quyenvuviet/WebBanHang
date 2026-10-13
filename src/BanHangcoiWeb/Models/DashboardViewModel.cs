namespace BanHangcoiWeb.Models;

public class DashboardViewModel
{
    public int LeadCount { get; set; }
    public int HighIntentLeadCount { get; set; }
    public int FollowUpLeadCount { get; set; }
    public int NoAnswerLeadCount { get; set; }
    public int ClosedWonLeadCount { get; set; }
    public int ClosedLostLeadCount { get; set; }
    public int OrderCount { get; set; }
    public int DeliveredOrderCount { get; set; }
    public int ReturnOrderCount { get; set; }
    public int UpsaleCandidateCount { get; set; }
    public int OpenTicketCount { get; set; }
    public long ClosedRevenueVnd { get; set; }
    public long DeliveredRevenueVnd { get; set; }
    public long AverageOrderValueVnd { get; set; }
    public decimal LeadToOrderRate { get; set; }
    public decimal DeliverySuccessRate { get; set; }
    public List<CrmLead> RecentLeads { get; set; } = [];
    public List<SalesOrder> RecentOrders { get; set; } = [];
    public List<InventorySummaryRow> Inventory { get; set; } = [];
    public List<SalePerformanceRow> SalePerformance { get; set; } = [];
    public List<ClosedCustomerRow> ClosedCustomers { get; set; } = [];
    public List<ChartPoint> LeadStatusChart { get; set; } = [];
    public List<ChartPoint> RevenueBySaleChart { get; set; } = [];
    public List<ChartPoint> OrderStatusChart { get; set; } = [];
    public long MaxDashboardChartValue { get; set; } = 1;
}

public class InventorySummaryRow
{
    public string ProductName { get; set; } = "";
    public string? Sku { get; set; }
    public int CurrentQty { get; set; }
}

public class SalePerformanceRow
{
    public string SaleName { get; set; } = "";
    public int Leads { get; set; }
    public int Calls { get; set; }
    public int ClosedWon { get; set; }
    public long RevenueVnd { get; set; }
    public int LeadsWithPurchaseReason { get; set; }
}

public class ClosedCustomerRow
{
    public string CustomerName { get; set; } = "";
    public string? Phone { get; set; }
    public string? ProductName { get; set; }
    public string? SaleName { get; set; }
    public long RevenueVnd { get; set; }
    public string? PurchaseReason { get; set; }
    public string? LastNote { get; set; }
}
