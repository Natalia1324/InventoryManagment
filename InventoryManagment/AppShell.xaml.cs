using System.Diagnostics;
using CommunityToolkit.Maui.Views;
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
            var popup = new LoadingPopup();
            try
            {

                Application.Current.MainPage.ShowPopup(popup);
                // Ścieżka do pliku bazy danych
                string dbPath = Path.Combine(FileSystem.AppDataDirectory, "Inventory_Managment_db.db3");
                string tempDbPath = Path.Combine(FileSystem.CacheDirectory, "Inventory_Managment_copydb.db3");
                ErrorLogger.LogError($"{Path.Combine(FileSystem.AppDataDirectory, "Inventory_Managment_db.db3")}", new Exception(""));
                Debug.WriteLine($"{Path.Combine(FileSystem.AppDataDirectory, "Inventory_Managment_db.db3")}");
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

    }
}

