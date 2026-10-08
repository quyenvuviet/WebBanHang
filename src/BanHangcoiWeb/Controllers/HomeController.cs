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

        var model = new DashboardViewModel
        {
            LeadCount = await db.CrmLeads.CountAsync(),
            HighIntentLeadCount = await db.CrmLeads.CountAsync(x => x.LeadStatus == LeadStatus.HighIntent),
            OrderCount = await db.SalesOrders.CountAsync(),
            DeliveredOrderCount = await db.SalesOrders.CountAsync(x => deliveredStatuses.Contains(x.OrderStatus)),
            OpenTicketCount = await db.SupportTickets.CountAsync(x => x.Status == TicketStatus.Open || x.Status == TicketStatus.InProgress),
            ClosedRevenueVnd = await db.SalesOrders
                .Where(x => activeOrderStatuses.Contains(x.OrderStatus))
                .SumAsync(x => x.TotalAmountVnd ?? 0),
            DeliveredRevenueVnd = await db.SalesOrders
                .Where(x => deliveredStatuses.Contains(x.OrderStatus))
                .SumAsync(x => x.TotalAmountVnd ?? 0),
            RecentLeads = await db.CrmLeads
                .Include(x => x.AssignedUser)
                .Include(x => x.Product)
                .OrderByDescending(x => x.LeadDate)
                .Take(6)
                .ToListAsync(),
            RecentOrders = await db.SalesOrders
                .Include(x => x.Customer)
                .Include(x => x.SaleUser)
                .OrderByDescending(x => x.OrderDate)
                .Take(6)
                .ToListAsync(),
            Inventory = inventory
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
