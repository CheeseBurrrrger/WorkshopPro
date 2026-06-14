using System.Collections.Generic;
using Dapper;
using System.Data.SQLite;
using WorkshopPro.Domain;
using WorkshopPro.Application;

namespace WorkshopPro.Infrastructure.Repositories
{
    public class SqliteVehicleModelRepository : IVehicleModelRepository
    {
        private readonly string _dbPath;
        public SqliteVehicleModelRepository(string dbPath) { _dbPath = dbPath; }

        private SQLiteConnection OpenConnection()
            => new SQLiteConnection($"Data Source={_dbPath}");

        public IEnumerable<ManufacturerEntity> GetAllManufacturers()
        {
            using (var conn = OpenConnection())
            {
                return conn.Query<ManufacturerEntity>(
                    "SELECT * FROM Manufacturer ORDER BY Name");
            }
        }

        public IEnumerable<VehicleModelEntity> GetAll()
        {
            using (var conn = OpenConnection())
            {
                return conn.Query<VehicleModelEntity>(
                    "SELECT * FROM VehicleModel ORDER BY ModelName");
            }
        }

        public IEnumerable<VehicleModelEntity> GetByManufacturer(int manufacturerId)
        {
            using (var conn = OpenConnection())
            {
                return conn.Query<VehicleModelEntity>(
                    @"SELECT * FROM VehicleModel 
                      WHERE ManufacturerId = @manufacturerId 
                      ORDER BY ModelName",
                    new { manufacturerId });
            }
        }

        public IEnumerable<VehicleModelEntity> Search(string keyword)
        {
            using (var conn = OpenConnection())
            {
                return conn.Query<VehicleModelEntity>(
                    @"SELECT * FROM VehicleModel 
                      WHERE ModelName LIKE @keyword 
                      ORDER BY ModelName",
                    new { keyword = $"%{keyword}%" });
            }
        }

        public VehicleModelEntity GetById(int id)
        {
            throw new System.NotImplementedException();
        }
    }
}