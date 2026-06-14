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
    public class SqliteManufacturerRepository : IManufacturerRepository
    {
        private readonly string _dbPath;
        public SqliteManufacturerRepository(string dbPath) { _dbPath = dbPath; }

        private SQLiteConnection OpenConnection()
            => new SQLiteConnection($"Data Source={_dbPath};Version=3;");

        public IEnumerable<ManufacturerEntity> GetAll()
        {
            using (var conn = OpenConnection())
                return conn.Query<ManufacturerEntity>(
                    "SELECT * FROM Manufacturer ORDER BY Name");
        }

        public ManufacturerEntity GetById(int id)
        {
            using (var conn = OpenConnection())
                return conn.QueryFirstOrDefault<ManufacturerEntity>(
                    "SELECT * FROM Manufacturer WHERE Id = @id", new { id });
        }
    }
}
