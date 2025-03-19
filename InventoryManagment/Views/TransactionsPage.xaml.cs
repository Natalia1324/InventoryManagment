using InventoryManagment.Data;
using InventoryManagment.Models;
using System.Collections.ObjectModel;

namespace InventoryManagment.Views
{
    public partial class TransactionsPage : ContentPage
    {
        private readonly LocalDbService _dbService;

        public ObservableCollection<dynamic> Transactions { get; set; } = new ObservableCollection<dynamic>();
        public ObservableCollection<dynamic> FilteredTransactions { get; set; } = new ObservableCollection<dynamic>();
        public TransactionsPage()
        {
            InitializeComponent();
            _dbService = App.Services.GetRequiredService<LocalDbService>();
            BindingContext = this;
            //LoadTransactions();
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadTransactions();
        }
        private async void LoadTransactions()
        {
            var transakcje = await _dbService.GetTransakcje();

            Transactions.Clear();
            FilteredTransactions.Clear();

            foreach (var transakcja in transakcje)
            {
                var produkt = await _dbService.GetProduktById(transakcja.ProduktId);
                var dokument = await _dbService.GetDokumentById(transakcja.DokumentId);

                string typDokumentu = dokument.Typ_Dokumentu == TypDokumentu.Rozchod_Zewnetrzny
                    ? "Rozchód"
                    : "Przychód";

                var transaction = new
                {
                    Produkt = produkt.ToString(),
                    Przeznaczenie = dokument.Przeznaczenie,
                    TypDokumentu = typDokumentu,
                    Dostawca = transakcja.Dostawca,
                    DataWystawienia = dokument.Data_Wystawienia,
                    ZmianaStanu = transakcja.Zmiana_Stanu,
                    Notatka = transakcja.Notatka
                };

                Transactions.Add(transaction);
                FilteredTransactions.Add(transaction);
            }
        }

        private void OnFilterChanged(object sender, TextChangedEventArgs e)
        {
            FilteredTransactions.Clear();

            var filtered = Transactions.Where(t =>
                (string.IsNullOrEmpty(ProductFilter.Text) || t.Produkt.Contains(ProductFilter.Text, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(DestinationFilter.Text) || t.Przeznaczenie.Contains(DestinationFilter.Text, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(DocumentTypeFilter.Text) || t.TypDokumentu.Contains(DocumentTypeFilter.Text, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(SupplierFilter.Text) || t.Dostawca.Contains(SupplierFilter.Text, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(DateFilter.Text) || t.DataWystawienia.ToString("yyyy-MM-dd").Contains(DateFilter.Text)) &&
                (string.IsNullOrEmpty(QuantityFilter.Text) || t.ZmianaStanu.ToString().Contains(QuantityFilter.Text)) &&
                (string.IsNullOrEmpty(NoteFilter.Text) || t.Notatka.Contains(NoteFilter.Text, StringComparison.OrdinalIgnoreCase))
            );

            foreach (var transaction in filtered)
            {
                FilteredTransactions.Add(transaction);
            }

        }
    }
}

