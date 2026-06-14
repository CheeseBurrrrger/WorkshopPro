using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkshopPro.Application;

namespace WorkshopPro.Domain
{
    public interface ICustomerRepository
    {
        CustomerEntity GetById(int id);
        IEnumerable<CustomerEntity> Search(string nameOrPhone);
        int Insert(CustomerEntity customer);
        void Update(CustomerEntity customer);
        CustomerEntity GetOrCreate(string name, string phone, string address);

    }
}
