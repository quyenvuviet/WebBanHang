using BanHangcoiWeb.Data;
using BanHangcoiWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace BanHangcoiWeb.Services;

public class LocalAiAdvisor(AppDbContext db)
{
    public async Task<List<AiInsight>> BuildInsightsAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var weekStart = today.AddDays(-7);

        var leads = await db.CrmLeads
            .Include(x => x.CallAttempts)
            .Where(x => x.LeadDate == null || x.LeadDate >= weekStart)
            .ToListAsync();

        var orders = await db.SalesOrders
            .Where(x => x.OrderDate == null || x.OrderDate >= weekStart)
            .ToListAsync();

        var shipments = await db.Shipments.Include(x => x.Order).ToListAsync();
        var tickets = await db.SupportTickets.Where(x => x.Status == TicketStatus.Open || x.Status == TicketStatus.InProgress).ToListAsync();

        var insights = new List<AiInsight>();
        var newLeads = leads.Count(x => x.LeadStatus == LeadStatus.New || x.LeadStatus == LeadStatus.Assigned);
        var calledLeads = leads.Count(x => x.CallAttempts.Count > 0);
        var closedWon = leads.Count(x => x.LeadStatus == LeadStatus.ClosedWon);
        var conversion = calledLeads == 0 ? 0 : (decimal)closedWon / calledLeads;

        if (newLeads > 0)
        {
            insights.Add(new AiInsight
            {
                Priority = "Cao",
                Title = "Lead mới cần gọi nhanh",
                Finding = $"Có {newLeads:N0} lead mới/đã giao trong 7 ngày gần đây chưa kết thúc.",
                Action = "Chia lead nóng cho Sale và đặt SLA gọi lần 1 trong 15 phút để tăng tỷ lệ chốt.",
                OwnerRole = "Quản lý/Sale"
            });
        }

        if (conversion < 0.18m && calledLeads >= 20)
        {
            insights.Add(new AiInsight
            {
                Priority = "Cao",
                Title = "Tỷ lệ chốt đang thấp",
                Finding = $"Tỷ lệ chốt sau gọi khoảng {(conversion * 100):N1}%.",
                Action = "Rà soát nội dung gọi, lọc lại số nóng, dùng script hỏi nỗi đau: loại xe, điện áp, nhu cầu âm thanh, ngân sách.",
                OwnerRole = "Quản lý/Sale"
            });
        }

        var pageGroups = leads
            .Where(x => !string.IsNullOrWhiteSpace(x.PageName))
            .GroupBy(x => x.PageName!)
            .Select(x => new
            {
                Page = x.Key,
                Leads = x.Count(),
                Won = x.Count(l => l.LeadStatus == LeadStatus.ClosedWon)
            })
            .OrderByDescending(x => x.Leads)
            .Take(5)
            .ToList();

        var weakPage = pageGroups.FirstOrDefault(x => x.Leads >= 10 && x.Won == 0);
        if (weakPage is not null)
        {
            insights.Add(new AiInsight
            {
                Priority = "Trung bình",
                Title = "Nguồn quảng cáo cần tối ưu",
                Finding = $"Page {weakPage.Page} có {weakPage.Leads:N0} lead nhưng chưa thấy đơn chốt trong tập dữ liệu gần đây.",
                Action = "Marketing kiểm tra lại content, tệp khách, từ khóa và chuyển ngân sách sang page/content có đơn.",
                OwnerRole = "Marketing"
            });
        }

        var waitingPack = orders.Count(x => x.OrderStatus == OrderStatus.Confirmed || x.OrderStatus == OrderStatus.WaitingPack);
        if (waitingPack > 0)
        {
            insights.Add(new AiInsight
            {
                Priority = "Trung bình",
                Title = "Đơn chốt cần đóng hàng",
                Finding = $"Có {waitingPack:N0} đơn đang ở bước xác nhận/chờ kho.",
                Action = "Kho ưu tiên đóng trong ngày, thiếu hàng thì báo quản lý ngay để không mất đơn.",
                OwnerRole = "Kho"
            });
        }

        var issueShipments = shipments.Count(x => x.ShipmentStatus == OrderStatus.DeliveryIssue || x.ShipmentStatus == OrderStatus.Returned);
        if (issueShipments + tickets.Count > 0)
        {
            insights.Add(new AiInsight
            {
                Priority = "Cao",
                Title = "Cần cứu đơn sau giao",
                Finding = $"Có {issueShipments:N0} vận đơn lỗi/hoàn và {tickets.Count:N0} ticket CSKH mở.",
                Action = "CSKH gọi xác nhận lý do, cập nhật ticket, chuyển Sale hỗ trợ thuyết phục nhận hàng nếu cần.",
                OwnerRole = "CSKH/Quản lý"
            });
        }

        if (insights.Count == 0)
        {
            insights.Add(new AiInsight
            {
                Priority = "Thấp",
                Title = "Vận hành ổn định",
                Finding = "Chưa thấy cảnh báo lớn từ dữ liệu hiện tại.",
                Action = "Tiếp tục đẩy lead nóng, kiểm tra đơn chờ đóng và chăm sóc lại khách đã mua.",
                OwnerRole = "Quản lý"
            });
        }

        return insights;
    }
}
