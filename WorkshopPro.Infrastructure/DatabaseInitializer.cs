using System.Data.SQLite;
using System;
using System.IO;


namespace WorkshopPro.Infrastructure
{
    public class DatabaseInitializer
    {
        private readonly string _dbPath;

        public DatabaseInitializer(string dbPath)
        {
            _dbPath = dbPath;
        }

        public void Initialize()
        {
            string dir = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                CreateTables(connection);
                Console.WriteLine($"[DB] Database ready at: {_dbPath}");
            }
        }

        private void CreateTables(SQLiteConnection connection)
        {
            ExecuteNonQuery(connection, "PRAGMA foreign_keys = ON;");

            // ── Seed / master tables ───────────────────────────────────────

            ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS Manufacturer (
                    Id      INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name    TEXT NOT NULL,
                    Country TEXT
                );");

            ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS VehicleModel (
                    Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                    ManufacturerId   INTEGER NOT NULL REFERENCES Manufacturer(Id),
                    ModelName        TEXT NOT NULL,
                    FuelType         TEXT NOT NULL,
                    EngineCC         INTEGER,
                    TransmissionType TEXT,
                    YearFrom         INTEGER,
                    YearTo           INTEGER
                );");

            // NEW in Phase 5: pre-seeded labor job names
            // Admins pick from this list when adding a labor line to an invoice.
            // Price is NOT stored here — it varies per job and is entered per-invoice.
            ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS LaborService (
                    Id   INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL
                );");

            // ── Operational tables ─────────────────────────────────────────

            ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS Customer (
                    Id      INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name    TEXT NOT NULL,
                    Phone   TEXT,
                    Address TEXT
                );");

            ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS Vehicle (
                    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlateNumber    TEXT NOT NULL UNIQUE,
                    CustomerId     INTEGER REFERENCES Customer(Id),
                    VehicleModelId INTEGER REFERENCES VehicleModel(Id),
                    Color          TEXT,
                    Year           INTEGER,
                    Notes          TEXT
                );");

            ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS SparePart (
                    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                    PartCode  TEXT NOT NULL UNIQUE,
                    PartName  TEXT NOT NULL,
                    Category  TEXT,
                    Unit      TEXT,
                    StockQty  INTEGER NOT NULL DEFAULT 0,
                    PriceBuy  REAL,
                    PriceSell REAL
                );");

            ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS StockMovement (
                    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                    SparePartId  INTEGER NOT NULL REFERENCES SparePart(Id),
                    MovementType TEXT NOT NULL,
                    Qty          INTEGER NOT NULL,
                    Reference    TEXT,
                    Notes        TEXT,
                    CreatedAt    TEXT NOT NULL
                );");

            ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS Invoice (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    InvoiceNo   TEXT NOT NULL UNIQUE,
                    VehicleId   INTEGER NOT NULL REFERENCES Vehicle(Id),
                    CustomerId  INTEGER NOT NULL REFERENCES Customer(Id),
                    ServiceDate TEXT NOT NULL,
                    TotalAmount REAL NOT NULL DEFAULT 0,
                    Status      TEXT NOT NULL DEFAULT 'Draft',
                    Notes       TEXT,
                    CreatedAt   TEXT NOT NULL
                );");

            ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS InvoiceItem (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    InvoiceId   INTEGER NOT NULL REFERENCES Invoice(Id),
                    ItemType    TEXT NOT NULL,
                    SparePartId INTEGER,
                    Description TEXT NOT NULL,
                    Qty         INTEGER NOT NULL DEFAULT 1,
                    UnitPrice   REAL NOT NULL,
                    Subtotal    REAL NOT NULL
                );");

            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_vehicle_plate    ON Vehicle(PlateNumber);");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_invoice_date     ON Invoice(ServiceDate);");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_invoice_vehicle  ON Invoice(VehicleId);");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_stockmovement_part ON StockMovement(SparePartId);");

            Console.WriteLine("[DB] All tables verified.");
        }

        private static void ExecuteNonQuery(SQLiteConnection connection, string sql)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
