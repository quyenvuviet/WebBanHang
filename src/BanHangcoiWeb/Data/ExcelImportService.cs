using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BanHangcoiWeb.Models;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace BanHangcoiWeb.Data;

public class ExcelImportService(AppDbContext db, ILogger<ExcelImportService> logger)
{
    private static readonly string Root = @"D:\AI_AGENCY_AGENTS\BanHangcoi";
    private readonly Dictionary<string, Customer> _customers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Product> _products = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AppUser> _users = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, MarketingCampaign> _campaigns = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _trackingCodes = new(StringComparer.OrdinalIgnoreCase);

    public async Task ImportIfNeededAsync()
    {
        var hasActualImport = await db.ImportBatches.AnyAsync(x =>
            x.BatchName == "PTOT dữ liệu - lead/call" || x.BatchName == "Đi đơn tháng 8 - đơn/vận/kho");

        if (hasActualImport)
        {
            return;
        }

        logger.LogInformation("Starting Excel import for BanHangcoi workbooks.");
        await ClearDemoDataAsync();
        await WarmCachesAsync();

        await ImportPtotLeadsAsync(Path.Combine(Root, "PTOTdữ liệu.xlsx"));
        await ImportAugustOrdersAsync(Path.Combine(Root, "Đi đơn tháng 8.xlsx"));

        logger.LogInformation("Excel import finished.");
    }

    private async Task ClearDemoDataAsync()
    {
        await db.SupportTickets.ExecuteDeleteAsync();
        await db.Shipments.ExecuteDeleteAsync();
        await db.InventoryMovements.ExecuteDeleteAsync();
        await db.OrderItems.ExecuteDeleteAsync();
        await db.SalesOrders.ExecuteDeleteAsync();
        await db.CallAttempts.ExecuteDeleteAsync();
        await db.CrmLeads.ExecuteDeleteAsync();
        await db.Customers.ExecuteDeleteAsync();
        await db.MarketingCampaigns.ExecuteDeleteAsync();
        await db.Products.ExecuteDeleteAsync();
        await db.ImportBatches.ExecuteDeleteAsync();
    }

    private async Task WarmCachesAsync()
    {
        foreach (var user in await db.AppUsers.ToListAsync())
        {
            _users[NormalizeKey(user.DisplayName)] = user;
        }
    }

    private async Task ImportPtotLeadsAsync(string path)
    {
        var batch = new ImportBatch
        {
            BatchName = "PTOT dữ liệu - lead/call",
            SourceFile = path,
            Status = "importing"
        };
        db.ImportBatches.Add(batch);

        using var workbook = new XLWorkbook(path);
        var leadSheets = workbook.Worksheets
            .Where(x => IsLeadSheet(x.Name))
            .ToList();

        var rawRows = 0;
        var acceptedRows = 0;
        foreach (var sheet in leadSheets)
        {
            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
            for (var row = 2; row <= lastRow; row++)
            {
                rawRows++;
                var phone = NormalizePhone(CellText(sheet, row, 3));
                var name = CellText(sheet, row, 2);
                var productRaw = CellText(sheet, row, 8);
                var hasUsefulData = !string.IsNullOrWhiteSpace(phone) || !string.IsNullOrWhiteSpace(name) || !string.IsNullOrWhiteSpace(productRaw);
                if (!hasUsefulData)
                {
                    continue;
                }

                var customer = await GetOrCreateCustomerAsync(phone, name, null, path, sheet.Name, row);
                var sale = await GetOrCreateUserAsync(CellText(sheet, row, 11), UserRole.Sale);
                var campaign = await GetOrCreateCampaignAsync(CellText(sheet, row, 6), CellText(sheet, row, 7), productRaw);
                var product = await GetOrCreateProductAsync(productRaw);
                var status = MapLeadStatus(CellText(sheet, row, 12), CellText(sheet, row, 14), CellText(sheet, row, 34));

                var lead = new CrmLead
                {
                    Customer = customer,
                    AssignedUser = sale,
                    Campaign = campaign,
                    LeadDate = ParseDate(CellText(sheet, row, 1), sheet.Cell(row, 1).Value),
                    LeadStatus = status,
                    PhoneNormalized = phone,
                    CustomerNameRaw = name,
                    PageName = campaign?.PageName,
                    ContentName = campaign?.ContentName,
                    LeadType = CellText(sheet, row, 4),
                    Occupation = CellText(sheet, row, 5),
                    ProductInterestRaw = productRaw,
                    Product = product,
                    VehicleAirType = CellText(sheet, row, 9),
                    VehicleType = CellText(sheet, row, 10),
                    SaleNameRaw = CellText(sheet, row, 11),
                    NextActionAt = GuessNextAction(status),
                    NextActionNote = CellText(sheet, row, 34),
                    SourceFile = Path.GetFileName(path),
                    SourceSheet = sheet.Name,
                    SourceRow = row
                };
                db.CrmLeads.Add(lead);

                AddCallAttempts(sheet, row, lead, Path.GetFileName(path), sheet.Name);
                acceptedRows++;

                if (acceptedRows % 500 == 0)
                {
                    await db.SaveChangesAsync();
                }
            }
        }

        batch.RawRows = rawRows;
        batch.StagedRows = rawRows;
        batch.AcceptedRows = acceptedRows;
        batch.RejectedRows = rawRows - acceptedRows;
        batch.Status = "accepted";
        batch.Notes = $"Imported {acceptedRows:N0} lead rows from {leadSheets.Count} sheets.";
        await db.SaveChangesAsync();
    }

