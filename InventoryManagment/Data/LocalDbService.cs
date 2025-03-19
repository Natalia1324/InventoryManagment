using System;
using System.Collections.Generic;
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
            _connection = new SQLiteAsyncConnection(Path.Combine(FileSystem.AppDataDirectory, DB_NAME));
            Task.Run(async () => await InitializeDatabase()).ConfigureAwait(false);
        }

        private async Task InitializeDatabase()
        {
            try
            {
                // Najpierw upewniamy się, że tabele istnieją
                await _connection.CreateTableAsync<Dokumenty>();
                await _connection.CreateTableAsync<Produkty>();
                await _connection.CreateTableAsync<Transakcje>();

                // Teraz sprawdzamy, czy tabele są puste
                var dokumentyCount = await _connection.Table<Dokumenty>().CountAsync();
                var produktyCount = await _connection.Table<Produkty>().CountAsync();
                var transakcjeCount = await _connection.Table<Transakcje>().CountAsync();

                // Jeśli tabela Produktów jest pusta, importujemy dane
                if (produktyCount == 0)
                {
                    await ImportProductsFromJson("C:\\Users\\natal\\source\\repos\\InventoryManagment\\InventoryManagment\\products.json");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Database initialization error: {ex.Message}");
            }
        }



        // ==============================
        // 📌 CRUD dla Dokumenty
        // ==============================

        // CREATE (Dodaj dokument)
        public async Task CreateDokument(Dokumenty dokument)
        {
            await _connection.InsertAsync(dokument);
        }

        // READ (Pobierz wszystkie dokumenty)
        public async Task<List<Dokumenty>> GetDokumenty()
        {
            return await _connection.Table<Dokumenty>().ToListAsync();
        }

        // READ (Pobierz dokument po ID)
        public async Task<Dokumenty> GetDokumentById(int id)
        {
            return await _connection.FindAsync<Dokumenty>(id);
        }

        // UPDATE (Aktualizuj dokument)
        public async Task UpdateDokument(Dokumenty dokument)
        {
            await _connection.UpdateAsync(dokument);
        }

        // DELETE (Usuń dokument)
        public async Task DeleteDokument(int id)
        {
            var dokument = await GetDokumentById(id);
            if (dokument != null)
            {
                await _connection.DeleteAsync(dokument);
            }
        }

        public async Task<List<Dokumenty>> GetDocumentsForMonth(int month, int year)
        {
            // Obliczenie zakresu dat dla danego miesiąca
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            // Zapytanie SQLite dla zakresu dat
            return await _connection.Table<Dokumenty>()
                .Where(d => d.Data_Wystawienia >= startDate && d.Data_Wystawienia < endDate)
                .ToListAsync();
        }


        // ==============================
        // 📌 CRUD dla Produkty
        // ==============================

        // CREATE (Dodaj produkt)
        public async Task CreateProdukt(Produkty produkt)
        {
            await _connection.InsertAsync(produkt);
        }

        // READ (Pobierz wszystkie produkty)
        public async Task<List<Produkty>> GetProdukty()
        {
            return await _connection.Table<Produkty>().ToListAsync();
        }

        // READ (Pobierz produkt po ID)
        public async Task<Produkty> GetProduktById(int id)
        {
            return await _connection.FindAsync<Produkty>(id);
        }

        // UPDATE (Aktualizuj produkt)
        public async Task UpdateProdukt(Produkty produkt)
        {
            await _connection.UpdateAsync(produkt);
        }

        // DELETE (Usuń produkt)
        public async Task DeleteProdukt(int id)
        {
            var produkt = await GetProduktById(id);
            if (produkt != null)
            {
                await _connection.DeleteAsync(produkt);
            }
        }

        // ==============================
        // 📌 CRUD dla Transakcje
        // ==============================

        // CREATE (Dodaj transakcję)
        public async Task CreateTransakcja(Transakcje transakcja)
        {
            await _connection.InsertAsync(transakcja);
        }

        // READ (Pobierz wszystkie transakcje)
        public async Task<List<Transakcje>> GetTransakcje()
        {
            return await _connection.Table<Transakcje>().ToListAsync();
        }

        // READ (Pobierz transakcję po ID)
        public async Task<Transakcje> GetTransakcjaById(int id)
        {
            return await _connection.FindAsync<Transakcje>(id);
        }

        // UPDATE (Aktualizuj transakcję)
        public async Task UpdateTransakcja(Transakcje transakcja)
        {
            await _connection.UpdateAsync(transakcja);
        }

        // DELETE (Usuń transakcję)
        public async Task DeleteTransakcja(int id)
        {
            var transakcja = await GetTransakcjaById(id);
            if (transakcja != null)
            {
                await _connection.DeleteAsync(transakcja);
            }
        }
        // READ (Transakcja per Dokument)
        public async Task<List<Transakcje>> GetTransakcjeForDokument(int dokumentId)
        {
            return await _connection.Table<Transakcje>().Where(t => t.DokumentId == dokumentId).ToListAsync();
        }


        public async Task ImportProductsFromJson(string filePath)
        {
            try
            {
                // Odczytanie zawartości pliku JSON
                var jsonData = await File.ReadAllTextAsync(filePath);

                // Deserializacja JSON do listy obiektów Produkty
                var products = JsonSerializer.Deserialize<List<Produkty>>(jsonData);

                if (products != null && products.Count > 0)
                {
                    foreach (var product in products)
                    {
                        // Dodanie produktu do bazy danych
                        await CreateProdukt(product);
                    }

                    Console.WriteLine($"✔️ Zaimportowano {products.Count} produktów do bazy danych.");
                }
                else
                {
                    Console.WriteLine("⚠️ Brak danych w pliku JSON lub plik jest pusty.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Wystąpił błąd podczas importowania produktów: {ex.Message}");
            }
        }

    }
}
