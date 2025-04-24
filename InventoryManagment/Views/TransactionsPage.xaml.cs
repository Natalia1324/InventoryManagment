using InventoryManagment.Data;
using InventoryManagment.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Maui.Views;

namespace InventoryManagment.Views
{

    public partial class TransactionsPage : ContentPage
    {
        private readonly LocalDbService _dbService;
        public List<dynamic> AllTransactions { get; set; } = new List<dynamic>(); // Pełna lista transakcji
        public ObservableCollection<dynamic> DisplayedTransactions { get; set; } = new ObservableCollection<dynamic>(); // Wyświetlana lista

        private int currentOffset = 0;
        private bool isLoading = false;
        private bool hasMoreData = true;
        private const int PageSize = 50;
        private string _currentSortColumn = "DataWystawienia";
        private bool _isAscending = true;

        public TransactionsPage()
        {
            InitializeComponent();
            _dbService = App.Services.GetRequiredService<LocalDbService>();
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var popup = new LoadingPopup("Ładuje...");
            this.ShowPopup(popup); // pokaż popup

            try
            {
                await LoadAllTransactions(); // ładowanie danych
                LoadNextPage();              // paginacja lokalna
            }
            finally
            {
                popup.Close(); // schowaj popup, niezależnie czy był wyjątek
            }
        }


        private async Task LoadAllTransactions()
        {
            try
            {
                isLoading = true;
                AllTransactions.Clear();
                DisplayedTransactions.Clear();
                currentOffset = 0;
                hasMoreData = true;

                var transakcje = await _dbService.GetTransakcje();

                foreach (var transakcja in transakcje)
                {
                    var produkt = await _dbService.GetProduktById(transakcja.ProduktId) ?? new Produkty { Rozmiar = "Nieznany produkt" };
                    var dokument = await _dbService.GetDokumentById(transakcja.DokumentId);
                    if (dokument == null) continue;

                    string typDokumentu = dokument.Typ_Dokumentu == TypDokumentu.Rozchod_Zewnetrzny ? "Rozchód" : "Przychód";

                    var transaction = new
                    {
                        Produkt = produkt.ToStringFull(),
                        Przeznaczenie = dokument.Przeznaczenie ?? "Brak danych",
                        TypDokumentu = typDokumentu,
                        Dostawca = transakcja.Dostawca ?? "Nieznany",
                        DataWystawienia = dokument.Data_Wystawienia.ToString("yyyy-MM-dd"),
                        ZmianaStanu = transakcja.Zmiana_Stanu,
                        Notatka = transakcja.Notatka ?? "Brak notatki"
                    };

                    AllTransactions.Add(transaction);
                }

                SortTransactions();
                LoadNextPage();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Błąd przy ładowaniu transakcji", ex);
                await DisplayAlert("Błąd", "Nie udało się pobrać listy transakcji.", "OK");
            }
            finally
            {
                isLoading = false;
            }
        }

        private void LoadNextPage()
        {
            if (!hasMoreData || isLoading) return;

            var nextBatch = AllTransactions.Skip(currentOffset).Take(PageSize).ToList();
            foreach (var transaction in nextBatch)
            {
                DisplayedTransactions.Add(transaction);
            }

            currentOffset += nextBatch.Count;
            hasMoreData = nextBatch.Count == PageSize; // Jeśli mniej niż PageSize, nie ma więcej danych
            RenderTransactionList(DisplayedTransactions.ToList());
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

                    // Produkt
                    var productLabel = new Label
                    {
                        Text = item.Produkt ?? "Brak danych",
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center,
                        FontSize = 14
                    };
                    grid.Add(productLabel, 0, 0);

                    // Przeznaczenie
                    var destinationLabel = new Label
                    {
                        Text = item.Przeznaczenie ?? "Brak danych",
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center,
                        FontSize = 14
                    };
                    grid.Add(destinationLabel, 1, 0);

                    // Typ dokumentu
                    var documentTypeLabel = new Label
                    {
                        Text = item.TypDokumentu ?? "Brak danych",
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center,
                        FontSize = 14
                    };
                    grid.Add(documentTypeLabel, 2, 0);

                    // Dostawca
                    var supplierLabel = new Label
                    {
                        Text = item.Dostawca ?? "Brak danych",
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center,
                        FontSize = 14
                    };
                    grid.Add(supplierLabel, 3, 0);

                    // Data wystawienia
                    var dateLabel = new Label
                    {
                        Text = item.DataWystawienia ?? "Brak daty",
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center,
                        FontSize = 14
                    };
                    grid.Add(dateLabel, 4, 0);

                    // Zmiana stanu (ilość)
                    var quantityLabel = new Label
                    {
                        Text = item.ZmianaStanu.ToString(),
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center,
                        FontSize = 14
                    };
                    grid.Add(quantityLabel, 5, 0);

                    // Notatka
                    var noteLabel = new Label
                    {
                        Text = item.Notatka ?? "Brak notatki",
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center,
                        FontSize = 14
                    };
                    grid.Add(noteLabel, 6, 0);

                    // Dodanie siatki do głównego kontenera
                    TransactionRowsStack.Children.Add(grid);

                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Błąd przy renderowaniu listy transakcji", ex);
            }
        }

