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
            await LoadTransactions();
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
            var transakcje = await _dbService.GetTransakcje();
            var dokumenty = await _dbService.GetDokumenty();
            var produkty = await _dbService.GetProdukty();

            var productTransactions = transakcje
                .Where(t => t.ProduktId == _productId)
                .Select(t => new
                {
                    Produkt = produkty.FirstOrDefault(p => p.Id == t.ProduktId)?.ToString() ?? "Nieznany produkt",
                    Przeznaczenie = dokumenty.FirstOrDefault(d => d.Id == t.DokumentId)?.Przeznaczenie ?? "",
                    TypDokumentu = dokumenty.FirstOrDefault(d => d.Id == t.DokumentId)?.Typ_Dokumentu == TypDokumentu.Rozchod_Zewnetrzny
                        ? "Rozchód"
                        : "Przychód",
                    Dostawca = t.Dostawca ?? "",
                    DataWystawienia = dokumenty.FirstOrDefault(d => d.Id == t.DokumentId)?.Data_Wystawienia,
                    ZmianaStanu = $"{(dokumenty.FirstOrDefault(d => d.Id == t.DokumentId)?.Typ_Dokumentu == TypDokumentu.Rozchod_Zewnetrzny
                        ? "-"
                        : "+")}{t.Zmiana_Stanu} szt.",
                    Notatka = t.Notatka ?? "Brak notatki"
                })
                .ToList();

            _allTransactions = productTransactions.Cast<object>().ToList();
            ApplyFilters(); // Początkowe zastosowanie filtrowania (wyświetla wszystkie)
        }

        private void OnFilterChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            string productFilter = ProductFilter.Text?.ToLower() ?? "";
            string destinationFilter = DestinationFilter.Text?.ToLower() ?? "";
            string docTypeFilter = DocumentTypeFilter.Text?.ToLower() ?? "";
            string supplierFilter = SupplierFilter.Text?.ToLower() ?? "";
            string dateFilter = DateFilter.Text?.ToLower() ?? "";
            string quantityFilter = QuantityFilter.Text?.ToLower() ?? "";
            string noteFilter = NoteFilter.Text?.ToLower() ?? "";

            var filtered = _allTransactions.Where(t =>
            {
                dynamic trans = t;

                bool matchesProduct = trans.Produkt.ToLower().Contains(productFilter);
                bool matchesDestination = trans.Przeznaczenie.ToLower().Contains(destinationFilter);
                bool matchesDocType = trans.TypDokumentu.Contains(docTypeFilter);
                bool matchesSupplier = trans.Dostawca.ToLower().Contains(supplierFilter);
                bool matchesDate = trans.DataWystawienia?.ToString("yyyy-MM-dd").ToLower().Contains(dateFilter) ?? false;
                bool matchesQuantity = trans.ZmianaStanu.ToLower().Contains(quantityFilter);
                bool matchesNote = trans.Notatka.ToLower().Contains(noteFilter);

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
    }
}
