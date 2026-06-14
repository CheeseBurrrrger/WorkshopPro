using System.Collections.Generic;
using Dapper;
using System.Data.SQLite;
using WorkshopPro.Domain;
using WorkshopPro.Application;

namespace WorkshopPro.Infrastructure.Repositories
{
	public class SqliteVehicleRepository : IVehicleRepository
	{
		private readonly string _dbPath;

		public SqliteVehicleRepository(string dbPath)
		{
			_dbPath = dbPath;
		}

		private SQLiteConnection OpenConnection()
			=> new SQLiteConnection($"Data Source={_dbPath}");

		public VehicleEntity GetByPlate(string plate)
		{
			using (var conn = OpenConnection())
			{
				return conn.QueryFirstOrDefault<VehicleEntity>(
					@"SELECT v.*, vm.ModelName, m.Name AS ManufacturerName
                      FROM Vehicle v
                      JOIN VehicleModel vm ON v.VehicleModelId = vm.Id
                      JOIN Manufacturer m  ON vm.ManufacturerId = m.Id
                      WHERE LOWER(v.PlateNumber) = LOWER(@plate)",
					new { plate });
			}
		}

		public VehicleEntity GetById(int id)
		{
			using (var conn = OpenConnection())
			{
				return conn.QueryFirstOrDefault<VehicleEntity>(
					"SELECT * FROM Vehicle WHERE Id = @id",
					new { id });
			}
		}

		public int Insert(VehicleEntity vehicle)
		{
			using (var conn = OpenConnection())
			{
				// ExecuteScalar returns the new auto-generated Id
				return conn.ExecuteScalar<int>(
					@"INSERT INTO Vehicle (PlateNumber, CustomerId, VehicleModelId, Color, Year, Notes)
                      VALUES (@PlateNumber, @CustomerId, @VehicleModelId, @Color, @Year, @Notes);
                      SELECT last_insert_rowid();",
					vehicle);
			}
		}

		public void Update(VehicleEntity vehicle)
		{
			using (var conn = OpenConnection())
			{
				conn.Execute(
					@"UPDATE Vehicle
                      SET PlateNumber = @PlateNumber,
                          CustomerId  = @CustomerId,
                          VehicleModelId = @VehicleModelId,
                          Color = @Color,
                          Year  = @Year,
                          Notes = @Notes
                      WHERE Id = @Id",
					vehicle);
			}
		}

        public IEnumerable<VehicleEntity> GetAll()
        {
            throw new System.NotImplementedException();
        }
    }
}