using System.Collections.Generic;
using Dapper;
using System.Data.SQLite;
using WorkshopPro.Domain;
using WorkshopPro.Application;

namespace WorkshopPro.Infrastructure.Repositories
{
    public class SqliteSparePartRepository : ISparePartRepository
    {
        private readonly string _dbPath;
        public SqliteSparePartRepository(string dbPath) { _dbPath = dbPath; }

        private SQLiteConnection OpenConnection()
            => new SQLiteConnection($"Data Source={_dbPath};Version=3;");

        public IEnumerable<SparePartEntity> GetAll()
        {
            using (var conn = OpenConnection())
                return conn.Query<SparePartEntity>("SELECT * FROM SparePart ORDER BY PartName");
        }

        public SparePartEntity GetById(int id)
        {
            using (var conn = OpenConnection())
                return conn.QueryFirstOrDefault<SparePartEntity>(
                    "SELECT * FROM SparePart WHERE Id = @id", new { id });
        }

        public SparePartEntity GetByCode(string code)
        {
            using (var conn = OpenConnection())
                return conn.QueryFirstOrDefault<SparePartEntity>(
                    "SELECT * FROM SparePart WHERE PartCode = @code COLLATE NOCASE",
                    new { code });
        }

        public IEnumerable<SparePartEntity> GetByCategory(string category)
        {
            using (var conn = OpenConnection())
                return conn.Query<SparePartEntity>(
                    "SELECT * FROM SparePart WHERE Category = @category ORDER BY PartName",
                    new { category });
        }

        /// <summary>
        /// Search by part name or code — used by the autocomplete popup.
        /// % wildcards make this a "contains" search: "%oli%" matches "Oli Mesin".
        /// LIMIT 10 keeps the popup small even with hundreds of parts.
        /// Java JDBC equivalent: prepareStatement with LIKE ? and "%term%"
        /// </summary>
        public IEnumerable<SparePartEntity> Search(string term)
        {
            using (var conn = OpenConnection())
                return conn.Query<SparePartEntity>(
                    @"SELECT * FROM SparePart
                      WHERE PartName LIKE @term OR PartCode LIKE @term
                      ORDER BY PartName
                      LIMIT 10",
                    new { term = $"%{term}%" });
        }

        public int Insert(SparePartEntity part)
        {
            using (var conn = OpenConnection())
                return conn.ExecuteScalar<int>(
                    @"INSERT INTO SparePart (PartCode, PartName, Category, Unit, StockQty, PriceBuy, PriceSell)
                      VALUES (@PartCode, @PartName, @Category, @Unit, @StockQty, @PriceBuy, @PriceSell);
                      SELECT last_insert_rowid();",
                    part);
        }

        public void Update(SparePartEntity part)
        {
            using (var conn = OpenConnection())
                conn.Execute(
                    @"UPDATE SparePart
                      SET PartName=@PartName, Category=@Category, Unit=@Unit,
                          PriceBuy=@PriceBuy, PriceSell=@PriceSell
                      WHERE Id=@Id",
                    part);
        }

        public void UpdateStock(int id, int newQty)
        {
            using (var conn = OpenConnection())
                conn.Execute(
                    "UPDATE SparePart SET StockQty = @newQty WHERE Id = @id",
                    new { id, newQty });
        }

        public void RecordMovement(StockMovementEntity movement)
        {
            using (var conn = OpenConnection())
                conn.Execute(
                    @"INSERT INTO StockMovement (SparePartId, MovementType, Qty, Reference, Notes, CreatedAt)
                      VALUES (@SparePartId, @MovementType, @Qty, @Reference, @Notes, @CreatedAt)",
                    movement);
        }

        public IEnumerable<StockMovementEntity> GetMovements(int sparePartId)
        {
            using (var conn = OpenConnection())
                return conn.Query<StockMovementEntity>(
                    @"SELECT sm.*, sp.PartName
                      FROM StockMovement sm
                      JOIN SparePart sp ON sp.Id = sm.SparePartId
                      WHERE sm.SparePartId = @sparePartId
                      ORDER BY sm.CreatedAt DESC",
                    new { sparePartId });
        }
    }
}