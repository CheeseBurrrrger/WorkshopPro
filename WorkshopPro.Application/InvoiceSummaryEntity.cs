using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkshopPro.Application
{
    public class InvoiceSummaryEntity
    {
        public int Id { get; set; }
        public string InvoiceNo { get; set; }
        public string ServiceDate { get; set; }
        public string PlateNumber { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
    }
}
