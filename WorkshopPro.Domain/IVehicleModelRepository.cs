using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkshopPro.Application;

namespace WorkshopPro.Domain
{
    public interface IVehicleModelRepository
    {
        IEnumerable<ManufacturerEntity> GetAllManufacturers();
        IEnumerable<VehicleModelEntity> GetByManufacturer(int manufacturerId);
        IEnumerable<VehicleModelEntity> GetAll();
        VehicleModelEntity GetById(int id);
        
        IEnumerable<VehicleModelEntity> Search(string keyword);
    }
}
