using CsvHelper;
using CsvHelper.Configuration;
using Dapper;
using System.Data.SQLite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkshopPro.Infrastructure
{
    public class SeedLoader
    {
        private readonly string _dbPath;
        private readonly string _dataFolder;

        public SeedLoader(string dbPath, string dataFolder)
        {
            _dbPath = dbPath;
            _dataFolder = dataFolder;
        }

        public void SeedAll()
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                SeedManufacturers(connection);
                SeedVehicleModels(connection);
                SeedLaborServices(connection);  // NEW in Phase 5
            }
        }

        // ── Manufacturers ──────────────────────────────────────────────────

        private void SeedManufacturers(SQLiteConnection connection)
        {
            int count = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Manufacturer");
            if (count > 0)
            {
                Console.WriteLine("[Seed] Manufacturers already seeded — skipping.");
                return;
            }

            string csvPath = Path.Combine(_dataFolder, "manufacturers_updated.csv");
            if (!File.Exists(csvPath))
            {
                Console.WriteLine($"[Seed] WARNING: {csvPath} not found — skipping manufacturers.");
                return;
            }

            using (var reader = new StreamReader(csvPath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            { Delimiter = ";" }))
            {
                foreach (var row in csv.GetRecords<ManufacturerCsvRow>())
                    connection.Execute(
                        "INSERT INTO Manufacturer (Name, Country) VALUES (@Name, @Country)",
                        new { row.Name, row.Country });
            }
            Console.WriteLine("[Seed] Manufacturers seeded.");
        }

        // ── Vehicle Models ─────────────────────────────────────────────────

        private void SeedVehicleModels(SQLiteConnection connection)
        {
            int count = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM VehicleModel");
            if (count > 0)
            {
                Console.WriteLine("[Seed] VehicleModels already seeded — skipping.");
                return;
            }

            string csvPath = Path.Combine(_dataFolder, "vehicle_models_ultra_complete.csv");
            if (!File.Exists(csvPath))
            {
                Console.WriteLine($"[Seed] WARNING: {csvPath} not found — skipping vehicle models.");
                return;
            }

            using (var reader = new StreamReader(csvPath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            { Delimiter = ";" }))
            {
                foreach (var row in csv.GetRecords<VehicleModelCsvRow>())
                    connection.Execute(
                        @"INSERT INTO VehicleModel
                            (ManufacturerId, ModelName, FuelType, EngineCC, TransmissionType, YearFrom, YearTo)
                          VALUES
                            (@ManufacturerId, @ModelName, @FuelType, @EngineCC, @TransmissionType, @YearFrom, @YearTo)",
                        new
                        {
                            row.ManufacturerId,
                            row.ModelName,
                            row.FuelType,
                            row.EngineCC,
                            row.TransmissionType,
                            row.YearFrom,
                            YearTo = string.IsNullOrWhiteSpace(row.YearTo)
                                        ? (int?)null
                                        : int.Parse(row.YearTo)
                        });
            }
            Console.WriteLine("[Seed] VehicleModels seeded.");
        }

        // ── Labor Services (NEW) ───────────────────────────────────────────
        // Reads labor_services.csv from the Data/ folder.
        // Same "skip if already seeded" logic as the others — safe to call on every start.

        private void SeedLaborServices(SQLiteConnection connection)
        {
            int count = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM LaborService");
            if (count > 0)
            {
                Console.WriteLine("[Seed] LaborServices already seeded — skipping.");
                return;
            }

            string csvPath = Path.Combine(_dataFolder, "labor_services.csv");
            if (!File.Exists(csvPath))
            {
                Console.WriteLine($"[Seed] WARNING: {csvPath} not found — skipping labor services.");
                return;
            }

            // labor_services.csv uses a plain comma delimiter (default)
            using (var reader = new StreamReader(csvPath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                foreach (var row in csv.GetRecords<LaborServiceCsvRow>())
                    connection.Execute(
                        "INSERT INTO LaborService (Name) VALUES (@Name)",
                        new { row.Name });
            }
            Console.WriteLine("[Seed] LaborServices seeded.");
        }

        // ── Private CSV row DTOs ───────────────────────────────────────────
        // WHY private inner classes and not the Entity classes themselves?
        // CSV column names might differ from DB column names. These DTOs are
        // purely for parsing — they never leave this class.

        private class ManufacturerCsvRow
        {
            public string Name { get; set; }
            public string Country { get; set; }
        }

        private class VehicleModelCsvRow
        {
            public int ManufacturerId { get; set; }
            public string ModelName { get; set; }
            public string FuelType { get; set; }
            public int EngineCC { get; set; }
            public string TransmissionType { get; set; }
            public int YearFrom { get; set; }
            public string YearTo { get; set; }
        }

        private class LaborServiceCsvRow
        {
            public string Name { get; set; }
        }
    }
}
