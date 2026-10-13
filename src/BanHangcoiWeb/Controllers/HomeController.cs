using System.Diagnostics;
using BanHangcoiWeb.Data;
using BanHangcoiWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BanHangcoiWeb.Controllers;

[Authorize]
public class HomeController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var deliveredStatuses = new[] { OrderStatus.Delivered, OrderStatus.Done };
        var returnStatuses = new[] { OrderStatus.Returned, OrderStatus.Cancelled };
        var activeOrderStatuses = new[]
        {
            OrderStatus.Confirmed,
            OrderStatus.WaitingPack,
            OrderStatus.Packed,
            OrderStatus.HandoverToCarrier,
            OrderStatus.Shipping,
            OrderStatus.DeliveryIssue,
            OrderStatus.Delivered,
            OrderStatus.Done
        };

        var inventory = await db.Products
            .Select(product => new InventorySummaryRow
            {
                ProductName = product.ProductName,
                Sku = product.Sku,
                CurrentQty = db.InventoryMovements
                    .Where(movement => movement.ProductId == product.Id)
                    .Sum(movement => movement.MovementType == InventoryMovementType.SaleOut || movement.MovementType == InventoryMovementType.DefectOut
                        ? -movement.Quantity
                        : movement.Quantity)
            })
            .OrderBy(row => row.CurrentQty)
            .Take(8)
            .ToListAsync();

        var leads = await db.CrmLeads
            .Include(x => x.AssignedUser)
            .Include(x => x.Product)
            .Include(x => x.CallAttempts)
            .ToListAsync();
        var orders = await db.SalesOrders
            .Include(x => x.Customer)
            .Include(x => x.SaleUser)
            .Include(x => x.Items)
            .ToListAsync();
        var closedRevenue = orders
            .Where(x => activeOrderStatuses.Contains(x.OrderStatus))
            .Sum(x => x.TotalAmountVnd ?? x.CodAmountVnd ?? 0);
        var deliveredRevenue = orders
            .Where(x => deliveredStatuses.Contains(x.OrderStatus))
            .Sum(x => x.TotalAmountVnd ?? x.CodAmountVnd ?? 0);
        var deliveredCount = orders.Count(x => deliveredStatuses.Contains(x.OrderStatus));
        var returnCount = orders.Count(x => returnStatuses.Contains(x.OrderStatus));
        var colors = new[] { "#0d9488", "#2563eb", "#f59e0b", "#16a34a", "#ef4444", "#7c3aed", "#64748b" };
        static ChartPoint Point(string label, long value, string color) => new() { Label = label, Value = value, Color = color };

        var leadStatusChart = leads
            .GroupBy(x => x.LeadStatus)
            .OrderByDescending(x => x.Count())
            .Take(7)
            .Select((x, index) => Point(x.Key.Vi(), x.Count(), colors[index % colors.Length]))
            .ToList();
        var revenueBySaleChart = orders
            .GroupBy(x => x.SaleUser?.DisplayName ?? x.SaleUser?.LoginName ?? "Chưa gán")
            .Select(x => Point(x.Key, x.Sum(order => order.TotalAmountVnd ?? order.CodAmountVnd ?? 0), colors[Math.Abs(x.Key.GetHashCode()) % colors.Length]))
            .OrderByDescending(x => x.Value)
            .Take(8)
            .ToList();
        var orderStatusChart = orders
            .GroupBy(x => x.OrderStatus)
            .OrderByDescending(x => x.Count())
            .Select((x, index) => Point(x.Key.Vi(), x.Count(), colors[index % colors.Length]))
            .ToList();

        var salePerformance = leads
            .GroupBy(x => x.AssignedUser?.DisplayName ?? x.SaleNameRaw ?? "Chưa gán")
            .Select(x => new SalePerformanceRow
            {
                SaleName = x.Key,
                Leads = x.Count(),
                Calls = x.Sum(lead => lead.CallAttempts.Count),
                ClosedWon = x.Count(lead => lead.LeadStatus == LeadStatus.ClosedWon),
                RevenueVnd = x.Sum(lead => lead.DealValueVnd ?? 0),
                LeadsWithPurchaseReason = x.Count(lead => !string.IsNullOrWhiteSpace(lead.PurchaseReason))
            })
            .OrderByDescending(x => x.RevenueVnd)
            .ThenByDescending(x => x.ClosedWon)
            .Take(8)
            .ToList();

        var closedCustomers = leads
            .Where(x => x.LeadStatus == LeadStatus.ClosedWon || (x.DealValueVnd ?? 0) > 0)
            .OrderByDescending(x => x.DealValueVnd ?? 0)
            .Take(12)
            .Select(x => new ClosedCustomerRow
            {
                CustomerName = x.CustomerNameRaw ?? "",
                Phone = x.PhoneNormalized,
                ProductName = x.Product?.ProductName ?? x.ProductInterestRaw,
                SaleName = x.AssignedUser?.DisplayName ?? x.SaleNameRaw,
                RevenueVnd = x.DealValueVnd ?? 0,
                PurchaseReason = x.PurchaseReason,
                LastNote = x.SaleNote ?? x.NextActionNote
            })
            .ToList();

        var maxChartValue = leadStatusChart
            .Concat(revenueBySaleChart)
            .Concat(orderStatusChart)
            .Select(x => x.Value)
            .DefaultIfEmpty(1)
            .Max();

        var model = new DashboardViewModel
        {
            LeadCount = leads.Count,
            HighIntentLeadCount = leads.Count(x => x.LeadStatus == LeadStatus.HighIntent),
            FollowUpLeadCount = leads.Count(x => x.LeadStatus == LeadStatus.FollowUp),
            NoAnswerLeadCount = leads.Count(x => x.LeadStatus == LeadStatus.NoAnswer),
            ClosedWonLeadCount = leads.Count(x => x.LeadStatus == LeadStatus.ClosedWon),
            ClosedLostLeadCount = leads.Count(x => x.LeadStatus == LeadStatus.ClosedLost),
            OrderCount = orders.Count,
            DeliveredOrderCount = deliveredCount,
            ReturnOrderCount = returnCount,
            UpsaleCandidateCount = orders
                .Where(x => deliveredStatuses.Contains(x.OrderStatus) && !string.IsNullOrWhiteSpace(x.PhoneNormalized))
                .Select(x => x.PhoneNormalized)
                .Distinct()
                .Count(),
            OpenTicketCount = await db.SupportTickets.CountAsync(x => x.Status == TicketStatus.Open || x.Status == TicketStatus.InProgress),
            ClosedRevenueVnd = closedRevenue,
            DeliveredRevenueVnd = deliveredRevenue,
            AverageOrderValueVnd = orders.Count == 0 ? 0 : orders.Sum(x => x.TotalAmountVnd ?? x.CodAmountVnd ?? 0) / orders.Count,
            LeadToOrderRate = leads.Count == 0 ? 0 : Math.Round((decimal)orders.Count / leads.Count * 100, 1),
            DeliverySuccessRate = orders.Count == 0 ? 0 : Math.Round((decimal)deliveredCount / orders.Count * 100, 1),
            RecentLeads = leads
                .OrderByDescending(x => x.LeadDate)
                .Take(6)
                .ToList(),
            RecentOrders = orders
                .OrderByDescending(x => x.OrderDate)
                .Take(6)
                .ToList(),
            Inventory = inventory,
            SalePerformance = salePerformance,
            ClosedCustomers = closedCustomers,
            LeadStatusChart = leadStatusChart,
            RevenueBySaleChart = revenueBySaleChart,
            OrderStatusChart = orderStatusChart,
            MaxDashboardChartValue = Math.Max(1, maxChartValue)
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
