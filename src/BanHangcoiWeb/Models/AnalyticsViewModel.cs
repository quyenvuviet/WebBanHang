namespace BanHangcoiWeb.Models;

public class AnalyticsViewModel
{
    public List<ChartPoint> DailyOrders { get; set; } = [];
    public List<ChartPoint> WeeklyOrders { get; set; } = [];
    public List<ChartPoint> MonthlyOrders { get; set; } = [];
    public List<ChartPoint> QuarterlyOrders { get; set; } = [];
    public List<ChartPoint> StatusPie { get; set; } = [];
    public long MaxBarValue { get; set; }
}

public class ChartPoint
{
    public string Label { get; set; } = "";
    public long Value { get; set; }
    public string Color { get; set; } = "#0d9488";
}
