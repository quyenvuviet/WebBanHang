using System.ComponentModel.DataAnnotations;

namespace BanHangcoiWeb.Models;

public class LeadEntryViewModel
{
    public Guid? Id { get; set; }
    [Display(Name = "NGÀY SỐ VỀ")] public DateOnly? LeadDate { get; set; }
    [Required, Display(Name = "TÊN KHÁCH HÀNG")] public string CustomerName { get; set; } = "";
    [Required, Display(Name = "SĐT")] public string Phone { get; set; } = "";
    [Display(Name = "SỐ NÓNG/SỐ CHECK")] public string? LeadType { get; set; }
    [Display(Name = "NGHỀ NGHIỆP")] public string? Occupation { get; set; }
    [Display(Name = "PAGE")] public string? PageName { get; set; }
    [Display(Name = "CONTENT")] public string? ContentName { get; set; }
    [Display(Name = "SP KHÁCH CHỐT")] public string? ProductInterestRaw { get; set; }
    [Display(Name = "XE CÓ HƠI/KHÔNG HƠI")] public string? VehicleAirType { get; set; }
    [Display(Name = "LOẠI XE")] public string? VehicleType { get; set; }
    [Display(Name = "SALE")] public string? SaleName { get; set; }
    [Display(Name = "GỌI LẦN 1")] public string? Call1Result { get; set; }
    [Display(Name = "NGÀY GỌI LẦN 1")] public DateOnly? Call1Date { get; set; }
    [Display(Name = "GHI CHÚ GỌI LẦN 1 // Nỗi đau khách hàng")] public string? Call1Note { get; set; }
    [Display(Name = "SỐ PHÚT GỌI")] public int? Call1Minutes { get; set; }
    [Display(Name = "GỌI LẦN 2")] public string? Call2Result { get; set; }
    [Display(Name = "NGÀY GỌI LẦN 2")] public DateOnly? Call2Date { get; set; }
    [Display(Name = "GHI CHÚ GỌI LẦN 2")] public string? Call2Note { get; set; }
    [Display(Name = "SỐ PHÚT GỌI")] public int? Call2Minutes { get; set; }
    [Display(Name = "GỌI LẦN 3")] public string? Call3Result { get; set; }
    [Display(Name = "NGÀY GỌI LẦN 3")] public DateOnly? Call3Date { get; set; }
    [Display(Name = "GHI CHÚ GỌI LẦN 3")] public string? Call3Note { get; set; }
    [Display(Name = "SỐ PHÚT GỌI")] public int? Call3Minutes { get; set; }
    [Display(Name = "GỌI LẦN 4")] public string? Call4Result { get; set; }
    [Display(Name = "NGÀY GỌI LẦN 4")] public DateOnly? Call4Date { get; set; }
    [Display(Name = "GHI CHÚ GỌI LẦN 4")] public string? Call4Note { get; set; }
    [Display(Name = "SỐ PHÚT GỌI")] public int? Call4Minutes { get; set; }
    [Display(Name = "THỜI GIAN TÁC NGHIỆP")] public DateTimeOffset? WorkAt { get; set; }
    [Display(Name = "ĐẾM THỜI GIAN THẢ NỔI")] public int? FloatingDays { get; set; }
    [Display(Name = "GIÁ TRỊ ĐƠN CHỐT")] public long? DealValueVnd { get; set; }
    [Display(Name = "NOTE")] public string? SaleNote { get; set; }
    [Display(Name = "MỨC BÁN")] public string? SaleLevel { get; set; }
    [Display(Name = "LÍ DO MUA HÀNG")] public string? PurchaseReason { get; set; }
    [Display(Name = "TRẠNG THÁI")] public LeadStatus LeadStatus { get; set; } = LeadStatus.New;

    public static LeadEntryViewModel FromLead(CrmLead lead)
    {
        var calls = lead.CallAttempts.ToDictionary(x => x.AttemptNo);
        calls.TryGetValue(1, out var c1);
        calls.TryGetValue(2, out var c2);
        calls.TryGetValue(3, out var c3);
        calls.TryGetValue(4, out var c4);

        return new LeadEntryViewModel
        {
            Id = lead.Id,
            LeadDate = lead.LeadDate,
            CustomerName = lead.CustomerNameRaw ?? "",
            Phone = lead.PhoneNormalized ?? "",
            LeadType = lead.LeadType,
            Occupation = lead.Occupation,
            PageName = lead.PageName,
            ContentName = lead.ContentName,
            ProductInterestRaw = lead.ProductInterestRaw,
            VehicleAirType = lead.VehicleAirType,
            VehicleType = lead.VehicleType,
            SaleName = lead.SaleNameRaw,
            Call1Result = c1?.ResultRaw,
            Call1Date = c1?.CallDate,
            Call1Note = c1?.PainNote,
            Call1Minutes = c1?.DurationSeconds / 60,
            Call2Result = c2?.ResultRaw,
            Call2Date = c2?.CallDate,
            Call2Note = c2?.PainNote,
            Call2Minutes = c2?.DurationSeconds / 60,
            Call3Result = c3?.ResultRaw,
            Call3Date = c3?.CallDate,
            Call3Note = c3?.PainNote,
            Call3Minutes = c3?.DurationSeconds / 60,
            Call4Result = c4?.ResultRaw,
            Call4Date = c4?.CallDate,
            Call4Note = c4?.PainNote,
            Call4Minutes = c4?.DurationSeconds / 60,
            WorkAt = lead.WorkAt,
            FloatingDays = lead.FloatingDays,
            DealValueVnd = lead.DealValueVnd,
            SaleNote = lead.SaleNote,
            SaleLevel = lead.SaleLevel,
            PurchaseReason = lead.PurchaseReason,
            LeadStatus = lead.LeadStatus
        };
    }
}
