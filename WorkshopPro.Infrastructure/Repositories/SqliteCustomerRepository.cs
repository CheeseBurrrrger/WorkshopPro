using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkshopPro.Application;
using WorkshopPro.Domain;


namespace WorkshopPro.Infrastructure.Repositories
{
    public class SqliteCustomerRepository : ICustomerRepository
    {
        private readonly string _dbPath;
        public SqliteCustomerRepository(string dbPath) { _dbPath = dbPath; }
        private SQLiteConnection OpenConnection()
            => new SQLiteConnection($"Data Source={_dbPath}");
        public CustomerEntity GetById(int id)
        {
            using (var conn = OpenConnection())
                return conn.QueryFirstOrDefault<CustomerEntity>(
                    "SELECT * FROM Customer WHERE Id = @id", new { id });
        }

        public int Insert(CustomerEntity customer)
        {
            using (var conn = OpenConnection())
                return conn.ExecuteScalar<int>(
                    @"INSERT INTO Customer (Name, Phone, Address)
                      VALUES (@Name, @Phone, @Address);
                      SELECT last_insert_rowid();",
                    customer);
        }

        public IEnumerable<CustomerEntity> Search(string keyword)
        {
            using (var conn = OpenConnection())
                return conn.Query<CustomerEntity>(
                    "SELECT * FROM Customer WHERE Name LIKE @kw OR Phone LIKE @kw ORDER BY Name",
                    new { kw = $"%{keyword}%" });
        }

        public void Update(CustomerEntity customer)
        {
            using (var conn = OpenConnection())
                conn.Execute(
                    @"UPDATE Customer SET Name=@Name, Phone=@Phone, Address=@Address
                      WHERE Id=@Id", customer);
        }

        public CustomerEntity GetOrCreate(string name, string phone, string address)
        {
            using (var conn = OpenConnection())
            {
                var existing = conn.QuerySingleOrDefault<CustomerEntity>(
                    "SELECT * FROM Customer WHERE Name = @name COLLATE NOCASE",
                    new { name });

                if (existing != null)
                    return existing;

                var newId = conn.ExecuteScalar<int>(
                    @"INSERT INTO Customer (Name, Phone, Address)
                      VALUES (@name, @phone, @address);
                      SELECT last_insert_rowid();",
                    new { name, phone, address });

                return new CustomerEntity
                {
                    Id = newId,
                    Name = name,
                    Phone = phone,
                    Address = address
                };
            }
        }
    }
}
