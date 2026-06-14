// Domain/Services/InventoryService.cs
using System;
using WorkshopPro.Domain;
using WorkshopPro.Application;

namespace WorkshopPro.Domain
{
    public class InventoryService
    {
        private readonly ISparePartRepository _repo;

        public InventoryService(ISparePartRepository repo)
        {
            _repo = repo;
        }

        public void ReceiveStock(int sparePartId, int qty, string reference)
        {
            if (qty <= 0)
                throw new ArgumentException("Quantity must be positive.");

            var part = _repo.GetById(sparePartId);
            if (part == null)
                throw new InvalidOperationException("Spare part not found.");

            _repo.UpdateStock(part.Id, part.StockQty + qty);
            _repo.RecordMovement(new StockMovementEntity
            {
                SparePartId = sparePartId,
                MovementType = "IN",
                Qty = qty,
                Reference = reference,
                Notes = "Stock received",
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        public bool IsStockSufficient(int sparePartId, int requiredQty)
        {
            var part = _repo.GetById(sparePartId);
            return part != null && part.StockQty >= requiredQty;
        }
    }
}