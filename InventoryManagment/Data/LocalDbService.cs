using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using InventoryManagment.Models;
using SQLite;

namespace InventoryManagment.Data
{
    public class LocalDbService
    {
        private const string DB_NAME = "Inventory_Managment_db.db3";
        private readonly SQLiteAsyncConnection _connection;

        public LocalDbService()
        {
            Debug.WriteLine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DB_NAME));
            _connection = new SQLiteAsyncConnection((Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DB_NAME)));
            Task.Run(async () => await InitializeDatabase()).ConfigureAwait(false);
        }

        private async Task InitializeDatabase()
        {
            try
            {
                await _connection.CreateTableAsync<Dokumenty>();
                await _connection.CreateTableAsync<Produkty>();
                await _connection.CreateTableAsync<Transakcje>();

                if (await _connection.Table<Produkty>().CountAsync() == 0)
                {
                    await ImportProductsFromJson(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "products.json"));
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Błąd podczas inicjalizacji bazy danych", ex);
                throw new Exception("Nie zainicjalizowano bazy danych");
            }
        }


        // ==============================
        // 📌 CRUD dla Dokumenty
        // ==============================

        public async Task CreateDokument(Dokumenty dokument)
        {
            try
            {
                await _connection.InsertAsync(dokument);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Błąd przy dodawaniu dokumentu", ex);
                throw new Exception("Nie dodano dokumentu");
            }
        }

        public async Task<List<Dokumenty>?> GetDokumenty()
        {
            try
            {
                return await _connection.Table<Dokumenty>().ToListAsync();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Błąd przy pobieraniu dokumentów", ex);
                return new List<Dokumenty>();
            }
        }

        public async Task<Dokumenty?> GetDokumentById(int id)
        {
            try
            {
                return await _connection.FindAsync<Dokumenty>(id);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Błąd przy pobieraniu dokumentu o ID {id}", ex);
                return null;
            }
        }

        public async Task UpdateDokument(Dokumenty dokument)
        {
            try
            {
                await _connection.UpdateAsync(dokument);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Błąd przy aktualizacji dokumentu", ex);
                throw new Exception("Nie zaktualizowano dokumentu");
            }
        }

        public async Task DeleteDokument(int id)
        {
            try
            {
                var dokument = await GetDokumentById(id);
                if (dokument != null)
                {
                    await _connection.DeleteAsync(dokument);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Błąd przy usuwaniu dokumentu o ID {id}", ex);
                throw new Exception("Nie usunieto dokumentu");
            }
        }

        public async Task<List<Dokumenty>?> GetDocumentsForMonth(int month, int year)
        {
            try
            {
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1);

                return await _connection.Table<Dokumenty>()
                    .Where(d => d.Data_Wystawienia >= startDate && d.Data_Wystawienia < endDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Błąd przy pobieraniu dokumentów dla miesiąca {month}/{year}", ex);
                return null;
            }
        }

        // ==============================
        // 📌 CRUD dla Produkty
        // ==============================

        public async Task CreateProdukt(Produkty produkt)
        {
            try
            {
                await _connection.InsertAsync(produkt);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Błąd przy dodawaniu produktu", ex);
                throw new Exception("Nie dodano produktu");
            }
        }

        public async Task<List<Produkty>?> GetProdukty()
        {
            try
            {
                return await _connection.Table<Produkty>().ToListAsync();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Błąd przy pobieraniu produktów", ex);
                return null;
            }
        }
        // READ (Pobierz produkt po ID)
        public async Task<Produkty?> GetProduktById(int id)
        {
            try
            {
                return await _connection.FindAsync<Produkty>(id);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Błąd przy pobieraniu produktów o id {id}", ex);
                return null;
            }
        }

        public async Task<List<Produkty>> GetProduktyPaginated(int offset, int limit)
        {
            return await _connection.Table<Produkty>()
                .Where(p => !p.isDel)
                .OrderBy(p => p.Id)
                .Skip(offset)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Produkty>> SearchProdukty(string query, int limit)
        {
            return await _connection.Table<Produkty>()
                .Where(p => !p.isDel && (p.ToString().Contains(query) || p.Kolor.Contains(query)))
                .OrderBy(p => p.Rozmiar)
                .Take(limit)
                .ToListAsync();
        }

        // ==============================
        // 📌 CRUD dla Transakcje
        // ==============================

        public async Task CreateTransakcja(Transakcje transakcja)
        {
            try
            {
                await _connection.InsertAsync(transakcja);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Błąd przy dodawaniu transakcji", ex);
                throw new Exception("Nie dodano transakcji");

            }
        }

        public async Task<List<Transakcje>?> GetTransakcje()
        {
            try
            {
                return await _connection.Table<Transakcje>().ToListAsync();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Błąd przy pobieraniu transakcji", ex);
                return null;
            }
        }

        // READ (Pobierz transakcję po ID)
        public async Task<Transakcje?> GetTransakcjaById(int id)
        {
            try
            {
                return await _connection.FindAsync<Transakcje>(id);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Błąd przy pobieraniu transakcji o id {id}", ex);
                return null;
            }
        }

        // UPDATE (Aktualizuj transakcję)
        public async Task UpdateTransakcja(Transakcje transakcja)
        {
            try
            {
                await _connection.UpdateAsync(transakcja);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Błąd przy aktualizowaniu transakcji", ex);
                throw new Exception("Nie zaktualizowano transakcji");

            }
        }
        // UPDATE (Aktualizuj produkt)
        public async Task UpdateProdukt(Produkty produkt)
        {
            try
            {
                await _connection.UpdateAsync(produkt);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Błąd przy aktualizowaniu produktu", ex);
                throw new Exception("Nie zaktualizowano produktu");

            }
        }
        public async Task ImportProductsFromJson(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("Plik JSON nie istnieje", filePath);
                }

                var jsonData = await File.ReadAllTextAsync(filePath);
                var products = JsonSerializer.Deserialize<List<Produkty>>(jsonData);

                if (products != null && products.Count > 0)
                {
                    foreach (var product in products)
                    {
                        await CreateProdukt(product);
                    }
                }
                else
                {
                    ErrorLogger.LogError("Plik JSON nie zawiera poprawnych danych", new Exception("Brak danych w JSON"));
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Błąd przy importowaniu produktów z JSON", ex);
            }
        }
    }
}
