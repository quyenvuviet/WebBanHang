using BanHangcoiWeb.Data;
using BanHangcoiWeb.Models;
using BanHangcoiWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BanHangcoiWeb.Controllers;

[Authorize]
public class ManagementController(AppDbContext db, LocalAiAdvisor aiAdvisor) : Controller
{
    [Authorize(Roles = "Admin,Manager,Marketing")]
    [HttpGet]
    public IActionResult CreateLead()
    {
        return View(new LeadEntryViewModel
        {
            LeadDate = DateOnly.FromDateTime(DateTime.Today),
            LeadType = "SỐ NÓNG"
        });
    }

    [Authorize(Roles = "Admin,Manager,Marketing")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLead(LeadEntryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var phone = NormalizePhone(model.Phone);
        var customer = await GetOrCreateCustomerAsync(phone, model.CustomerName);
        var campaign = await GetOrCreateCampaignAsync(model.PageName, model.ContentName, model.ProductInterestRaw);
        var lead = new CrmLead
        {
            Customer = customer,
            Campaign = campaign,
            LeadDate = model.LeadDate,
            LeadStatus = LeadStatus.New,
            PhoneNormalized = phone,
            CustomerNameRaw = model.CustomerName,
            LeadType = model.LeadType,
            Occupation = model.Occupation,
            PageName = model.PageName,
            ContentName = model.ContentName,
            ProductInterestRaw = model.ProductInterestRaw,
            VehicleAirType = model.VehicleAirType,
            VehicleType = model.VehicleType,
            SourceFile = "manual",
            SourceSheet = "marketing-entry",
            SourceRow = 0
        };
        db.CrmLeads.Add(lead);
        await NotifyRoleAsync(UserRole.Manager, "Lead mới từ Marketing", $"{model.CustomerName} - {phone} - {model.PageName}", "CrmLead", lead.Id);
        await AuditAsync("CrmLead", lead.Id, "Marketing nhập lead", null, $"{model.CustomerName} {phone}");
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(EditLead), new { id = lead.Id });
    }

    [Authorize(Roles = "Admin,Manager,Sale,Marketing")]
    [HttpGet]
    public async Task<IActionResult> EditLead(Guid id)
    {
        var lead = await db.CrmLeads
            .Include(x => x.CallAttempts)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (lead is null)
        {
            return NotFound();
        }

        return View(LeadEntryViewModel.FromLead(lead));
    }

