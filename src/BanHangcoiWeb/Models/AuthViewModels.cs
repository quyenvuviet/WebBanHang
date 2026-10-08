using System.ComponentModel.DataAnnotations;

namespace BanHangcoiWeb.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Nhập tài khoản")]
    public string LoginName { get; set; } = "";

    [Required(ErrorMessage = "Nhập mật khẩu")]
    public string Password { get; set; } = "";

    public string? ErrorMessage { get; set; }
}
