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

            RenderTransactionList(FilteredTransactions.ToList());
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
            ).ToList();

            foreach (var transaction in filtered)
            {
                FilteredTransactions.Add(transaction);
            }

            RenderTransactionList(filtered);
        }

        private void RenderTransactionList(List<dynamic> transactions)
        {
            TransactionRowsStack.Children.Clear();

            for (int i = 0; i < transactions.Count; i++)
            {
                var item = transactions[i];

                var grid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Star }
                    },
                    Padding = new Thickness(5, 2),
                    BackgroundColor = i % 2 == 0 ? Colors.White : Color.FromArgb("#f0f0f0"),
                    RowSpacing = 1,
                    ColumnSpacing = 1
                };

                // Kolumny danych transakcji
                grid.Add(new Label
                {
                    Text = item.Produkt,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontSize = 14
                }, 0, 0);

                grid.Add(new Label
                {
                    Text = item.Przeznaczenie,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontSize = 14
                }, 1, 0);

                grid.Add(new Label
                {
                    Text = item.TypDokumentu,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontSize = 14
                }, 2, 0);

                grid.Add(new Label
                {
                    Text = item.Dostawca,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontSize = 14
                }, 3, 0);

                grid.Add(new Label
                {
                    Text = item.DataWystawienia.ToString("dd-MM-yyyy"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontSize = 14
                }, 4, 0);

                grid.Add(new Label
                {
                    Text = item.ZmianaStanu.ToString(),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontSize = 14
                }, 5, 0);

                grid.Add(new Label
                {
                    Text = item.Notatka,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontSize = 14
                }, 6, 0);

                TransactionRowsStack.Children.Add(grid);
            }
        }
    }
}