    [Authorize(Roles = "Admin,Manager,Sale")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditLead(Guid id, LeadEntryViewModel model)
    {
        var lead = await db.CrmLeads
            .Include(x => x.CallAttempts)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (lead is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.Id = id;
            return View(model);
        }

        lead.LeadDate = model.LeadDate;
        lead.CustomerNameRaw = model.CustomerName;
        lead.PhoneNormalized = NormalizePhone(model.Phone);
        lead.LeadType = model.LeadType;
        lead.Occupation = model.Occupation;
        lead.PageName = model.PageName;
        lead.ContentName = model.ContentName;
        lead.ProductInterestRaw = model.ProductInterestRaw;
        lead.VehicleAirType = model.VehicleAirType;
        lead.VehicleType = model.VehicleType;
        lead.SaleNameRaw = model.SaleName;
        lead.WorkAt = model.WorkAt;
        lead.FloatingDays = model.FloatingDays;
        lead.DealValueVnd = model.DealValueVnd;
        lead.SaleNote = model.SaleNote;
        lead.SaleLevel = model.SaleLevel;
        lead.PurchaseReason = model.PurchaseReason;
        lead.NextActionNote = model.SaleNote;
        lead.LeadStatus = model.LeadStatus;
        if (lead.AssignedUserId is null)
        {
            lead.AssignedUserId = CurrentUserId();
        }

        UpsertCall(lead, 1, model.Call1Result, model.Call1Date, model.Call1Note, model.Call1Minutes);
        UpsertCall(lead, 2, model.Call2Result, model.Call2Date, model.Call2Note, model.Call2Minutes);
        UpsertCall(lead, 3, model.Call3Result, model.Call3Date, model.Call3Note, model.Call3Minutes);
        UpsertCall(lead, 4, model.Call4Result, model.Call4Date, model.Call4Note, model.Call4Minutes);

        if (model.LeadStatus == LeadStatus.ClosedWon && model.DealValueVnd > 0)
        {
            var existingOrder = await db.SalesOrders.FirstOrDefaultAsync(x => x.LeadId == lead.Id);
            if (existingOrder is null)
            {
                db.SalesOrders.Add(new SalesOrder
                {
                    Lead = lead,
                    Customer = lead.Customer,
                    OrderStatus = OrderStatus.Confirmed,
                    OrderDate = model.LeadDate,
                    CustomerNameRaw = model.CustomerName,
                    PhoneNormalized = lead.PhoneNormalized,
                    CodAmountVnd = model.DealValueVnd,
                    TotalAmountVnd = model.DealValueVnd,
                    NoteRaw = model.SaleNote,
                    ReceiverName = model.CustomerName,
                    ReceiverPhone = lead.PhoneNormalized,
                    ShippingAddress = lead.Customer?.AddressRaw,
                    SourceFile = "manual",
                    SourceSheet = "sale-close",
                    SourceRow = 0
                });
                await NotifyRoleAsync(UserRole.Warehouse, "Sale vừa chốt đơn", $"{model.CustomerName} - {(model.DealValueVnd ?? 0):N0}đ", "CrmLead", lead.Id);
            }
        }

        await AuditAsync("CrmLead", lead.Id, "Sale cập nhật lead", null, $"{lead.LeadStatus.Vi()} - {model.DealValueVnd:N0}");
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Leads));
    }

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Workflow()
    {
        var model = new WorkflowDashboardViewModel
        {
            RoleTasks = BuildRoleTasks(),
            Notifications = await db.WorkNotifications
                .Include(x => x.TargetUser)
                .OrderByDescending(x => x.CreatedAt)
                .Take(30)
                .ToListAsync(),
            AuditLogs = await db.AuditLogs
                .Include(x => x.Actor)
                .OrderByDescending(x => x.CreatedAt)
                .Take(30)
                .ToListAsync(),
            AiInsights = await aiAdvisor.BuildInsightsAsync()
        };

        return View(model);
    }

    [Authorize]
    public async Task<IActionResult> MyTasks()
    {
        var role = CurrentUserRole();
        var userId = CurrentUserId();
        var notifications = await db.WorkNotifications
            .Where(x => x.Status == NotificationStatus.Unread && (x.TargetUserId == userId || x.TargetRole == role || x.TargetRole == null))
            .OrderByDescending(x => x.CreatedAt)
            .Take(40)
            .ToListAsync();

        ViewBag.Role = role.HasValue ? role.Value.Vi() : "";
        return View(notifications);
    }

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> AiAdvisor()
    {
        return View(await aiAdvisor.BuildInsightsAsync());
    }

    [Authorize(Roles = "Admin,Manager,Sale")]
    [HttpGet]
    public async Task<IActionResult> CreateOrder(Guid leadId)
    {
        var lead = await db.CrmLeads.Include(x => x.Customer).FirstOrDefaultAsync(x => x.Id == leadId);
        if (lead is null)
        {
            return NotFound();
        }

        return View(new SalesOrderFormViewModel
        {
            LeadId = lead.Id,
            CustomerName = lead.CustomerNameRaw ?? lead.Customer?.CustomerName ?? "",
            Phone = lead.PhoneNormalized ?? lead.Customer?.PhoneNormalized ?? "",
            ProductName = lead.ProductInterestRaw ?? "",
            TotalAmountVnd = lead.DealValueVnd ?? 0,
            ReceiverName = lead.CustomerNameRaw ?? lead.Customer?.CustomerName ?? "",
            ReceiverPhone = lead.PhoneNormalized ?? lead.Customer?.PhoneNormalized ?? "",
            ShippingAddress = lead.Customer?.AddressRaw ?? "",
            Note = lead.SaleNote
        });
    }

    [Authorize(Roles = "Admin,Manager,Sale")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOrder(SalesOrderFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var lead = await db.CrmLeads.Include(x => x.Customer).FirstOrDefaultAsync(x => x.Id == model.LeadId);
        if (lead is null)
        {
            return NotFound();
        }

        var order = new SalesOrder
        {
            Lead = lead,
            Customer = lead.Customer,
            SaleUserId = CurrentUserId(),
            OrderStatus = OrderStatus.WaitingPack,
            OrderDate = DateOnly.FromDateTime(DateTime.Today),
            CustomerNameRaw = model.CustomerName,
            PhoneNormalized = NormalizePhone(model.Phone),
            CodAmountVnd = model.TotalAmountVnd,
            TotalAmountVnd = model.TotalAmountVnd,
            ShippingFeeVnd = model.ShippingFeeVnd,
            ReceiverName = model.ReceiverName,
            ReceiverPhone = NormalizePhone(model.ReceiverPhone),
            ShippingAddress = model.ShippingAddress,
            AddressRaw = model.ShippingAddress,
            ComboRaw = model.ProductName,
            PackingNote = model.PackingNote,
            NoteRaw = model.Note,
            SourceFile = "manual",
            SourceSheet = "sale-order",
            SourceRow = 0
        };
        db.SalesOrders.Add(order);
        db.OrderItems.Add(new OrderItem
        {
            Order = order,
            ProductNameRaw = model.ProductName,
            Quantity = model.Quantity <= 0 ? 1 : model.Quantity,
            UnitPriceVnd = model.TotalAmountVnd,
            LineAmountVnd = model.TotalAmountVnd
        });

        lead.LeadStatus = LeadStatus.ClosedWon;
        lead.DealValueVnd = model.TotalAmountVnd;
        await NotifyRoleAsync(UserRole.Warehouse, "Đơn mới cần đóng hàng", $"{model.CustomerName} - {model.ProductName} - {model.TotalAmountVnd:N0}đ", "SalesOrder", order.Id);
        await NotifyRoleAsync(UserRole.Manager, "Sale chốt đơn", $"{User.Identity?.Name} chốt {model.TotalAmountVnd:N0}đ", "SalesOrder", order.Id);
        await AuditAsync("SalesOrder", order.Id, "Sale tạo đơn", null, $"{model.CustomerName} {model.TotalAmountVnd:N0}");
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Orders), new { status = OrderStatus.WaitingPack.ToString() });
    }

    [Authorize(Roles = "Admin,Manager,Warehouse")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PackOrder(Guid id, string? packingNote)
    {
        var order = await db.SalesOrders.FirstOrDefaultAsync(x => x.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        var oldStatus = order.OrderStatus.Vi();
        order.OrderStatus = OrderStatus.Packed;
        order.PackingNote = packingNote;
        await NotifyRoleAsync(UserRole.Shipping, "Kho đã đóng hàng", $"{order.CustomerNameRaw} - COD {(order.TotalAmountVnd ?? 0):N0}đ", "SalesOrder", order.Id);
        await AuditAsync("SalesOrder", order.Id, "Kho đóng hàng", oldStatus, order.OrderStatus.Vi());
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Orders), new { status = OrderStatus.Packed.ToString() });
    }

    [Authorize(Roles = "Admin,Manager,Shipping")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateShipment(ShipmentUpdateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Shipments));
        }

        var order = await db.SalesOrders.FirstOrDefaultAsync(x => x.Id == model.OrderId);
        if (order is null)
        {
            return NotFound();
        }

        var shipment = await db.Shipments.FirstOrDefaultAsync(x => x.OrderId == model.OrderId);
        if (shipment is null)
        {
            shipment = new Shipment { OrderId = model.OrderId };
            db.Shipments.Add(shipment);
        }

        shipment.TrackingCode = model.TrackingCode;
        shipment.CarrierName = model.CarrierName;
        shipment.ShipmentStatus = model.ShipmentStatus;
        shipment.DeliveryIssueNote = model.DeliveryIssueNote;
        shipment.ShippedAt ??= DateTimeOffset.Now;
        if (model.ShipmentStatus == OrderStatus.Delivered || model.ShipmentStatus == OrderStatus.Done)
        {
            shipment.DeliveredAt = DateTimeOffset.Now;
            await NotifyRoleAsync(UserRole.Support, "Đơn giao thành công cần CSKH", $"{order.CustomerNameRaw} - {order.PhoneNormalized}", "SalesOrder", order.Id);
        }
        if (model.ShipmentStatus == OrderStatus.DeliveryIssue || model.ShipmentStatus == OrderStatus.Returned)
        {
            await NotifyRoleAsync(UserRole.Support, "Vận đơn có vấn đề", $"{order.CustomerNameRaw} - {model.DeliveryIssueNote}", "SalesOrder", order.Id);
            await NotifyRoleAsync(UserRole.Manager, "Cảnh báo vận chuyển", $"{order.CustomerNameRaw} - {model.ShipmentStatus.Vi()}", "SalesOrder", order.Id);
        }

        order.OrderStatus = model.ShipmentStatus;
        await AuditAsync("Shipment", shipment.Id, "Vận chuyển cập nhật", null, $"{model.TrackingCode} - {model.ShipmentStatus.Vi()}");
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Shipments));
    }

    [Authorize(Roles = "Admin,Manager,Support")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSupport(SupportUpdateViewModel model)
    {
        var order = await db.SalesOrders.Include(x => x.Customer).FirstOrDefaultAsync(x => x.Id == model.OrderId);
        if (order is null)
        {
            return NotFound();
        }

        var ticket = await db.SupportTickets.FirstOrDefaultAsync(x => x.OrderId == model.OrderId && x.Status != TicketStatus.Closed);
        if (ticket is null)
        {
            ticket = new SupportTicket
            {
                Order = order,
                Customer = order.Customer,
                AssignedUserId = CurrentUserId()
            };
            db.SupportTickets.Add(ticket);
        }

        ticket.IssueType = model.IssueType;
        ticket.Status = model.Status;
        ticket.IssueNote = model.IssueNote;
        ticket.ResolutionNote = model.ResolutionNote;
        ticket.AssignedUserId ??= CurrentUserId();
        if (model.Status == TicketStatus.Resolved || model.Status == TicketStatus.Closed)
        {
            order.OrderStatus = OrderStatus.Done;
            await NotifyRoleAsync(UserRole.Manager, "CSKH đã xử lý xong", $"{order.CustomerNameRaw} - đơn chuyển Done", "SalesOrder", order.Id);
        }

        await AuditAsync("SupportTicket", ticket.Id, "CSKH cập nhật", null, $"{model.IssueType} - {model.Status.Vi()}");
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Support));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotificationDone(long id)
    {
        var notification = await db.WorkNotifications.FirstOrDefaultAsync(x => x.Id == id);
        if (notification is null)
        {
            return NotFound();
        }

        notification.Status = NotificationStatus.Done;
        notification.ReadAt = DateTimeOffset.Now;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(MyTasks));
    }

    public async Task<IActionResult> Analytics()
    {
        var orders = await db.SalesOrders
            .Where(x => x.OrderDate != null)
            .Select(x => new { x.OrderDate, Revenue = x.TotalAmountVnd ?? x.CodAmountVnd ?? 0, x.OrderStatus })
            .ToListAsync();

        static ChartPoint Point(string label, long value, string color = "#0d9488") => new() { Label = label, Value = value, Color = color };

        var daily = orders
            .GroupBy(x => x.OrderDate!.Value)
            .OrderBy(x => x.Key)
            .Select(x => Point(x.Key.ToString("dd/MM"), x.Count()))
            .ToList();

        var weekly = orders
            .GroupBy(x => System.Globalization.ISOWeek.GetWeekOfYear(x.OrderDate!.Value.ToDateTime(TimeOnly.MinValue)))
            .OrderBy(x => x.Key)
            .Select(x => Point($"Tuần {x.Key}", x.Count(), "#14b8a6"))
            .ToList();

        var monthly = orders
            .GroupBy(x => new { x.OrderDate!.Value.Year, x.OrderDate.Value.Month })
            .OrderBy(x => x.Key.Year)
            .ThenBy(x => x.Key.Month)
            .Select(x => Point($"{x.Key.Month:00}/{x.Key.Year}", x.Sum(o => o.Revenue), "#0f766e"))
            .ToList();

        var quarterly = orders
            .GroupBy(x => new { x.OrderDate!.Value.Year, Quarter = ((x.OrderDate.Value.Month - 1) / 3) + 1 })
            .OrderBy(x => x.Key.Year)
            .ThenBy(x => x.Key.Quarter)
            .Select(x => Point($"Q{x.Key.Quarter}/{x.Key.Year}", x.Sum(o => o.Revenue), "#0b665e"))
            .ToList();

        var colors = new[] { "#0d9488", "#14b8a6", "#0f766e", "#0b665e", "#ccfbf1", "#f59e0b", "#ef4444" };
        var statusPie = orders
            .GroupBy(x => x.OrderStatus)
            .OrderByDescending(x => x.Count())
            .Select((x, index) => Point(x.Key.Vi(), x.Count(), colors[index % colors.Length]))
            .ToList();

        var orderCountMax = daily.Concat(weekly).Select(x => x.Value).DefaultIfEmpty(1).Max();
        var revenueMax = monthly.Concat(quarterly).Select(x => x.Value).DefaultIfEmpty(1).Max();
        return View(new AnalyticsViewModel
        {
            DailyOrders = daily,
            WeeklyOrders = weekly,
            MonthlyOrders = monthly,
            QuarterlyOrders = quarterly,
            StatusPie = statusPie,
            MaxBarValue = Math.Max(1, Math.Max(orderCountMax, revenueMax)),
            MaxOrderCountValue = Math.Max(1, orderCountMax),
            MaxRevenueValue = Math.Max(1, revenueMax)
        });
    }

    public async Task<IActionResult> Leads(string? status)
    {
        var query = db.CrmLeads
            .Include(x => x.AssignedUser)
            .Include(x => x.Product)
            .Include(x => x.Campaign)
            .Include(x => x.CallAttempts)
            .AsQueryable();

        if (Enum.TryParse<LeadStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(x => x.LeadStatus == parsedStatus);
        }

        ViewBag.Status = status;
        return View(await query.OrderByDescending(x => x.LeadDate).Take(100).ToListAsync());
    }

    private async Task<Customer?> GetOrCreateCustomerAsync(string? phone, string? name)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var customer = await db.Customers.FirstOrDefaultAsync(x => x.PhoneNormalized == phone);
        if (customer is not null)
        {
            if (string.IsNullOrWhiteSpace(customer.CustomerName))
            {
                customer.CustomerName = name;
            }
            return customer;
        }

        customer = new Customer { PhoneNormalized = phone, CustomerName = name };
        db.Customers.Add(customer);
        return customer;
    }

    private async Task<MarketingCampaign?> GetOrCreateCampaignAsync(string? page, string? content, string? product)
    {
        if (string.IsNullOrWhiteSpace(page) && string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var campaign = await db.MarketingCampaigns.FirstOrDefaultAsync(x => x.PageName == page && x.ContentName == content);
        if (campaign is not null)
        {
            return campaign;
        }

        campaign = new MarketingCampaign
        {
            CampaignName = string.IsNullOrWhiteSpace(content) ? page ?? "Nguồn mới" : $"{page} - {content}",
            PageName = page,
            ContentName = content,
            ProductFocus = product
        };
        db.MarketingCampaigns.Add(campaign);
        return campaign;
    }

    private static void UpsertCall(CrmLead lead, int attemptNo, string? result, DateOnly? date, string? note, int? minutes)
    {
        var hasData = !string.IsNullOrWhiteSpace(result) || !string.IsNullOrWhiteSpace(note) || date is not null || minutes is not null;
        if (!hasData)
        {
            return;
        }

        var call = lead.CallAttempts.FirstOrDefault(x => x.AttemptNo == attemptNo);
        if (call is null)
        {
            call = new CallAttempt { Lead = lead, AttemptNo = attemptNo };
            lead.CallAttempts.Add(call);
        }

        call.ResultRaw = result;
        call.ResultStatus = lead.LeadStatus;
        call.CallDate = date;
        call.PainNote = note;
        call.DurationSeconds = minutes.HasValue ? minutes.Value * 60 : null;
    }

    private static string? NormalizePhone(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("84") && digits.Length >= 10)
        {
            digits = "0" + digits[2..];
        }
        if (digits.Length == 9)
        {
            digits = "0" + digits;
        }
        return digits;
    }

    [Authorize(Roles = "Admin,Manager,Sale,Warehouse,Shipping,Finance")]
    public async Task<IActionResult> Orders(string? status)
    {
        var query = db.SalesOrders
            .Include(x => x.Customer)
            .Include(x => x.SaleUser)
            .Include(x => x.Items)
            .AsQueryable();

        if (Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(x => x.OrderStatus == parsedStatus);
        }

        ViewBag.Status = status;
        return View(await query.OrderByDescending(x => x.OrderDate).Take(100).ToListAsync());
    }

    [Authorize(Roles = "Admin,Manager,Warehouse,Sale")]
    public async Task<IActionResult> Products()
    {
        var movements = await db.InventoryMovements.ToListAsync();
        var products = await db.Products
            .OrderBy(x => x.Category)
            .ThenBy(x => x.ProductName)
            .ToListAsync();

        var rows = products
            .Select(product => new InventorySummaryRow
            {
                ProductName = product.ProductName,
                Sku = product.Sku,
                CurrentQty = movements
                    .Where(movement => movement.ProductId == product.Id)
                    .Sum(movement => movement.MovementType == InventoryMovementType.SaleOut || movement.MovementType == InventoryMovementType.DefectOut
                        ? -movement.Quantity
                        : movement.Quantity)
            })
            .ToList();

        return View(rows);
    }

    [Authorize(Roles = "Admin,Manager,Shipping,Support")]
    public async Task<IActionResult> Shipments()
    {
        var shipments = await db.Shipments
            .Include(x => x.Order)
            .ThenInclude(x => x!.Customer)
            .OrderByDescending(x => x.ShippedAt)
            .Take(100)
            .ToListAsync();

        return View(shipments);
    }

    [Authorize(Roles = "Admin,Manager,Support")]
    public async Task<IActionResult> Support()
    {
        var tickets = await db.SupportTickets
            .Include(x => x.Order)
            .Include(x => x.Customer)
            .Include(x => x.AssignedUser)
            .OrderByDescending(x => x.OpenedAt)
            .Take(100)
            .ToListAsync();

        return View(tickets);
    }

    public async Task<IActionResult> Imports()
    {
        return View(await db.ImportBatches.OrderByDescending(x => x.BatchName).ToListAsync());
    }

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Accounts()
    {
        return View(await db.AppUsers.OrderBy(x => x.Role).ThenBy(x => x.DisplayName).ToListAsync());
    }

    private Guid? CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private UserRole? CurrentUserRole()
    {
        var value = User.FindFirstValue(ClaimTypes.Role);
        return Enum.TryParse<UserRole>(value, out var role) ? role : null;
    }

    private async Task NotifyRoleAsync(UserRole role, string title, string message, string entityType, Guid? entityId)
    {
        db.WorkNotifications.Add(new WorkNotification
        {
            TargetRole = role,
            Title = title,
            Message = message,
            EntityType = entityType,
            EntityId = entityId,
            Status = NotificationStatus.Unread
        });
        await Task.CompletedTask;
    }

    private async Task AuditAsync(string entityType, Guid? entityId, string action, string? oldValue, string? newValue)
    {
        var actorId = CurrentUserId();
        db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            ActorRole = CurrentUserRole(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValue = oldValue,
            NewValue = newValue
        });
        await Task.CompletedTask;
    }

    private static List<RoleTaskRow> BuildRoleTasks() =>
    [
        new()
        {
            RoleName = "Marketing",
            Mission = "Tạo số chất lượng để Sale bán được nhiều hơn.",
            MainScreen = "Nhập lead, Lead & cuộc gọi, Biểu đồ",
            SuccessMetric = "Số lead mới, số nóng, CPL, doanh số theo page/content",
            Tasks = ["Chạy quảng cáo", "Nhập số khách", "Gắn page/content/sản phẩm", "Báo nguồn kém chất lượng cho quản lý"]
        },
        new()
        {
            RoleName = "Sale",
            Mission = "Gọi nhanh, hiểu nhu cầu, chốt đơn và điền đủ thông tin giao hàng.",
            MainScreen = "Lead & cuộc gọi, Tạo đơn",
            SuccessMetric = "Tỷ lệ gọi đúng hạn, tỷ lệ chốt, doanh thu",
            Tasks = ["Nhận lead", "Gọi lần 1-4", "Ghi nỗi đau khách hàng", "Chốt đơn", "Tạo thông tin vận đơn cho kho"]
        },
        new()
        {
            RoleName = "Kho",
            Mission = "Đóng hàng đúng, đủ, nhanh để không mất đơn.",
            MainScreen = "Đơn hàng, Sản phẩm & kho",
            SuccessMetric = "Đơn đóng trong ngày, tỷ lệ thiếu hàng",
            Tasks = ["Nhận đơn chờ đóng", "Kiểm tồn", "Đóng hàng", "Báo thiếu hàng"]
        },
        new()
        {
            RoleName = "Vận chuyển",
            Mission = "Cập nhật mã vận đơn và cứu đơn giao lỗi kịp thời.",
            MainScreen = "Vận đơn",
            SuccessMetric = "Tỷ lệ giao thành công, đơn lỗi/hoàn",
            Tasks = ["Nhận đơn đã đóng", "Nhập mã vận đơn", "Cập nhật trạng thái giao", "Báo lỗi vận chuyển"]
        },
        new()
        {
            RoleName = "CSKH",
            Mission = "Hỗ trợ khách nhận hàng, giảm hoàn, tăng mua lại.",
            MainScreen = "Hỗ trợ nhận hàng",
            SuccessMetric = "Ticket xử lý xong, đơn Done, tỷ lệ hoàn giảm",
            Tasks = ["Gọi nhắc nhận hàng", "Xử lý khiếu nại", "Chuyển vấn đề cho bộ phận liên quan", "Đóng ticket"]
        },
        new()
        {
            RoleName = "Quản lý",
            Mission = "Nhận cảnh báo, điều phối đội nhóm, tối ưu doanh thu.",
            MainScreen = "Workflow, AI local, Dashboard, Báo cáo",
            SuccessMetric = "Doanh thu ngày/tuần/tháng/quý, tỷ lệ chốt, tốc độ xử lý",
            Tasks = ["Theo dõi thông báo", "Xem AI gợi ý", "Điều phối lead nóng", "Xử lý ngoại lệ"]
        }
    ];
}
