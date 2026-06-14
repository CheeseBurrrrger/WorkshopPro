using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkshopPro.Application
{
    public class InvoiceEntity
    {
        public int Id { get; set; }
        public string InvoiceNo { get; set; }
        public int VehicleId { get; set; }
        public int CustomerId { get; set; }
        public string ServiceDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Draft";
        public string Notes { get; set; }
        public string CreatedAt { get; set; }

        public string PlateNumber { get; set; }
        public string CustomerName { get; set; }

        public List<InvoiceItemEntity> Items { get; set; } = new List<InvoiceItemEntity>();

    }
}
