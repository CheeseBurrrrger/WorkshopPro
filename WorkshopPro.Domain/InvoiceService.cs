// Domain/Services/InvoiceService.cs
using System;
using System.Linq;
using WorkshopPro.Application;
using WorkshopPro.Domain;

namespace WorkshopPro.Domain
{
    public class InvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly ISparePartRepository _sparePartRepo;

        public InvoiceService(
            IInvoiceRepository invoiceRepo,
            ISparePartRepository sparePartRepo)
        {
            _invoiceRepo = invoiceRepo;
            _sparePartRepo = sparePartRepo;
        }
        public InvoiceEntity CreateDraftInvoice(int vehicleId, int customerId)
        {
            var invoice = new InvoiceEntity
            {
                InvoiceNo = GenerateInvoiceNumber(),
                VehicleId = vehicleId,
                CustomerId = customerId,
                ServiceDate = DateTime.Now.ToString("yyyy-MM-dd"),
                TotalAmount = 0,
                Status = "Draft",
                Notes = "",
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            invoice.Id = _invoiceRepo.Insert(invoice);
            return invoice;
        }

        // ── Legacy method kept for backward compatibility with old InvoiceForm ──
        // The old form built up a full InvoiceEntity with Items in memory
        // then called CreateInvoice once at the end. Phase 5 calls CreateDraftInvoice
        // immediately and adds items one by one. Both patterns work.
        public InvoiceEntity CreateInvoice(int vehicleId, int customerId, string notes = null)
        {
            var invoice = new InvoiceEntity
            {
                InvoiceNo = GenerateInvoiceNumber(),
                VehicleId = vehicleId,
                CustomerId = customerId,
                ServiceDate = DateTime.Now.ToString("yyyy-MM-dd"),
                TotalAmount = 0,
                Status = "Draft",
                Notes = notes,
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            invoice.Id = _invoiceRepo.Insert(invoice);
            return invoice;
        }
        public InvoiceItemEntity AddSparePartItem(int invoiceId, int sparePartId, int qty, decimal unitPrice)
        {
            var part = _sparePartRepo.GetById(sparePartId);
            if (part == null)
                throw new InvalidOperationException($"Spare part ID {sparePartId} not found.");
            if (part.StockQty < qty)
                throw new InvalidOperationException(
                    $"Stok '{part.PartName}' tidak cukup. " +
                    $"Tersedia: {part.StockQty}, Diminta: {qty}.");

            var item = new InvoiceItemEntity
            {
                InvoiceId = invoiceId,
                ItemType = "SparePart",
                SparePartId = sparePartId,
                Description = part.PartName,
                Qty = qty,
                UnitPrice = unitPrice,
                Subtotal = qty * unitPrice
            };

            item.Id = _invoiceRepo.InsertItem(item);

            _sparePartRepo.UpdateStock(sparePartId, part.StockQty - qty);
            _sparePartRepo.RecordMovement(new StockMovementEntity
            {
                SparePartId = sparePartId,
                MovementType = "OUT",
                Qty = qty,
                Reference = _invoiceRepo.GetById(invoiceId)?.InvoiceNo ?? "",
                Notes = "Deducted on invoice item add",
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });

            RecalculateTotal(invoiceId);
            return item;
        }
        public InvoiceItemEntity AddLaborItem(int invoiceId, string description, decimal price)
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Labor description cannot be empty.");
            if (price <= 0)
                throw new ArgumentException("Labor price must be greater than zero.");

            var item = new InvoiceItemEntity
            {
                InvoiceId = invoiceId,
                ItemType = "Labor",
                SparePartId = null,
                Description = description,
                Qty = 1,
                UnitPrice = price,
                Subtotal = price
            };

            item.Id = _invoiceRepo.InsertItem(item);
            RecalculateTotal(invoiceId);
            return item;
        }

        /// <summary>
        /// Removes a line item. Restores stock if it was a SparePart line.
        /// </summary>
        public void RemoveItem(int invoiceId, InvoiceItemEntity item)
        {
            _invoiceRepo.DeleteItem(item.Id);

            if (item.ItemType == "SparePart" && item.SparePartId.HasValue)
            {
                var part = _sparePartRepo.GetById(item.SparePartId.Value);
                if (part != null)
                {
                    _sparePartRepo.UpdateStock(part.Id, part.StockQty + item.Qty);
                    _sparePartRepo.RecordMovement(new StockMovementEntity
                    {
                        SparePartId = part.Id,
                        MovementType = "IN",
                        Qty = item.Qty,
                        Reference = "VOID",
                        Notes = "Restored — invoice item removed",
                        CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
            }

            RecalculateTotal(invoiceId);
        }


        public void MarkAsPaid(int invoiceId)
        {
            var invoice = _invoiceRepo.GetById(invoiceId);
            if (invoice == null) throw new InvalidOperationException("Invoice not found.");
            if (invoice.Status != "Draft")
                throw new InvalidOperationException(
                    $"Only Draft invoices can be marked Paid. Current: {invoice.Status}");
            _invoiceRepo.UpdateStatus(invoiceId, "Paid");
        }

        public void CancelInvoice(int invoiceId)
        {
            var invoice = _invoiceRepo.GetById(invoiceId);
            if (invoice == null) throw new InvalidOperationException("Invoice not found.");
            if (invoice.Status != "Draft")
                throw new InvalidOperationException(
                    $"Only Draft invoices can be cancelled. Current: {invoice.Status}");

            // Restore stock for every spare part line
            foreach (var item in _invoiceRepo.GetItemsByInvoiceId(invoiceId))
            {
                if (item.ItemType == "SparePart" && item.SparePartId.HasValue)
                {
                    var part = _sparePartRepo.GetById(item.SparePartId.Value);
                    if (part != null)
                    {
                        _sparePartRepo.UpdateStock(part.Id, part.StockQty + item.Qty);
                        _sparePartRepo.RecordMovement(new StockMovementEntity
                        {
                            SparePartId = part.Id,
                            MovementType = "IN",
                            Qty = item.Qty,
                            Reference = invoice.InvoiceNo,
                            Notes = "Restored — invoice cancelled",
                            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        });
                    }
                }
            }

            _invoiceRepo.UpdateStatus(invoiceId, "Cancelled");
        }


        public InvoiceEntity GetById(int id) => _invoiceRepo.GetById(id);

        public System.Collections.Generic.IEnumerable<InvoiceSummaryEntity> GetRecentSummaries() =>
            _invoiceRepo.GetRecentSummaries();

        public System.Collections.Generic.IEnumerable<InvoiceSummaryEntity> Search(string term) =>
            _invoiceRepo.Search(term);

        public System.Collections.Generic.IEnumerable<InvoiceItemEntity> GetItems(int invoiceId) =>
            _invoiceRepo.GetItemsByInvoiceId(invoiceId);

        private string GenerateInvoiceNumber()
        {
            int year = DateTime.Now.Year;
            int count = _invoiceRepo.CountThisYear(year);
            return $"INV-{year}-{(count + 1):D4}";
        }

        private void RecalculateTotal(int invoiceId)
        {
            var items = _invoiceRepo.GetItemsByInvoiceId(invoiceId);
            decimal total = items.Sum(i => i.Subtotal);
            _invoiceRepo.UpdateTotal(invoiceId, total);
        }
    }
}