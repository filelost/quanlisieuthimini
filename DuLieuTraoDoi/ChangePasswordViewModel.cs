using System.ComponentModel.DataAnnotations;

namespace QuanLySieuThi.DTO;

public sealed class ChangePasswordViewModel
{
    [Required, StringLength(256)]
    [Display(Name = "Mật khẩu hiện tại")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, StringLength(256, MinimumLength = 8)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, Compare(nameof(NewPassword), ErrorMessage = "Hai mật khẩu mới phải giống nhau.")]
    [Display(Name = "Xác nhận mật khẩu mới")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
