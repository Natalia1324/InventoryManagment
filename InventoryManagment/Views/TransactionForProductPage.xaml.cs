using InventoryManagment.Data;
using InventoryManagment.Models;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace InventoryManagment.Views
{
    public partial class TransactionForProductPage : ContentPage
    {
        private readonly LocalDbService _dbService;
        private int _productId;

        private List<object> _allTransactions; // Przechowuje wszystkie transakcje do filtrowania

        public ObservableCollection<object> FilteredTransactions { get; set; }

        public TransactionForProductPage(int productId)
        {
            InitializeComponent();
            _dbService = App.Services.GetRequiredService<LocalDbService>();
            _productId = productId;

            FilteredTransactions = new ObservableCollection<object>();
            _allTransactions = new List<object>();

            BindingContext = this;

            MauiProgram.OnKeyDown += HandleKeyDown;
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            this.Focus();
            try
            {
                await LoadTransactions();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Błąd przy ładowaniu transakcji", ex);
                await DisplayAlert("Błąd", "Nie udało się pobrać listy transakcji.", "OK");
            }
        }
        private void HandleKeyDown(FrameworkElement sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                Navigation.PopAsync(); // Powrót do poprzedniej strony
            }
        }
        private async Task LoadTransactions()
        {
            try
            {
                var transakcje = await _dbService.GetTransakcje();
                var dokumenty = await _dbService.GetDokumenty();
                var produkty = await _dbService.GetProdukty();

                if (transakcje == null || dokumenty == null || produkty == null)
                {
                    throw new Exception("Błąd pobierania danych z bazy");
                }

                var productTransactions = transakcje
                    .Where(t => t != null && t.ProduktId == _productId)
                    .Select(t =>
                    {
                        var produkt = produkty.FirstOrDefault(p => p?.Id == t?.ProduktId)?.ToString() ?? "Nieznany produkt";
                        var dokument = dokumenty.FirstOrDefault(d => d?.Id == t?.DokumentId);
                        var typDokumentu = dokument?.Typ_Dokumentu == TypDokumentu.Rozchod_Zewnetrzny ? "Rozchód" : "Przychód";
                        var zmianaStanu = $"{(dokument?.Typ_Dokumentu == TypDokumentu.Rozchod_Zewnetrzny ? "-" : "+")}{t?.Zmiana_Stanu ?? 0} szt.";

                        return new
                        {
                            Produkt = produkt,
                            Przeznaczenie = dokument?.Przeznaczenie ?? "",
                            TypDokumentu = typDokumentu,
                            Dostawca = t?.Dostawca ?? "",
                            DataWystawienia = dokument?.Data_Wystawienia,
                            ZmianaStanu = zmianaStanu,
                            Notatka = t?.Notatka ?? "Brak notatki"
                        };
                    })
                    .ToList();

                _allTransactions = productTransactions.Cast<object>().ToList();
                ApplyFilters(); // Początkowe zastosowanie filtrowania (wyświetla wszystkie)
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd ładowania transakcji: {ex.Message}");
                await DisplayAlert("Błąd", "Nie udało się pobrać danych o transakcjach.", "OK");
            }
        }

        private void OnFilterChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                ApplyFilters();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas filtrowania: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            try
            {
                string productFilter = ProductFilter?.Text?.ToLower() ?? "";
                string destinationFilter = DestinationFilter?.Text?.ToLower() ?? "";
                string docTypeFilter = DocumentTypeFilter?.Text?.ToLower() ?? "";
                string supplierFilter = SupplierFilter?.Text?.ToLower() ?? "";
                string dateFilter = DateFilter?.Text?.ToLower() ?? "";
                string quantityFilter = QuantityFilter?.Text?.ToLower() ?? "";
                string noteFilter = NoteFilter?.Text?.ToLower() ?? "";

                var filtered = _allTransactions.Where(t =>
                {
                    dynamic trans = t;

                    bool matchesProduct = trans.Produkt?.ToLower().Contains(productFilter) ?? false;
                    bool matchesDestination = trans.Przeznaczenie?.ToLower().Contains(destinationFilter) ?? false;
                    bool matchesDocType = trans.TypDokumentu?.Contains(docTypeFilter) ?? false;
                    bool matchesSupplier = trans.Dostawca?.ToLower().Contains(supplierFilter) ?? false;
                    bool matchesDate = trans.DataWystawienia?.ToString("yyyy-MM-dd").ToLower().Contains(dateFilter) ?? false;
                    bool matchesQuantity = trans.ZmianaStanu?.ToLower().Contains(quantityFilter) ?? false;
                    bool matchesNote = trans.Notatka?.ToLower().Contains(noteFilter) ?? false;

                    return matchesProduct &&
                           matchesDestination &&
                           matchesDocType &&
                           matchesSupplier &&
                           matchesDate &&
                           matchesQuantity &&
                           matchesNote;
                }).ToList();

                FilteredTransactions.Clear();
                foreach (var item in filtered)
                {
                    FilteredTransactions.Add(item);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas stosowania filtrów: {ex.Message}");
            }
        }
    }
}

