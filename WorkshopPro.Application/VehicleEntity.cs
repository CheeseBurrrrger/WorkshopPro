using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkshopPro.Application
{
    public class VehicleEntity
    {
        public int Id { get; set; }
        public string PlateNumber { get; set; }
        public int? CustomerId { get; set; }
        public int? VehicleModelId { get; set; }
        public string Color { get; set; }
        public int? Year { get; set; }
        public string Notes { get; set; }

        public string customerName { get; set; }
        public string modelDisplayName { get; set; }
    }
}
