using QuanLySieuThi.Models.Entities;

namespace QuanLySieuThi.Service
{
    internal static class StockMovement
    {
        public static StockTransaction Apply(Inventory inventory, decimal newQuantity, string type,
            string referenceType, long? referenceId, string? reason, int userId)
        {
            var movement = new StockTransaction
            {
                ProductId = inventory.ProductId,
                Type = type,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                Reason = reason,
                CreatedByUserId = userId,
                QuantityBefore = inventory.QuantityOnHand,
                QuantityAfter = newQuantity,
                QuantityChange = newQuantity - inventory.QuantityOnHand
            };
            inventory.QuantityOnHand = newQuantity;
            inventory.UpdatedAt = movement.CreatedAt;
            return movement;
        }
    }
}
