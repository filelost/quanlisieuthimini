using System.ComponentModel.DataAnnotations;

namespace QuanLySieuThi.Models
{
    public sealed class CustomerFormViewModel
    {
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Phone { get; set; }

        [StringLength(150)]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string? Email { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }
    }
}
