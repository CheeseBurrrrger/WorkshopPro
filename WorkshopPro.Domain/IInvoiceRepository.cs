using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkshopPro.Application;

namespace WorkshopPro.Domain
{
    public interface IInvoiceRepository
    {
        InvoiceEntity GetById(int id);
        IEnumerable<InvoiceEntity> GetByDateRange(DateTime from, DateTime to);
        IEnumerable<InvoiceEntity> GetByVehicle(int vehicleId);
        IEnumerable<InvoiceEntity> GetByCustomer(int customerId);

        int Insert(InvoiceEntity invoice);

        void UpdateStatus(int invoiceId, string status);
        void UpdateTotal(int invoiceId, decimal total);


        IEnumerable<InvoiceSummaryEntity> GetRecentSummaries(int limit = 200);
        IEnumerable<InvoiceSummaryEntity> Search(string term);


        int InsertItem(InvoiceItemEntity item);
        IEnumerable<InvoiceItemEntity> GetItemsByInvoiceId(int invoiceId);
        void DeleteItem(int itemId);

        string GenerateNextInvoiceNo();

        int CountThisYear(int year);
    }
}