    private async Task ImportAugustOrdersAsync(string path)
    {
        var batch = new ImportBatch
        {
            BatchName = "Đi đơn tháng 8 - đơn/vận/kho",
            SourceFile = path,
            Status = "importing"
        };
        db.ImportBatches.Add(batch);

        using var workbook = new XLWorkbook(path);
        var accepted = await ImportOrdersSheetAsync(workbook.Worksheet("Danh sách đơn"), path);
        var stockRows = await ImportInventorySheetsAsync(workbook, path);

        batch.RawRows = accepted.RawRows + stockRows.RawRows;
        batch.StagedRows = batch.RawRows;
        batch.AcceptedRows = accepted.AcceptedRows + stockRows.AcceptedRows;
        batch.RejectedRows = batch.RawRows - batch.AcceptedRows;
        batch.Status = "accepted";
        batch.Notes = $"Imported {accepted.AcceptedRows:N0} order rows and {stockRows.AcceptedRows:N0} inventory rows.";
        await db.SaveChangesAsync();
    }

    private async Task<(int RawRows, int AcceptedRows)> ImportOrdersSheetAsync(IXLWorksheet sheet, string path)
    {
        var rawRows = 0;
        var accepted = 0;
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var row = 2; row <= lastRow; row++)
        {
            rawRows++;
            var phone = NormalizePhone(CellText(sheet, row, 4));
            var productRaw = CellText(sheet, row, 8);
            var customerName = CellText(sheet, row, 3);
            var trackingCode = CellText(sheet, row, 15).Trim().ToUpperInvariant();
            var cod = ParseMoney(CellText(sheet, row, 11));
            var hasData = !string.IsNullOrWhiteSpace(phone) || !string.IsNullOrWhiteSpace(productRaw) || cod > 0 || !string.IsNullOrWhiteSpace(trackingCode);
            if (!hasData)
            {
                continue;
            }

            var customer = await GetOrCreateCustomerAsync(phone, customerName, CellText(sheet, row, 5), path, sheet.Name, row);
            var product = await GetOrCreateProductAsync(productRaw);
            var sale = await GetOrCreateUserAsync(CellText(sheet, row, 10), UserRole.Sale);
            var status = MapOrderStatus(CellText(sheet, row, 17), CellText(sheet, row, 14));
            var order = new SalesOrder
            {
                Customer = customer,
                SaleUser = sale,
                OrderStatus = status,
                OrderDate = ParseDate(CellText(sheet, row, 1), sheet.Cell(row, 1).Value),
                CustomerNameRaw = customerName,
                PhoneNormalized = phone,
                AddressRaw = CellText(sheet, row, 5),
                ComboRaw = CellText(sheet, row, 6),
                CodAmountVnd = cod,
                ShippingFeeVnd = ParseMoney(CellText(sheet, row, 12)),
                TotalAmountVnd = cod,
                NoteRaw = CellText(sheet, row, 22),
                SourceFile = Path.GetFileName(path),
                SourceSheet = sheet.Name,
                SourceRow = row
            };
            db.SalesOrders.Add(order);

            db.OrderItems.Add(new OrderItem
            {
                Order = order,
                Product = product,
                ProductNameRaw = string.IsNullOrWhiteSpace(productRaw) ? CellText(sheet, row, 6) : productRaw,
                Quantity = ParseInt(CellText(sheet, row, 9), 1),
                UnitPriceVnd = cod,
                LineAmountVnd = cod
            });

            if (!string.IsNullOrWhiteSpace(trackingCode) && !_trackingCodes.Contains(trackingCode))
            {
                _trackingCodes.Add(trackingCode);
                db.Shipments.Add(new Shipment
                {
                    Order = order,
                    TrackingCode = trackingCode,
                    CarrierName = CellText(sheet, row, 16),
                    ShipmentStatus = status,
                    DeliveredAt = ParseDate(CellText(sheet, row, 18), sheet.Cell(row, 18).Value)?.ToDateTime(TimeOnly.MinValue),
                    ReturnReason = CellText(sheet, row, 20),
                    DeliveryIssueNote = CellText(sheet, row, 19)
                });
            }

            if (product is not null)
            {
                db.InventoryMovements.Add(new InventoryMovement
                {
                    Product = product,
                    Order = order,
                    MovementType = status == OrderStatus.Returned ? InventoryMovementType.ReturnIn : InventoryMovementType.SaleOut,
                    MovementDate = ParseDate(CellText(sheet, row, 1), sheet.Cell(row, 1).Value) ?? new DateOnly(2026, 8, 1),
                    Quantity = ParseInt(CellText(sheet, row, 9), 1),
                    Note = status == OrderStatus.Returned ? "Hàng hoàn từ đơn Excel" : "Xuất bán từ đơn Excel"
                });
            }

            if (status is OrderStatus.DeliveryIssue or OrderStatus.Returned)
            {
                db.SupportTickets.Add(new SupportTicket
                {
                    Order = order,
                    Customer = customer,
                    Status = TicketStatus.Open,
                    IssueType = status == OrderStatus.Returned ? "Đơn hoàn" : "Hỗ trợ nhận hàng",
                    IssueNote = $"{CellText(sheet, row, 19)} {CellText(sheet, row, 20)}".Trim()
                });
            }

            accepted++;
            if (accepted % 500 == 0)
            {
                await db.SaveChangesAsync();
            }
        }

