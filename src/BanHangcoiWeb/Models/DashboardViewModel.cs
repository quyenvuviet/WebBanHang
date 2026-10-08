namespace BanHangcoiWeb.Models;

public class DashboardViewModel
{
    public int LeadCount { get; set; }
    public int HighIntentLeadCount { get; set; }
    public int OrderCount { get; set; }
    public int DeliveredOrderCount { get; set; }
    public int OpenTicketCount { get; set; }
    public long ClosedRevenueVnd { get; set; }
    public long DeliveredRevenueVnd { get; set; }
    public List<CrmLead> RecentLeads { get; set; } = [];
    public List<SalesOrder> RecentOrders { get; set; } = [];
    public List<InventorySummaryRow> Inventory { get; set; } = [];
}

public class InventorySummaryRow
{
    public string ProductName { get; set; } = "";
    public string? Sku { get; set; }
    public int CurrentQty { get; set; }
}
