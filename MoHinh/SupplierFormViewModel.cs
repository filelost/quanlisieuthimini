using System.ComponentModel.DataAnnotations;

namespace QuanLySieuThi.Models
{
    public sealed class SupplierFormViewModel
    {
        public int SupplierId { get; set; }

        [Required, StringLength(30)]
        [Display(Name = "Mã nhà cung cấp")]
        public string SupplierCode { get; set; } = string.Empty;

        [Required, StringLength(180)]
        [Display(Name = "Tên nhà cung cấp")]
        public string Name { get; set; } = string.Empty;

        [StringLength(30)]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [EmailAddress, StringLength(150)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(300)]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public sealed class SupplierListItemViewModel
    {
        public int SupplierId { get; init; }
        public string SupplierCode { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Phone { get; init; }
        public string? Email { get; init; }
        public string? Address { get; init; }
        public int PurchaseCount { get; init; }
        public bool IsActive { get; init; }
    }
}
