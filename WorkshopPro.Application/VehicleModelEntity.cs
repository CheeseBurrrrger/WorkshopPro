using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkshopPro.Application
{
    public class VehicleModelEntity
    {
        public int Id { get; set; }
        public int ManufacturerId { get; set; }
        public string ModelName { get; set; }
        public string FuelType { get; set; }
        public int EngineCC { get; set; }
        public string TransmissionType { get; set; }
        public int YearFrom { get; set; }
        public int? YearTo { get; set; }

        public string ManufacturerName { get; set; }
        public override string ToString()
        {
            string yearRange = YearTo.HasValue ? $"{YearFrom}-{YearTo}" : $"{YearFrom}-Present";
            return $"{ModelName} {EngineCC}cc {FuelType} {TransmissionType} ({yearRange})";
        }
    }
}
