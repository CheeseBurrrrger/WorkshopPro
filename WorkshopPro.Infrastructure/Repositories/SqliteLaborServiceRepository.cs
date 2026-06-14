using Dapper;
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
    public class SqliteLaborServiceRepository : ILaborServiceRepository
    {
        private readonly string _dbPath;
        public SqliteLaborServiceRepository(string dbPath) { _dbPath = dbPath; }

        private SQLiteConnection OpenConnection()
            => new SQLiteConnection($"Data Source={_dbPath};Version=3;");

        public IEnumerable<LaborServiceEntity> GetAll()
        {
            using (var conn = OpenConnection())
                return conn.Query<LaborServiceEntity>(
                    "SELECT Id, Name FROM LaborService ORDER BY Name");
        }
    }
}
