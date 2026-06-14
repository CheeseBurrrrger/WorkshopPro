using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkshopPro.Application
{
    public class StockMovementEntity
    {
        public int Id { get; set; }
        public int SparePartId { get; set; }
        public string MovementType { get; set; } // "IN" or "OUT"
        public int Qty { get; set; }
        public string Reference { get; set; } // INV-2024-0001 or PO number
        public string Notes { get; set; }
        public string CreatedAt { get; set; }

        public string PartName { get; set; }
    }
}
