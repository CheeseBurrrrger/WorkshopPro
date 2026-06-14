using System;
using System.Collections.Generic;
using Dapper;
using System.Data.SQLite;
using WorkshopPro.Domain;
using WorkshopPro.Application;

namespace WorkshopPro.Infrastructure.Repositories
{
    public class SqliteInvoiceRepository : IInvoiceRepository
    {
        private readonly string _dbPath;
        public SqliteInvoiceRepository(string dbPath) { _dbPath = dbPath; }

        private SQLiteConnection OpenConnection()
            => new SQLiteConnection($"Data Source={_dbPath};Version=3;");

        // ── INSERT invoice header + items in one transaction ───────────────
        // A transaction means: either ALL inserts succeed, or NONE do.
        // Java equivalent: entityManager.getTransaction().begin() / commit()
        public int Insert(InvoiceEntity invoice)
        {
            using (var conn = OpenConnection())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        int invoiceId = conn.ExecuteScalar<int>(
                            @"INSERT INTO Invoice
                                (InvoiceNo, VehicleId, CustomerId, ServiceDate,
                                 TotalAmount, Status, Notes, CreatedAt)
                              VALUES
                                (@InvoiceNo, @VehicleId, @CustomerId, @ServiceDate,
                                 @TotalAmount, @Status, @Notes, @CreatedAt);
                              SELECT last_insert_rowid();",
                            invoice, tx);

                        foreach (var item in invoice.Items)
                        {
                            item.InvoiceId = invoiceId;
                            conn.Execute(
                                @"INSERT INTO InvoiceItem
                                    (InvoiceId, ItemType, SparePartId,
                                     Description, Qty, UnitPrice, Subtotal)
                                  VALUES
                                    (@InvoiceId, @ItemType, @SparePartId,
                                     @Description, @Qty, @UnitPrice, @Subtotal)",
                                item, tx);
                        }

                        tx.Commit();
                        return invoiceId;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        // ── READ ───────────────────────────────────────────────────────────
        public InvoiceEntity GetById(int id)
        {
            using (var conn = OpenConnection())
            {
                return conn.QueryFirstOrDefault<InvoiceEntity>(
                    "SELECT * FROM Invoice WHERE Id = @id", new { id });
            }
        }

        public IEnumerable<InvoiceEntity> GetByDateRange(DateTime from, DateTime to)
        {
            using (var conn = OpenConnection())
            {
                return conn.Query<InvoiceEntity>(
                    @"SELECT * FROM Invoice
                      WHERE ServiceDate BETWEEN @from AND @to
                      ORDER BY ServiceDate DESC",
                    new { from = from.ToString("yyyy-MM-dd"), to = to.ToString("yyyy-MM-dd") });
            }
        }

        public IEnumerable<InvoiceEntity> GetByVehicle(int vehicleId)
        {
            using (var conn = OpenConnection())
            {
                return conn.Query<InvoiceEntity>(
                    "SELECT * FROM Invoice WHERE VehicleId = @vehicleId ORDER BY ServiceDate DESC",
                    new { vehicleId });
            }
        }

        public IEnumerable<InvoiceEntity> GetByCustomer(int customerId)
        {
            using (var conn = OpenConnection())
            {
                return conn.Query<InvoiceEntity>(
                    "SELECT * FROM Invoice WHERE CustomerId = @customerId ORDER BY ServiceDate DESC",
                    new { customerId });
            }
        }

        // ── SUMMARY QUERIES (for invoice list panel) ───────────────────────
        // WHY LEFT JOIN and not INNER JOIN?
        // LEFT JOIN returns the invoice even if the vehicle/customer row is somehow
        // missing (data integrity edge case). INNER JOIN would silently hide those
        // invoices. We'd rather see them with nulls than lose them entirely.
        public IEnumerable<InvoiceSummaryEntity> GetRecentSummaries(int limit = 200)
        {
            using (var conn = OpenConnection())
            {
                return conn.Query<InvoiceSummaryEntity>(
                    @"SELECT i.Id, i.InvoiceNo, i.ServiceDate, i.TotalAmount, i.Status,
                             v.PlateNumber,
                             c.Name AS CustomerName
                      FROM Invoice i
                      LEFT JOIN Vehicle  v ON v.Id = i.VehicleId
                      LEFT JOIN Customer c ON c.Id = i.CustomerId
                      ORDER BY i.ServiceDate DESC, i.Id DESC
                      LIMIT @limit",
                    new { limit });
            }
        }

        public IEnumerable<InvoiceSummaryEntity> Search(string term)
        {
            using (var conn = OpenConnection())
            {
                return conn.Query<InvoiceSummaryEntity>(
                    @"SELECT i.Id, i.InvoiceNo, i.ServiceDate, i.TotalAmount, i.Status,
                             v.PlateNumber,
                             c.Name AS CustomerName
                      FROM Invoice i
                      LEFT JOIN Vehicle  v ON v.Id = i.VehicleId
                      LEFT JOIN Customer c ON c.Id = i.CustomerId
                      WHERE v.PlateNumber LIKE @term
                         OR i.InvoiceNo   LIKE @term
                         OR c.Name        LIKE @term
                      ORDER BY i.ServiceDate DESC, i.Id DESC
                      LIMIT 100",
                    new { term = $"%{term}%" });
            }
        }

        // ── STATUS + TOTAL UPDATES ─────────────────────────────────────────
        public void UpdateStatus(int invoiceId, string status)
        {
            using (var conn = OpenConnection())
            {
                conn.Execute(
                    "UPDATE Invoice SET Status = @status WHERE Id = @invoiceId",
                    new { invoiceId, status });
            }
        }

        public void UpdateTotal(int invoiceId, decimal total)
        {
            using (var conn = OpenConnection())
            {
                conn.Execute(
                    "UPDATE Invoice SET TotalAmount = @total WHERE Id = @invoiceId",
                    new { invoiceId, total });
            }
        }

        // ── INVOICE NUMBER GENERATION ──────────────────────────────────────
        public string GenerateNextInvoiceNo()
        {
            int year = DateTime.Now.Year;
            int count = CountThisYear(year);
            return $"INV-{year}-{(count + 1):D4}";
        }

        public int CountThisYear (int year)
        {
            using (var conn = OpenConnection())
            {
                return conn.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM Invoice WHERE InvoiceNo LIKE @pattern",
                    new { pattern = $"INV-{year}-%" });
            }
        }

        // ── LINE ITEMS ─────────────────────────────────────────────────────
        public int InsertItem(InvoiceItemEntity item)
        {
            using (var conn = OpenConnection())
            {
                return conn.ExecuteScalar<int>(
                    @"INSERT INTO InvoiceItem
                        (InvoiceId, ItemType, SparePartId, Description, Qty, UnitPrice, Subtotal)
                      VALUES
                        (@InvoiceId, @ItemType, @SparePartId, @Description, @Qty, @UnitPrice, @Subtotal);
                      SELECT last_insert_rowid();",
                    item);
            }
        }

        public IEnumerable<InvoiceItemEntity> GetItemsByInvoiceId(int invoiceId)
        {
            using (var conn = OpenConnection())
            {
                return conn.Query<InvoiceItemEntity>(
                    "SELECT * FROM InvoiceItem WHERE InvoiceId = @invoiceId ORDER BY Id",
                    new { invoiceId });
            }
        }

        public void DeleteItem(int itemId)
        {
            using (var conn = OpenConnection())
            {
                conn.Execute("DELETE FROM InvoiceItem WHERE Id = @itemId", new { itemId });
            }
        }
    }
}