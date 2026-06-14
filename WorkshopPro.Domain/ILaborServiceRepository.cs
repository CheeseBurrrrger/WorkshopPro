using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkshopPro.Application;

namespace WorkshopPro.Domain
{
    public interface ILaborServiceRepository
    {
        IEnumerable<LaborServiceEntity> GetAll();
    }
}
