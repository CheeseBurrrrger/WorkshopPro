using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkshopPro.Application
{
    public class InvoiceItemEntity
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string ItemType { get; set; } // "Service" or "SparePart"
        public int? SparePartId { get; set; }
        public string Description { get; set; }
        public int Qty { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }
}
