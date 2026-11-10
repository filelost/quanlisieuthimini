using System.ComponentModel.DataAnnotations;

namespace QuanLySieuThi.Models
{
    public sealed class EmployeeAccountFormViewModel
    {
        [Required]
        public string RoleName { get; set; } = string.Empty;

        [Required, StringLength(30)]
        [Display(Name = "Mã nhân viên")]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required, StringLength(150)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(30)]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [EmailAddress, StringLength(150)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(80)]
        [Display(Name = "Chức vụ")]
        public string? Position { get; set; }

        [Required, StringLength(60)]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required, MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu tạm thời")]
        public string Password { get; set; } = string.Empty;
    }

    public sealed class EmployeeAccountListItemViewModel
    {
        public int EmployeeId { get; init; }
        public string EmployeeCode { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string? Phone { get; init; }
        public string? Position { get; init; }
        public string? Username { get; init; }
        public string? RoleName { get; init; }
        public bool IsActive { get; init; }
    }
}
