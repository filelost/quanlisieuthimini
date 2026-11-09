namespace QuanLySieuThi.Models.Entities
{
    public sealed class ProductImage
    {
        public int ProductId { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public byte[] Content { get; set; } = [];
    }
}
