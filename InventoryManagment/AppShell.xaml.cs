using System.Diagnostics;
using CommunityToolkit.Maui.Views;
using InventoryManagment.Data;
using InventoryManagment.Views;
using InventoryManagment.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;

namespace InventoryManagment
{
    public partial class AppShell : Shell
    {
        readonly MegaService _megaService = new();
        private LocalDbService _dbService;
        private List<ProductWithStock> _productWithStock = new();

        public AppShell()
        {
            try
            {
                InitializeComponent();
                _dbService = App.Services.GetRequiredService<LocalDbService>();

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppShell Error] {ex.Message}");
                throw;
            }
        }

        public async void ExportStockToPdf(object sender, EventArgs e)
        {
            try
            {
                var datePickerPage = new DateSelectPage();
                await Navigation.PushModalAsync(datePickerPage);
                var selectedDate = await datePickerPage.GetSelectedDateAsync();

                if (selectedDate == null)
                    return; // użytkownik anulował

                var date = selectedDate.Value;

                QuestPDF.Settings.License = LicenseType.Community;
                var fileName = $"Remanent_{date:yyyyMMdd_HHmmss}.pdf";
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                var produkty = (await _dbService.GetProdukty())?.Where(p => !p.isDel).ToList() ?? new List<Produkty>();
                var transakcje = await _dbService.GetTransakcje();
                var dokumenty = (await _dbService.GetDokumenty())?.Where(d => d.Data_Wystawienia < date).ToList();

                var productList = produkty
                    .OrderBy(p => p.ToString())
                    .Select(p => new ProductWithStock
                    {
                        Produkt = p,
                        Stock = transakcje.Where(t => t.ProduktId == p.Id)
                                          .Sum(t => GetStockChange(t, dokumenty))
                    })
                    .Where(item => item.Stock > 0)  // Filtrujemy produkty, których Stock jest większy niż 0
                    .ToList();


                // 📄 Tworzenie dokumentu PDF
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(40);
                        page.Size(PageSizes.A4);
                        page.DefaultTextStyle(x => x.FontSize(12));

                        // 🧾 Nagłówek
                        page.Header().Text($"Remanent na dzień {date:dd.MM.yyyy}")
                        .SemiBold().FontSize(16).AlignCenter();

                        // 📋 Tabela
                        page.Content().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); // Nazwa
                                columns.RelativeColumn(1); // Stan
                            });

                            // Nagłówki kolumn
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Nazwa produktu");
                                header.Cell().Element(CellStyle).Text("Stan magazynowy");

                                static QuestPDF.Infrastructure.IContainer CellStyle(QuestPDF.Infrastructure.IContainer container) =>
                                    container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5)
                                             .Background(QuestPDF.Helpers.Colors.Grey.Lighten3).BorderBottom(1);
                            });

                            // Wiersze danych
                            foreach (var item in productList)
                            {
                                table.Cell().Text(item.Produkt.ToStringFull() ?? "Nieznany produkt");
                                table.Cell().Text(item.Stock.ToString());
                            }
                        });
                    });
                }).GeneratePdf(filePath);

                await DisplayAlert("Sukces", $"Plik PDF zapisano jako: {filePath}", "OK");
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Export PDF: Błąd podczas eksportu danych", ex);
                await DisplayAlert("Błąd", "Nie udało się wyeksportować danych do PDF.", "OK");
            }
        }

    private static int GetStockChange(Transakcje t, List<Dokumenty>? dokumenty)
        {
            try
            {
                var dokument = dokumenty?.FirstOrDefault(d => d?.Id == t?.DokumentId);
                if (dokument == null) return 0;

                return dokument.Typ_Dokumentu switch
                {
                    TypDokumentu.Przychod_Zewnetrzny => t.Zmiana_Stanu,
                    TypDokumentu.Przychod_Wewnetrzny => t.Zmiana_Stanu,
                    TypDokumentu.Rozchod_Zewnetrzny => -t.Zmiana_Stanu,
                    _ => 0
                };
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Problem z pobieraniem stanów magazynowych", ex);
            }
            return 0;
        }
        private async void OnExportDatabaseClicked(object sender, EventArgs e)
        {
            var popup = new LoadingPopup();
            try
            {

                Application.Current.MainPage.ShowPopup(popup);
                // Ścieżka do pliku bazy danych
                string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Inventory_Managment_db.db3");
                string tempDbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Inventory_Managment_copydb.db3");
                ErrorLogger.LogError($"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Inventory_Managment_db.db3")}", new Exception(""));
                Debug.WriteLine($"AppShell path: {dbPath}");
                File.Copy(dbPath, tempDbPath, overwrite: true);

                if (!File.Exists(tempDbPath))
                {
                    await DisplayAlert("Błąd", "Plik bazy danych nie istnieje.", "OK");
                    return;
                }

                // Inicjalizacja i logowanie do MEGA
                var megaService = new MegaService();
                await Task.Run(() => megaService.Login());

                // Upload pliku
                await Task.Run(() => megaService.UploadFile(tempDbPath));

                await DisplayAlert("Sukces", "Baza danych została wyeksportowana do MEGA.", "OK");

                megaService.Logout();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Błąd przy dodawaniu bazy danych", ex);
                await DisplayAlert("Błąd", "Nie udało się wyeksportować bazy.", "OK");
            }
            finally
            {
                popup.Close(); // Zamknij popup
            }
        }

        public class ProductWithStock
        {
            public Produkty? Produkt { get; set; }
            public int Stock { get; set; }
        }
    }
}

