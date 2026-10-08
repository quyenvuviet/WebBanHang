using System.ComponentModel.DataAnnotations;

namespace BanHangcoiWeb.Models;

public enum UserRole { Admin, Manager, Sale, Warehouse, Shipping, Support, Marketing, Finance, QaReadonly }
public enum LeadStatus { New, Assigned, CalledOnce, FollowUp, NoAnswer, LowIntent, HighIntent, Qualified, ClosedWon, ClosedLost, Recycled, Duplicate, Invalid }
public enum OrderStatus { Draft, Confirmed, WaitingPack, Packed, HandoverToCarrier, Shipping, DeliveryIssue, Delivered, Returned, Cancelled, Done }
public enum TicketStatus { Open, InProgress, WaitingCustomer, Resolved, Closed }
public enum InventoryMovementType { Opening, PurchaseIn, SaleOut, ReturnIn, DefectOut, Adjustment }
public enum NotificationStatus { Unread, Read, Done }

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, StringLength(120)] public string DisplayName { get; set; } = "";
    [StringLength(80)] public string? LoginName { get; set; }
    [StringLength(160)] public string? Email { get; set; }
    [StringLength(30)] public string? Phone { get; set; }
    public UserRole Role { get; set; }
    [StringLength(80)] public string? TeamName { get; set; }
    public bool IsActive { get; set; } = true;
    [StringLength(128)] public string? PasswordHash { get; set; }
}

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, StringLength(20)] public string PhoneNormalized { get; set; } = "";
    [StringLength(20)] public string? PhoneRaw { get; set; }
    [StringLength(160)] public string? CustomerName { get; set; }
    [StringLength(500)] public string? AddressRaw { get; set; }
    [StringLength(80)] public string? Province { get; set; }
    [StringLength(80)] public string? District { get; set; }
    [StringLength(80)] public string? Ward { get; set; }
    public string? FirstSourceFile { get; set; }
    public string? FirstSourceSheet { get; set; }
    public int? FirstSourceRow { get; set; }
    public List<CrmLead> Leads { get; set; } = [];
}

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [StringLength(50)] public string? Sku { get; set; }
    [Required, StringLength(180)] public string ProductName { get; set; } = "";
    [Required, StringLength(180)] public string NormalizedName { get; set; } = "";
    [StringLength(80)] public string? Category { get; set; }
    [StringLength(40)] public string? Voltage { get; set; }
    [StringLength(40)] public string? Color { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MarketingCampaign
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, StringLength(160)] public string CampaignName { get; set; } = "";
    [StringLength(120)] public string? PageName { get; set; }
    [StringLength(160)] public string? ContentName { get; set; }
    [StringLength(160)] public string? ProductFocus { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CrmLead
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid? AssignedUserId { get; set; }
    public AppUser? AssignedUser { get; set; }
    public Guid? CampaignId { get; set; }
    public MarketingCampaign? Campaign { get; set; }
    public LeadStatus LeadStatus { get; set; } = LeadStatus.New;
    public DateOnly? LeadDate { get; set; }
    [StringLength(20)] public string? PhoneNormalized { get; set; }
    [StringLength(160)] public string? CustomerNameRaw { get; set; }
    [StringLength(120)] public string? PageName { get; set; }
    [StringLength(160)] public string? ContentName { get; set; }
    [StringLength(40)] public string? LeadType { get; set; }
    [StringLength(120)] public string? Occupation { get; set; }
    [StringLength(120)] public string? VehicleType { get; set; }
    [StringLength(80)] public string? VehicleAirType { get; set; }
    [StringLength(180)] public string? ProductInterestRaw { get; set; }
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    [StringLength(80)] public string? SaleNameRaw { get; set; }
    public DateTimeOffset? NextActionAt { get; set; }
    [StringLength(500)] public string? NextActionNote { get; set; }
    public DateTimeOffset? WorkAt { get; set; }
    public int? FloatingDays { get; set; }
    public long? DealValueVnd { get; set; }
    [StringLength(500)] public string? SaleNote { get; set; }
    [StringLength(80)] public string? SaleLevel { get; set; }
    [StringLength(500)] public string? PurchaseReason { get; set; }
    [Required] public string SourceFile { get; set; } = "manual";
    [Required] public string SourceSheet { get; set; } = "manual";
    public int SourceRow { get; set; }
    public List<CallAttempt> CallAttempts { get; set; } = [];
}

