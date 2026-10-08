using System.ComponentModel.DataAnnotations;

namespace BanHangcoiWeb.Models;

public class RoleTaskRow
{
    public string RoleName { get; set; } = "";
    public string Mission { get; set; } = "";
    public string MainScreen { get; set; } = "";
    public string SuccessMetric { get; set; } = "";
    public List<string> Tasks { get; set; } = [];
}

public class WorkflowDashboardViewModel
{
    public List<RoleTaskRow> RoleTasks { get; set; } = [];
    public List<WorkNotification> Notifications { get; set; } = [];
    public List<AuditLog> AuditLogs { get; set; } = [];
    public List<AiInsight> AiInsights { get; set; } = [];
}

public class AiInsight
{
    public string Priority { get; set; } = "Trung bình";
    public string Title { get; set; } = "";
    public string Finding { get; set; } = "";
    public string Action { get; set; } = "";
    public string OwnerRole { get; set; } = "";
}

public class SalesOrderFormViewModel
{
    public Guid LeadId { get; set; }
    [Required] public string CustomerName { get; set; } = "";
    [Required] public string Phone { get; set; } = "";
    [Required] public string ProductName { get; set; } = "";
    [Required] public long TotalAmountVnd { get; set; }
    public int Quantity { get; set; } = 1;
    public long? ShippingFeeVnd { get; set; }
    [Required] public string ReceiverName { get; set; } = "";
    [Required] public string ReceiverPhone { get; set; } = "";
    [Required] public string ShippingAddress { get; set; } = "";
    public string? Note { get; set; }
    public string? PackingNote { get; set; }
}

public class ShipmentUpdateViewModel
{
    public Guid OrderId { get; set; }
    [Required] public string TrackingCode { get; set; } = "";
    [Required] public string CarrierName { get; set; } = "";
    public OrderStatus ShipmentStatus { get; set; } = OrderStatus.HandoverToCarrier;
    public string? DeliveryIssueNote { get; set; }
}

public class SupportUpdateViewModel
{
    public Guid OrderId { get; set; }
    [Required] public string IssueType { get; set; } = "Hỗ trợ nhận hàng";
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public string? IssueNote { get; set; }
    public string? ResolutionNote { get; set; }
}