        private void OnFilterChanged(object sender, TextChangedEventArgs e)
        {
            var filtered = AllTransactions
                .Where(t =>
                    (string.IsNullOrEmpty(ProductFilter?.Text) || (t.Produkt?.Contains(ProductFilter.Text, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                    (string.IsNullOrEmpty(DestinationFilter?.Text) || (t.Przeznaczenie?.Contains(DestinationFilter.Text, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                    (string.IsNullOrEmpty(DocumentTypeFilter?.Text) || (t.TypDokumentu?.Contains(DocumentTypeFilter.Text, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                    (string.IsNullOrEmpty(SupplierFilter?.Text) || (t.Dostawca?.Contains(SupplierFilter.Text, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                    (string.IsNullOrEmpty(DateFilter?.Text) || (t.DataWystawienia?.Contains(DateFilter.Text, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                    (string.IsNullOrEmpty(QuantityFilter?.Text) || (t.ZmianaStanu.ToString().Contains(QuantityFilter.Text, StringComparison.OrdinalIgnoreCase))) &&
                    (string.IsNullOrEmpty(NoteFilter?.Text) || (t.Notatka?.Contains(NoteFilter.Text, StringComparison.OrdinalIgnoreCase) ?? false)))
                .ToList();

            // Resetowanie paginacji i aktualizacja listy
            DisplayedTransactions.Clear();
            foreach (var transaction in filtered)
            {
                DisplayedTransactions.Add(transaction);
            }

            // Aktualizacja UI
            RenderTransactionList(DisplayedTransactions.ToList());
        }

        private void OnScrolled(object sender, ScrolledEventArgs e)
        {
            var scrollView = (ScrollView)sender;
            if (scrollView.ScrollY >= scrollView.ContentSize.Height - scrollView.Height - 20)
            {
                LoadNextPage();
            }
        }

        private void OnSortClicked(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                string clickedColumn = button.CommandParameter.ToString();

                // Jeśli kliknięto ten sam nagłówek co wcześniej, zmieniamy kolejność sortowania
                if (_currentSortColumn == clickedColumn)
                {
                    _isAscending = !_isAscending;
                }
                else
                {
                    _currentSortColumn = clickedColumn;
                    _isAscending = true; // Domyślnie sortujemy rosnąco przy pierwszym kliknięciu nowej kolumny
                }

                SortTransactions();
            }
        }


        private void SortTransactions()
        {
            // Sortowanie listy transakcji na podstawie wybranej kolumny i kierunku sortowania
            AllTransactions = (_isAscending
                ? AllTransactions.OrderBy(t => t.GetType().GetProperty(_currentSortColumn)?.GetValue(t))
                : AllTransactions.OrderByDescending(t => t.GetType().GetProperty(_currentSortColumn)?.GetValue(t)))
                .ToList();

            // Reset paginacji
            DisplayedTransactions.Clear();
            currentOffset = 0;
            hasMoreData = true;

            // Załaduj ponownie pierwszą stronę
            LoadNextPage();
        }

    }
}