public class CallAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LeadId { get; set; }
    public CrmLead? Lead { get; set; }
    public int AttemptNo { get; set; }
    public DateOnly? CallDate { get; set; }
    public DateTimeOffset? CallStartedAt { get; set; }
    public int? DurationSeconds { get; set; }
    [StringLength(120)] public string? ResultRaw { get; set; }
    public LeadStatus? ResultStatus { get; set; }
    [StringLength(1000)] public string? PainNote { get; set; }
    public DateTimeOffset? NextFollowUpAt { get; set; }
}

public class SalesOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [StringLength(50)] public string? OrderCode { get; set; }
    public Guid? LeadId { get; set; }
    public CrmLead? Lead { get; set; }
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid? SaleUserId { get; set; }
    public AppUser? SaleUser { get; set; }
    public OrderStatus OrderStatus { get; set; } = OrderStatus.Draft;
    public DateOnly? OrderDate { get; set; }
    [StringLength(160)] public string? CustomerNameRaw { get; set; }
    [StringLength(20)] public string? PhoneNormalized { get; set; }
    [StringLength(500)] public string? AddressRaw { get; set; }
    [StringLength(180)] public string? ComboRaw { get; set; }
    public long? CodAmountVnd { get; set; }
    public long? ShippingFeeVnd { get; set; }
    public long DiscountVnd { get; set; }
    public long? TotalAmountVnd { get; set; }
    [StringLength(1000)] public string? NoteRaw { get; set; }
    [StringLength(80)] public string? ReceiverName { get; set; }
    [StringLength(30)] public string? ReceiverPhone { get; set; }
    [StringLength(500)] public string? ShippingAddress { get; set; }
    [StringLength(500)] public string? PackingNote { get; set; }
    [Required] public string SourceFile { get; set; } = "manual";
    [Required] public string SourceSheet { get; set; } = "manual";
    public int SourceRow { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}

public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public SalesOrder? Order { get; set; }
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    [Required, StringLength(180)] public string ProductNameRaw { get; set; } = "";
    public int Quantity { get; set; } = 1;
    public long? UnitPriceVnd { get; set; }
    public long? LineAmountVnd { get; set; }
}

public class Shipment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public SalesOrder? Order { get; set; }
    [StringLength(80)] public string? TrackingCode { get; set; }
    [StringLength(80)] public string? CarrierName { get; set; }
    public OrderStatus ShipmentStatus { get; set; } = OrderStatus.HandoverToCarrier;
    public DateTimeOffset? ShippedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    [StringLength(500)] public string? ReturnReason { get; set; }
    [StringLength(1000)] public string? DeliveryIssueNote { get; set; }
}

public class SupportTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? OrderId { get; set; }
    public SalesOrder? Order { get; set; }
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid? AssignedUserId { get; set; }
    public AppUser? AssignedUser { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    [Required, StringLength(120)] public string IssueType { get; set; } = "";
    [StringLength(1000)] public string? IssueNote { get; set; }
    [StringLength(1000)] public string? ResolutionNote { get; set; }
    public DateTimeOffset OpenedAt { get; set; } = DateTimeOffset.Now;
}

public class InventoryMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    public InventoryMovementType MovementType { get; set; }
    public DateOnly MovementDate { get; set; }
    public int Quantity { get; set; }
    public Guid? OrderId { get; set; }
    public SalesOrder? Order { get; set; }
    [StringLength(500)] public string? Note { get; set; }
}

public class ImportBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, StringLength(160)] public string BatchName { get; set; } = "";
    [Required, StringLength(260)] public string SourceFile { get; set; } = "";
    [StringLength(40)] public string Status { get; set; } = "created";
    public int RawRows { get; set; }
    public int StagedRows { get; set; }
    public int AcceptedRows { get; set; }
    public int RejectedRows { get; set; }
    [StringLength(1000)] public string? Notes { get; set; }
}

public class AuditLog
{
    public long Id { get; set; }
    public Guid? ActorId { get; set; }
    public AppUser? Actor { get; set; }
    public UserRole? ActorRole { get; set; }
    [Required, StringLength(80)] public string EntityType { get; set; } = "";
    public Guid? EntityId { get; set; }
    [Required, StringLength(80)] public string Action { get; set; } = "";
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    [StringLength(500)] public string? Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
}

public class WorkNotification
{
    public long Id { get; set; }
    [Required, StringLength(160)] public string Title { get; set; } = "";
    [StringLength(1000)] public string? Message { get; set; }
    public Guid? TargetUserId { get; set; }
    public AppUser? TargetUser { get; set; }
    public UserRole? TargetRole { get; set; }
    [StringLength(80)] public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Unread;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset? ReadAt { get; set; }
}
