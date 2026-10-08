using Microsoft.AspNetCore.Mvc.Rendering;

namespace BanHangcoiWeb.Models;

public static class VietnameseLabels
{
    public static string Vi(this UserRole role) => role switch
    {
        UserRole.Admin => "Quản trị",
        UserRole.Manager => "Quản lý",
        UserRole.Sale => "Sale",
        UserRole.Warehouse => "Kho",
        UserRole.Shipping => "Vận chuyển",
        UserRole.Support => "CSKH",
        UserRole.Marketing => "Marketing",
        UserRole.Finance => "Kế toán",
        UserRole.QaReadonly => "Nghiệm thu",
        _ => role.ToString()
    };

    public static string Vi(this LeadStatus status) => status switch
    {
        LeadStatus.New => "Số mới",
        LeadStatus.Assigned => "Đã giao sale",
        LeadStatus.CalledOnce => "Đã gọi lần 1",
        LeadStatus.FollowUp => "Hẹn gọi lại",
        LeadStatus.NoAnswer => "Không nghe máy",
        LeadStatus.LowIntent => "Nhu cầu thấp",
        LeadStatus.HighIntent => "Nhu cầu cao",
        LeadStatus.Qualified => "Đủ điều kiện chốt",
        LeadStatus.ClosedWon => "Đã chốt đơn",
        LeadStatus.ClosedLost => "Không chốt được",
        LeadStatus.Recycled => "Nuôi lại",
        LeadStatus.Duplicate => "Trùng số",
        LeadStatus.Invalid => "Sai số",
        _ => status.ToString()
    };

    public static string Vi(this OrderStatus status) => status switch
    {
        OrderStatus.Draft => "Nháp",
        OrderStatus.Confirmed => "Đã xác nhận",
        OrderStatus.WaitingPack => "Chờ kho đóng hàng",
        OrderStatus.Packed => "Đã đóng hàng",
        OrderStatus.HandoverToCarrier => "Chờ vận chuyển lấy",
        OrderStatus.Shipping => "Đang giao",
        OrderStatus.DeliveryIssue => "Giao hàng có vấn đề",
        OrderStatus.Delivered => "Giao thành công",
        OrderStatus.Returned => "Hoàn hàng",
        OrderStatus.Cancelled => "Đã hủy",
        OrderStatus.Done => "Hoàn tất",
        _ => status.ToString()
    };

    public static string Vi(this TicketStatus status) => status switch
    {
        TicketStatus.Open => "Mới mở",
        TicketStatus.InProgress => "Đang xử lý",
        TicketStatus.WaitingCustomer => "Chờ khách phản hồi",
        TicketStatus.Resolved => "Đã xử lý",
        TicketStatus.Closed => "Đã đóng",
        _ => status.ToString()
    };

    public static string Vi(this NotificationStatus status) => status switch
    {
        NotificationStatus.Unread => "Chưa đọc",
        NotificationStatus.Read => "Đã đọc",
        NotificationStatus.Done => "Đã xong",
        _ => status.ToString()
    };

    public static string ViRole(UserRole? role) => role.HasValue ? role.Value.Vi() : "Tất cả";

    public static string ViPriority(string? priority) => priority switch
    {
        "High" => "Cao",
        "Medium" => "Trung bình",
        "Low" => "Thấp",
        "Cao" => "Cao",
        "Trung bình" => "Trung bình",
        "Thấp" => "Thấp",
        _ => priority ?? ""
    };

    public static string PriorityClass(string? priority) => priority switch
    {
        "High" or "Cao" => "high",
        "Medium" or "Trung bình" => "medium",
        "Low" or "Thấp" => "low",
        _ => "medium"
    };

    public static string ViOwnerRole(string? ownerRole) => ownerRole switch
    {
        "Manager/Sale" => "Quản lý/Sale",
        "Support/Manager" => "CSKH/Quản lý",
        "Quản lý/Sale" => "Quản lý/Sale",
        "CSKH/Quản lý" => "CSKH/Quản lý",
        "Marketing" => "Marketing",
        "Warehouse" => "Kho",
        "Kho" => "Kho",
        "Manager" => "Quản lý",
        "Quản lý" => "Quản lý",
        _ => ownerRole ?? ""
    };

    public static string ViImportStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "created" => "Mới tạo",
        "accepted" => "Đã nhận",
        "rejected" => "Bị từ chối",
        "failed" => "Lỗi",
        "imported" => "Đã import",
        "done" => "Hoàn tất",
        _ => status ?? ""
    };

    public static List<SelectListItem> LeadStatusItems(LeadStatus selected) =>
        Enum.GetValues<LeadStatus>().Select(x => new SelectListItem(x.Vi(), x.ToString(), x == selected)).ToList();

    public static List<SelectListItem> OrderStatusItems(OrderStatus selected) =>
        Enum.GetValues<OrderStatus>().Select(x => new SelectListItem(x.Vi(), x.ToString(), x == selected)).ToList();

    public static List<SelectListItem> TicketStatusItems(TicketStatus selected) =>
        Enum.GetValues<TicketStatus>().Select(x => new SelectListItem(x.Vi(), x.ToString(), x == selected)).ToList();
}
