using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkshopPro.Application;

namespace WorkshopPro.Domain
{
    public interface ISparePartRepository
    {
        IEnumerable<SparePartEntity> GetAll();
        SparePartEntity GetById(int id);
        SparePartEntity GetByCode(string partCode);
        IEnumerable<SparePartEntity> GetByCategory(string category);
        IEnumerable<SparePartEntity> Search(string term);


        int Insert(SparePartEntity sparePart);
        void Update(SparePartEntity sparePart);

        void UpdateStock(int id, int newQty);

        void RecordMovement(StockMovementEntity movement);
        IEnumerable<StockMovementEntity> GetMovements(int sparePartId);
    }
}