        await db.SaveChangesAsync();
        return (rawRows, accepted);
    }

    private async Task<(int RawRows, int AcceptedRows)> ImportInventorySheetsAsync(XLWorkbook workbook, string path)
    {
        var raw = 0;
        var accepted = 0;
        foreach (var sheetName in new[] { "Nhập hàng", "Hàng Hoàn" })
        {
            if (!workbook.TryGetWorksheet(sheetName, out var sheet))
            {
                continue;
            }

            var movementType = sheetName == "Nhập hàng" ? InventoryMovementType.PurchaseIn : InventoryMovementType.ReturnIn;
            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
            var lastCol = Math.Min(sheet.LastColumnUsed()?.ColumnNumber() ?? 1, 33);
            for (var row = 2; row <= lastRow; row++)
            {
                var productName = CellText(sheet, row, 1);
                if (string.IsNullOrWhiteSpace(productName))
                {
                    continue;
                }

                var product = await GetOrCreateProductAsync(productName);
                for (var col = 2; col <= lastCol; col++)
                {
                    raw++;
                    var qty = ParseInt(CellText(sheet, row, col), 0);
                    if (qty == 0)
                    {
                        continue;
                    }

                    db.InventoryMovements.Add(new InventoryMovement
                    {
                        Product = product,
                        MovementType = movementType,
                        MovementDate = ParseDate(CellText(sheet, 1, col), sheet.Cell(1, col).Value) ?? new DateOnly(2026, 8, Math.Min(28, col - 1)),
                        Quantity = qty,
                        Note = sheetName
                    });
                    accepted++;
                }
            }
        }

        await db.SaveChangesAsync();
        return (raw, accepted);
    }

    private void AddCallAttempts(IXLWorksheet sheet, int row, CrmLead lead, string sourceFile, string sourceSheet)
    {
        var specs = new[]
        {
            (Attempt: 1, ResultCol: 12, DateCol: 13, NoteCol: 14, DurationCol: 15),
            (Attempt: 2, ResultCol: 16, DateCol: 17, NoteCol: 18, DurationCol: 19),
            (Attempt: 3, ResultCol: 20, DateCol: 21, NoteCol: 22, DurationCol: 23),
            (Attempt: 4, ResultCol: 24, DateCol: 25, NoteCol: 26, DurationCol: 27)
        };

        foreach (var spec in specs)
        {
            var result = CellText(sheet, row, spec.ResultCol);
            var note = CellText(sheet, row, spec.NoteCol);
            var date = ParseDate(CellText(sheet, row, spec.DateCol), sheet.Cell(row, spec.DateCol).Value);
            if (string.IsNullOrWhiteSpace(result) && string.IsNullOrWhiteSpace(note) && date is null)
            {
                continue;
            }

            db.CallAttempts.Add(new CallAttempt
            {
                Lead = lead,
                AttemptNo = spec.Attempt,
                CallDate = date,
                DurationSeconds = ParseDurationSeconds(CellText(sheet, row, spec.DurationCol)),
                ResultRaw = result,
                ResultStatus = MapLeadStatus(result, note, null),
                PainNote = note
            });
        }
    }

    private async Task<Customer?> GetOrCreateCustomerAsync(string? phone, string? name, string? address, string sourceFile, string sourceSheet, int sourceRow)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        if (_customers.TryGetValue(phone, out var cached))
        {
            if (string.IsNullOrWhiteSpace(cached.CustomerName) && !string.IsNullOrWhiteSpace(name))
            {
                cached.CustomerName = name;
            }
            if (string.IsNullOrWhiteSpace(cached.AddressRaw) && !string.IsNullOrWhiteSpace(address))
            {
                cached.AddressRaw = address;
            }
            return cached;
        }

        var existing = await db.Customers.FirstOrDefaultAsync(x => x.PhoneNormalized == phone);
        if (existing is not null)
        {
            _customers[phone] = existing;
            return existing;
        }

        var customer = new Customer
        {
            PhoneNormalized = phone,
            CustomerName = name,
            AddressRaw = address,
            FirstSourceFile = Path.GetFileName(sourceFile),
            FirstSourceSheet = sourceSheet,
            FirstSourceRow = sourceRow
        };
        db.Customers.Add(customer);
        _customers[phone] = customer;
        return customer;
    }

    private async Task<AppUser?> GetOrCreateUserAsync(string? rawName, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return null;
        }

        var key = NormalizeKey(rawName);
        if (_users.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var user = new AppUser
        {
            DisplayName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(rawName.Trim().ToLowerInvariant()),
            Role = role,
            TeamName = role == UserRole.Sale ? "Sale phụ tùng" : null
        };
        db.AppUsers.Add(user);
        _users[key] = user;
        return user;
    }

    private async Task<Product?> GetOrCreateProductAsync(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return null;
        }

        var normalized = NormalizeKey(rawName);
        if (_products.TryGetValue(normalized, out var cached))
        {
            return cached;
        }

        var product = new Product
        {
            Sku = $"SP-{_products.Count + 1:0000}",
            ProductName = rawName.Trim(),
            NormalizedName = normalized,
            Category = GuessCategory(rawName)
        };
        db.Products.Add(product);
        _products[normalized] = product;
        return product;
    }

    private async Task<MarketingCampaign?> GetOrCreateCampaignAsync(string? page, string? content, string? productFocus)
    {
        if (string.IsNullOrWhiteSpace(page) && string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var key = NormalizeKey($"{page}|{content}");
        if (_campaigns.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var campaign = new MarketingCampaign
        {
            CampaignName = string.IsNullOrWhiteSpace(content) ? page!.Trim() : $"{page} - {content}",
            PageName = page?.Trim(),
            ContentName = content?.Trim(),
            ProductFocus = productFocus
        };
        db.MarketingCampaigns.Add(campaign);
        _campaigns[key] = campaign;
        return campaign;
    }

    private static bool IsLeadSheet(string name)
    {
        var n = name.Trim().ToLowerInvariant();
        return n.StartsWith("nhân viên")
            || n.StartsWith("nhan viên")
            || n.StartsWith("nhân viên")
            || n.Contains("kho số")
            || n.Contains("databackup");
    }

    private static string CellText(IXLWorksheet sheet, int row, int col)
    {
        return sheet.Cell(row, col).GetFormattedString().Trim();
    }

    private static string? NormalizePhone(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var digits = Regex.Replace(raw, "[^0-9]", "");
        if (digits.StartsWith("84") && digits.Length >= 10)
        {
            digits = "0" + digits[2..];
        }
        if (digits.Length == 9)
        {
            digits = "0" + digits;
        }

        return digits.Length is >= 10 and <= 11 ? digits : null;
    }

    private static DateOnly? ParseDate(string text, XLCellValue value)
    {
        if (value.IsDateTime)
        {
            return DateOnly.FromDateTime(value.GetDateTime());
        }

        if (double.TryParse(text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var serial)
            && serial > 30000
            && serial < 60000)
        {
            return DateOnly.FromDateTime(DateTime.FromOADate(serial));
        }

        if (DateTime.TryParse(text, new CultureInfo("vi-VN"), DateTimeStyles.None, out var parsed)
            || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
        {
            return DateOnly.FromDateTime(parsed);
        }

        return null;
    }

    private static long ParseMoney(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return 0;
        }

        var lower = raw.Trim().ToLowerInvariant();
        var multiplier = lower.Contains('k') ? 1000 : 1;
        lower = lower.Replace("đ", "").Replace("vnd", "").Replace("k", "");
        var digits = Regex.Replace(lower, "[^0-9]", "");
        if (!long.TryParse(digits, out var value))
        {
            return 0;
        }

        return value * multiplier;
    }

    private static int ParseInt(string? raw, int fallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        var digits = Regex.Replace(raw, "[^0-9-]", "");
        return int.TryParse(digits, out var value) ? value : fallback;
    }

    private static int? ParseDurationSeconds(string? raw)
    {
        var minutes = ParseInt(raw, 0);
        return minutes > 0 ? minutes * 60 : null;
    }

    private static LeadStatus MapLeadStatus(string? result, string? note, string? note2)
    {
        var text = NormalizeKey($"{result} {note} {note2}");
        if (text.Contains("chot")) return LeadStatus.ClosedWon;
        if (text.Contains("knm") || text.Contains("thue bao") || text.Contains("khong nghe") || text.Contains("tat may")) return LeadStatus.NoAnswer;
        if (text.Contains("hen") || text.Contains("nhan") || text.Contains("can doi") || text.Contains("xem")) return LeadStatus.FollowUp;
        if (text.Contains("nhu cau cao")) return LeadStatus.HighIntent;
        if (text.Contains("nhu cau thap")) return LeadStatus.LowIntent;
        if (text.Contains("khong quan tam") || text.Contains("k mua") || text.Contains("mua roi")) return LeadStatus.ClosedLost;
        return LeadStatus.Assigned;
    }

    private static OrderStatus MapOrderStatus(string? status, string? warehouseStatus)
    {
        var text = NormalizeKey($"{status} {warehouseStatus}");
        if (text.Contains("thanh cong")) return OrderStatus.Delivered;
        if (text.Contains("hoan")) return OrderStatus.Returned;
        if (text.Contains("huy") || text.Contains("khong di don")) return OrderStatus.Cancelled;
        if (text.Contains("dang giao") || text.Contains("van chuyen")) return OrderStatus.Shipping;
        if (text.Contains("dong hang")) return OrderStatus.Packed;
        return OrderStatus.Confirmed;
    }

    private static DateTimeOffset? GuessNextAction(LeadStatus status)
    {
        return status is LeadStatus.FollowUp or LeadStatus.NoAnswer or LeadStatus.HighIntent
            ? DateTimeOffset.Now.AddHours(4)
            : null;
    }

    private static string GuessCategory(string raw)
    {
        var key = NormalizeKey(raw);
        if (key.Contains("coi")) return "Còi hơi";
        if (key.Contains("dem") || key.Contains("ghe")) return "Đệm ghế";
        if (key.Contains("led")) return "Đèn LED";
        if (key.Contains("de do") || key.Contains("role")) return "Phụ kiện";
        return "Phụ tùng";
    }

    private static string NormalizeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return Regex.Replace(builder.ToString().Normalize(NormalizationForm.FormC), "\\s+", " ");
    }
}
