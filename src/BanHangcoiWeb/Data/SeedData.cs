using BanHangcoiWeb.Models;
using BanHangcoiWeb.Services;
using Microsoft.EntityFrameworkCore;

namespace BanHangcoiWeb.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync();
        await EnsureMutableSchemaAsync(db);

        if (await db.AppUsers.AnyAsync())
        {
            await EnsureDefaultAccountsAsync(db);
            return;
        }

        var admin = new AppUser { DisplayName = "Admin", LoginName = "admin", Role = UserRole.Admin, Email = "admin@banhangcoi.local", PasswordHash = PasswordService.Hash("Admin@123") };
        var manager = new AppUser { DisplayName = "Trưởng sale", LoginName = "manager", Role = UserRole.Manager, Email = "manager@banhangcoi.local", PasswordHash = PasswordService.Hash("Manager@123") };
        var marketing = new AppUser { DisplayName = "Marketing", LoginName = "marketing", Role = UserRole.Marketing, Email = "marketing@banhangcoi.local", PasswordHash = PasswordService.Hash("Marketing@123") };
        var sale = new AppUser { DisplayName = "Sale Hồng", LoginName = "sale.hong", Role = UserRole.Sale, TeamName = "Sale phụ tùng", PasswordHash = PasswordService.Hash("Sale@123") };
        var warehouse = new AppUser { DisplayName = "Kho vận", LoginName = "kho", Role = UserRole.Warehouse, TeamName = "Kho", PasswordHash = PasswordService.Hash("Kho@123") };
        var support = new AppUser { DisplayName = "CSKH", LoginName = "support", Role = UserRole.Support, TeamName = "Hỗ trợ", PasswordHash = PasswordService.Hash("Support@123") };
        db.AppUsers.AddRange(admin, manager, marketing, sale, warehouse, support);

        var products = new[]
        {
            new Product { Sku = "COI-70", ProductName = "Còi hơi 70cm", NormalizedName = "coi hoi 70cm", Category = "Còi hơi", Voltage = "12V/24V" },
            new Product { Sku = "COI-1M", ProductName = "Còi hơi 1m", NormalizedName = "coi hoi 1m", Category = "Còi hơi", Voltage = "12V/24V" },
            new Product { Sku = "DEM-CONG-THAI", ProductName = "Đệm ghế công thái học", NormalizedName = "dem ghe cong thai hoc", Category = "Đệm ghế" },
            new Product { Sku = "DE-DO", ProductName = "Đế đỏ + role", NormalizedName = "de do role", Category = "Phụ kiện" }
        };
        db.Products.AddRange(products);

        var campaign = new MarketingCampaign
        {
            CampaignName = "Lê Nga 9 - Còi hơi",
            PageName = "LÊ NGA 9",
            ContentName = "Video còi xe tải",
            ProductFocus = "Còi hơi 70cm/1m"
        };
        db.MarketingCampaigns.Add(campaign);

        var customer1 = new Customer
        {
            CustomerName = "Nguyễn Anh Quang",
            PhoneNormalized = "0904867368",
            AddressRaw = "Hà Nội",
            FirstSourceFile = "PTOTdữ liệu.xlsx",
            FirstSourceSheet = "Nhân viên B",
            FirstSourceRow = 2
        };
        var customer2 = new Customer
        {
            CustomerName = "Phạm Văn Thành",
            PhoneNormalized = "0934325786",
            AddressRaw = "Nam Định",
            FirstSourceFile = "Đi đơn tháng 8.xlsx",
            FirstSourceSheet = "Danh sách đơn",
            FirstSourceRow = 2
        };
        db.Customers.AddRange(customer1, customer2);

        var lead1 = new CrmLead
        {
            Customer = customer1,
            AssignedUser = sale,
            Campaign = campaign,
            LeadDate = new DateOnly(2026, 8, 1),
            LeadStatus = LeadStatus.HighIntent,
            PhoneNormalized = customer1.PhoneNormalized,
            CustomerNameRaw = customer1.CustomerName,
            PageName = campaign.PageName,
            ContentName = campaign.ContentName,
            LeadType = "SỐ NÓNG",
            ProductInterestRaw = "70 - 1M",
            Product = products[0],
            SaleNameRaw = "HỒNG",
            NextActionAt = DateTimeOffset.Now.AddHours(2),
            NextActionNote = "Khách hỏi giá, cần gọi lại xác nhận điện áp.",
            SourceFile = "PTOTdữ liệu.xlsx",
            SourceSheet = "Nhân viên B",
            SourceRow = 2
        };
        db.CrmLeads.Add(lead1);

        db.CallAttempts.Add(new CallAttempt
        {
            Lead = lead1,
            AttemptNo = 1,
            CallDate = new DateOnly(2026, 8, 1),
            DurationSeconds = 260,
            ResultRaw = "Nhu cầu cao",
            ResultStatus = LeadStatus.HighIntent,
            PainNote = "Xe tải, muốn còi to, đang cân nhắc giá."
        });

        var order1 = new SalesOrder
        {
            Lead = lead1,
            Customer = customer1,
            SaleUser = sale,
            OrderStatus = OrderStatus.Shipping,
            OrderDate = new DateOnly(2026, 8, 1),
            CustomerNameRaw = customer1.CustomerName,
            PhoneNormalized = customer1.PhoneNormalized,
            AddressRaw = customer1.AddressRaw,
            CodAmountVnd = 1_650_000,
            ShippingFeeVnd = 35_000,
            TotalAmountVnd = 1_650_000,
            NoteRaw = "Khách lấy còi 70cm, xe 24V.",
            SourceFile = "Đi đơn tháng 8.xlsx",
            SourceSheet = "Danh sách đơn",
            SourceRow = 2
        };
        db.SalesOrders.Add(order1);
        db.OrderItems.Add(new OrderItem
        {
            Order = order1,
            Product = products[0],
            ProductNameRaw = "Còi hơi 70cm",
            Quantity = 1,
            UnitPriceVnd = 1_650_000,
            LineAmountVnd = 1_650_000
        });
        db.Shipments.Add(new Shipment
        {
            Order = order1,
            TrackingCode = "GY8CVBRH",
            CarrierName = "GHN",
            ShipmentStatus = OrderStatus.Shipping,
            ShippedAt = DateTimeOffset.Now.AddDays(-1),
            DeliveryIssueNote = "Đang giao, cần theo dõi nếu delay."
        });

        db.SupportTickets.Add(new SupportTicket
        {
            Order = order1,
            Customer = customer1,
            AssignedUser = support,
            Status = TicketStatus.Open,
            IssueType = "Hỗ trợ nhận hàng",
            IssueNote = "Gọi khách nhắc nhận hàng khi ship liên hệ."
        });

        db.InventoryMovements.AddRange(
            new InventoryMovement { Product = products[0], MovementType = InventoryMovementType.Opening, MovementDate = new DateOnly(2026, 8, 1), Quantity = 50, Note = "Tồn đầu kỳ" },
            new InventoryMovement { Product = products[0], MovementType = InventoryMovementType.SaleOut, MovementDate = new DateOnly(2026, 8, 1), Quantity = 1, Order = order1, Note = "Xuất bán" },
            new InventoryMovement { Product = products[2], MovementType = InventoryMovementType.Opening, MovementDate = new DateOnly(2026, 8, 1), Quantity = 35, Note = "Tồn đầu kỳ" }
        );

        db.ImportBatches.Add(new ImportBatch
        {
            BatchName = "Import mẫu tháng 8",
            SourceFile = "Đi đơn tháng 8.xlsx",
            Status = "accepted",
            RawRows = 3440,
            StagedRows = 3440,
            AcceptedRows = 3301,
            RejectedRows = 139,
            Notes = "Dữ liệu mẫu mô phỏng theo workbook nguồn."
        });

        await db.SaveChangesAsync();
    }

    private static async Task EnsureMutableSchemaAsync(AppDbContext db)
    {
        var sql = """
IF COL_LENGTH('AppUsers', 'LoginName') IS NULL ALTER TABLE AppUsers ADD LoginName nvarchar(80) NULL;
IF COL_LENGTH('AppUsers', 'PasswordHash') IS NULL ALTER TABLE AppUsers ADD PasswordHash nvarchar(128) NULL;
IF COL_LENGTH('CrmLeads', 'WorkAt') IS NULL ALTER TABLE CrmLeads ADD WorkAt datetimeoffset NULL;
IF COL_LENGTH('CrmLeads', 'FloatingDays') IS NULL ALTER TABLE CrmLeads ADD FloatingDays int NULL;
IF COL_LENGTH('CrmLeads', 'DealValueVnd') IS NULL ALTER TABLE CrmLeads ADD DealValueVnd bigint NULL;
IF COL_LENGTH('CrmLeads', 'SaleNote') IS NULL ALTER TABLE CrmLeads ADD SaleNote nvarchar(500) NULL;
IF COL_LENGTH('CrmLeads', 'SaleLevel') IS NULL ALTER TABLE CrmLeads ADD SaleLevel nvarchar(80) NULL;
IF COL_LENGTH('CrmLeads', 'PurchaseReason') IS NULL ALTER TABLE CrmLeads ADD PurchaseReason nvarchar(500) NULL;
IF COL_LENGTH('SalesOrders', 'ReceiverName') IS NULL ALTER TABLE SalesOrders ADD ReceiverName nvarchar(80) NULL;
IF COL_LENGTH('SalesOrders', 'ReceiverPhone') IS NULL ALTER TABLE SalesOrders ADD ReceiverPhone nvarchar(30) NULL;
IF COL_LENGTH('SalesOrders', 'ShippingAddress') IS NULL ALTER TABLE SalesOrders ADD ShippingAddress nvarchar(500) NULL;
IF COL_LENGTH('SalesOrders', 'PackingNote') IS NULL ALTER TABLE SalesOrders ADD PackingNote nvarchar(500) NULL;
IF OBJECT_ID('WorkNotifications', 'U') IS NULL
BEGIN
    CREATE TABLE WorkNotifications (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_WorkNotifications PRIMARY KEY,
        Title nvarchar(160) NOT NULL,
        Message nvarchar(1000) NULL,
        TargetUserId uniqueidentifier NULL,
        TargetRole int NULL,
        EntityType nvarchar(80) NULL,
        EntityId uniqueidentifier NULL,
        Status int NOT NULL DEFAULT 0,
        CreatedAt datetimeoffset NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        ReadAt datetimeoffset NULL,
        CONSTRAINT FK_WorkNotifications_AppUsers_TargetUserId FOREIGN KEY (TargetUserId) REFERENCES AppUsers(Id)
    );
    CREATE INDEX IX_WorkNotifications_Status_TargetRole ON WorkNotifications(Status, TargetRole);
END;
""";
        await db.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task EnsureDefaultAccountsAsync(AppDbContext db)
    {
        var accounts = new[]
        {
            ("admin", "Admin", UserRole.Admin, "Admin@123"),
            ("manager", "Trưởng sale", UserRole.Manager, "Manager@123"),
            ("marketing", "Marketing", UserRole.Marketing, "Marketing@123"),
            ("sale.hong", "Sale Hồng", UserRole.Sale, "Sale@123"),
            ("sale.phuong", "Sale Phương", UserRole.Sale, "Sale@123"),
            ("sale.giang", "Sale Giang", UserRole.Sale, "Sale@123"),
            ("kho", "Kho vận", UserRole.Warehouse, "Kho@123"),
            ("shipping", "Vận chuyển", UserRole.Shipping, "Shipping@123"),
            ("support", "CSKH", UserRole.Support, "Support@123"),
            ("finance", "Kế toán", UserRole.Finance, "Finance@123")
        };

        foreach (var (login, name, role, password) in accounts)
        {
            var user = await db.AppUsers.FirstOrDefaultAsync(x => x.LoginName == login || x.DisplayName == name);
            if (user is null)
            {
                db.AppUsers.Add(new AppUser
                {
                    LoginName = login,
                    DisplayName = name,
                    Role = role,
                    PasswordHash = PasswordService.Hash(password),
                    TeamName = role == UserRole.Sale ? "Sale phụ tùng" : null,
                    IsActive = true
                });
            }
            else
            {
                user.LoginName ??= login;
                user.PasswordHash ??= PasswordService.Hash(password);
                user.Role = role;
                user.IsActive = true;
            }
        }

        await db.SaveChangesAsync();
    }
}
