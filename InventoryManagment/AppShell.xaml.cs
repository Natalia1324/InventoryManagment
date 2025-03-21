using System.Diagnostics;
using InventoryManagment.Data;
using InventoryManagment.Views;
namespace InventoryManagment
{
    public partial class AppShell : Shell
    {
        readonly MegaService _megaService = new();

        public AppShell()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppShell Error] {ex.Message}");
                throw;
            }
        }

        private async void OnExportDatabaseClicked(object sender, EventArgs e)
        {
            try
            {
                // Ścieżka do pliku bazy danych
                string dbPath = Path.Combine(FileSystem.AppDataDirectory, "Inventory_Managment_db.db3");
                string tempDbPath = Path.Combine(FileSystem.CacheDirectory, "Inventory_Managment_copydb.db3");

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
                Debug.WriteLine($"[Export Error] {ex.Message}");
                await DisplayAlert("Błąd", "Nie udało się wyeksportować bazy.", "OK");
            }
        }

    }
}

