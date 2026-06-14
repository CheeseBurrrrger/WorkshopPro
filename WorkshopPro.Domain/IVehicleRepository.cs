using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkshopPro.Application;

namespace WorkshopPro.Domain
{
    public interface IVehicleRepository
    {
        VehicleEntity GetByPlate(string plateNumber);
        VehicleEntity GetById(int id);
        IEnumerable<VehicleEntity> GetAll();
        int Insert(VehicleEntity vehicle);
        void Update(VehicleEntity vehicle);

    }
}
