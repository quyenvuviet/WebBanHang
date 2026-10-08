using BanHangcoiWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace BanHangcoiWeb.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<MarketingCampaign> MarketingCampaigns => Set<MarketingCampaign>();
    public DbSet<CrmLead> CrmLeads => Set<CrmLead>();
    public DbSet<CallAttempt> CallAttempts => Set<CallAttempt>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<WorkNotification> WorkNotifications => Set<WorkNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>().HasIndex(x => x.PhoneNormalized).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(x => x.Sku).IsUnique();
        modelBuilder.Entity<Shipment>().HasIndex(x => x.TrackingCode).IsUnique().HasFilter("[TrackingCode] IS NOT NULL");
        modelBuilder.Entity<CrmLead>().HasIndex(x => new { x.LeadStatus, x.AssignedUserId });
        modelBuilder.Entity<SalesOrder>().HasIndex(x => x.OrderStatus);
        modelBuilder.Entity<SalesOrder>().HasIndex(x => x.PhoneNormalized);
        modelBuilder.Entity<WorkNotification>().HasIndex(x => new { x.Status, x.TargetRole });

        modelBuilder.Entity<SalesOrder>()
            .HasMany(x => x.Items)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Customer>()
            .HasMany(x => x.Leads)
            .WithOne(x => x.Customer)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
