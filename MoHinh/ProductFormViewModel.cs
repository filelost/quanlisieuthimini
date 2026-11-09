using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace QuanLySieuThi.Models
{
    public sealed class ProductFormViewModel
    {
        public int ProductId { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Mã sản phẩm")]
        public string Sku { get; set; } = string.Empty;

        [StringLength(80)]
        [Display(Name = "Mã vạch")]
        public string? Barcode { get; set; }

        [Required, StringLength(180)]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục.")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [Required, StringLength(30)]
        [Display(Name = "Đơn vị tính")]
        public string Unit { get; set; } = "cái";

        [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0.")]
        [Display(Name = "Giá bán")]
        public decimal SalePrice { get; set; }

        [Range(typeof(decimal), "0", "999999999999999", ErrorMessage = "Mức tồn tối thiểu không hợp lệ.")]
        [Display(Name = "Mức tồn tối thiểu")]
        public decimal ReorderLevel { get; set; }

        public bool IsActive { get; set; } = true;
        [Display(Name = "Ảnh sản phẩm")]
        public IFormFile? ImageFile { get; set; }
        public bool HasImage { get; set; }
        public IEnumerable<SelectListItem> Categories { get; set; } = [];
    }

    public sealed class ProductListItemViewModel
    {
        public int ProductId { get; init; }
        public string Sku { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string CategoryName { get; init; } = string.Empty;
        public decimal SalePrice { get; init; }
        public decimal QuantityOnHand { get; init; }
        public string Unit { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public bool HasImage { get; init; }
    }
}
