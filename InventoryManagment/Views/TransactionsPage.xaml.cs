using InventoryManagment.Data;
using InventoryManagment.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;

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
            try
            {
                LoadTransactions();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Błąd przy ładowaniu transakcji", ex);
                DisplayAlert("Błąd", "Nie udało się pobrać listy transakcji.", "OK");
            }
        }

        private async void LoadTransactions()
        {
            try
            {
                var transakcje = await _dbService.GetTransakcje();
                if (transakcje == null)
                {
                    throw new Exception("Błąd pobierania transakcji.");
                }

                Transactions.Clear();
                FilteredTransactions.Clear();

                foreach (var transakcja in transakcje)
                {
                    if (transakcja == null) continue;

                    var produkt = await _dbService.GetProduktById(transakcja.ProduktId) ?? new Produkty { Rozmiar = "Nieznany produkt" };
                    var dokument = await _dbService.GetDokumentById(transakcja.DokumentId);

                    if (dokument == null) continue;

                    string typDokumentu = dokument?.Typ_Dokumentu == TypDokumentu.Rozchod_Zewnetrzny ? "Rozchód" : "Przychód";

                    var transaction = new
                    {
                        Produkt = produkt.ToString(),
                        Przeznaczenie = dokument?.Przeznaczenie ?? "Brak danych",
                        TypDokumentu = typDokumentu,
                        Dostawca = transakcja?.Dostawca ?? "Nieznany",
                        DataWystawienia = dokument?.Data_Wystawienia.ToString("yyyy-MM-dd") ?? "Brak daty",
                        ZmianaStanu = transakcja?.Zmiana_Stanu ?? 0,
                        Notatka = transakcja?.Notatka ?? "Brak notatki"
                    };

                    Transactions.Add(transaction);
                    FilteredTransactions.Add(transaction);
                }

                RenderTransactionList(FilteredTransactions.ToList());
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Błąd przy ładowaniu transakcji", ex);
                await DisplayAlert("Błąd", "Nie udało się pobrać danych o transakcjach.", "OK");
            }
        }


        private void OnFilterChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                FilteredTransactions.Clear();

                var filtered = Transactions.Where(t =>
                    (string.IsNullOrEmpty(ProductFilter?.Text) || (t.Produkt?.Contains(ProductFilter.Text, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                    (string.IsNullOrEmpty(DestinationFilter?.Text) || (t.Przeznaczenie?.Contains(DestinationFilter.Text, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                    (string.IsNullOrEmpty(DocumentTypeFilter?.Text) || (t.TypDokumentu?.Contains(DocumentTypeFilter.Text, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                    (string.IsNullOrEmpty(SupplierFilter?.Text) || (t.Dostawca?.Contains(SupplierFilter.Text, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                    (string.IsNullOrEmpty(DateFilter?.Text) || (t.DataWystawienia?.Contains(DateFilter.Text) ?? false)) &&
                    (string.IsNullOrEmpty(QuantityFilter?.Text) || (t.ZmianaStanu?.ToString().Contains(QuantityFilter.Text) ?? false)) &&
                    (string.IsNullOrEmpty(NoteFilter?.Text) || (t.Notatka?.Contains(NoteFilter.Text, StringComparison.OrdinalIgnoreCase) ?? false))
                ).ToList();

                foreach (var transaction in filtered)
                {
                    FilteredTransactions.Add(transaction);
                }

                RenderTransactionList(filtered);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Błąd przy filtrowaniu transakcji", ex);
            }
        }

        private void RenderTransactionList(List<dynamic> transactions)
        {
            try
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
                        Text = item.Produkt ?? "Brak danych",
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center,
                        FontSize = 14
                    }, 0, 0);

                    grid.Add(new Label
                    {
                        Text = item.Przeznaczenie ?? "Brak danych",
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center,
                        FontSize = 14
                    }, 1, 0);

                    grid.Add(new Label
                    {
                        Text = item.TypDokumentu ?? "Brak danych",
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center,
                        FontSize = 14
                    }, 2, 0);

                    grid.Add(new Label
                    {
                        Text = item.Dostawca ?? "Brak danych",
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center,
                        FontSize = 14
                    }, 3, 0);

                    grid.Add(new Label
                    {
                        Text = item.DataWystawienia ?? "Brak daty",
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
                        Text = item.Notatka ?? "Brak notatki",
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center,
                        FontSize = 14
                    }, 6, 0);

                    TransactionRowsStack.Children.Add(grid);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Błąd przy renderowaniu listy transakcji", ex);
            }
        }
    }
}
