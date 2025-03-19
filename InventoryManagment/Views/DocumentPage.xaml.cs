using InventoryManagment.Data;
using InventoryManagment.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace InventoryManagment.Views
{
    public partial class DocumentPage : ContentPage
    {
        private readonly LocalDbService _dbService;
        private List<Produkty> _produktyList;
        private TypDokumentu _selectedTypDokumentu;
        private int _transactionIndex = 1; // Śledzenie numeracji transakcji
        private List<string> _productSuggestions = new List<string>();
        private List<string> _filteredProductSuggestions = new List<string>();


        public DocumentPage(LocalDbService dbService)
        {
            InitializeComponent();
            _dbService = dbService;
            AddTransactionForm(null, null); // Dodanie pierwszej transakcji
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_produktyList == null)
            {
                await LoadProducts();
            }
        }

        private async Task LoadProducts()
        {
            _produktyList = await _dbService.GetProdukty();
        }

        private void OnTypeSelected(object sender, EventArgs e)
        {
            // Resetowanie podświetlenia przycisków
            RozchodButton.BackgroundColor = Colors.LightGray;
            PrzychodZewButton.BackgroundColor = Colors.LightGray;
            PrzychodWewButton.BackgroundColor = Colors.LightGray;

            // Ustawienie wybranego typu dokumentu
            var button = (Button)sender;
            button.BackgroundColor = Colors.LightBlue;

            if (button == PrzychodWewButton)
            {
                PrzeznaczenieEntry.IsVisible = true;
                PrzeznaczenieEntry.Text = "Helplast"; // Wartość automatyczna
                PrzeznaczenieEntry.Placeholder = "Nadawca";
            }
            else if (button == RozchodButton)
            {
                PrzeznaczenieEntry.IsVisible = true;
            }
            else if (button == PrzychodZewButton)
            {
                PrzeznaczenieEntry.IsVisible = false;
            }
            else
            {
                PrzeznaczenieEntry.Text = string.Empty; // Czyszczenie pola dla innych opcji
            }

            if (button == RozchodButton)
                _selectedTypDokumentu = TypDokumentu.Rozchod_Zewnetrzny;
            else if (button == PrzychodZewButton)
                _selectedTypDokumentu = TypDokumentu.Przychod_Zewnetrzny;
            else if (button == PrzychodWewButton)
                _selectedTypDokumentu = TypDokumentu.Przychod_Wewnetrzny;
        }

        private async void AddTransactionForm(object sender, EventArgs e)
        {
            await LoadProducts();

            // Tworzenie wiersza dla transakcji
            var transactionRow = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
        {
            new ColumnDefinition { Width = GridLength.Star }, // Produkt
            new ColumnDefinition { Width = GridLength.Star }, // Dostawca
            new ColumnDefinition { Width = GridLength.Star }, // Zmiana Stanu
            new ColumnDefinition { Width = GridLength.Star }  // Notatka
        },
                RowSpacing = 10,
                Margin = new Thickness(0, 10)
            };

            // Pole wyszukiwania produktu
            var productSearchEntry = new Entry
            {
                Placeholder = "Wybierz produkt",
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            // Lista sugestii produktów (na początku ukryta)
            var productSuggestionsList = new ListView
            {
                IsVisible = false,
                HeightRequest = 150,
                ItemsSource = _produktyList.Select(p => p.ToString()).ToList()
            };

            // Obsługa wpisywania tekstu w polu wyszukiwania
            productSearchEntry.TextChanged += (s, args) =>
            {
                var searchText = args.NewTextValue?.ToLower() ?? string.Empty;

                // Filtracja produktów
                var filteredProducts = _produktyList
                    .Select(p => p.ToString())
                    .Where(p => p.ToLower().Contains(searchText))
                    .ToList();

                productSuggestionsList.ItemsSource = filteredProducts;
                productSuggestionsList.IsVisible = filteredProducts.Any();
            };

            // Obsługa wyboru produktu z listy
            productSuggestionsList.ItemTapped += (s, args) =>
            {
                if (args.Item is string selectedProduct)
                {
                    productSearchEntry.Text = selectedProduct;
                    productSuggestionsList.IsVisible = false;
                }
            };

            // Pole dostawcy
            var dostawcaEntry = new Entry
            {
                Placeholder = "Dostawca",
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            // Pole zmiana stanu
            var zmianaStanuEntry = new Entry
            {
                Placeholder = "Ilość",
                Keyboard = Keyboard.Numeric,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            // Pole notatka
            var notatkaEntry = new Entry
            {
                Placeholder = "Notatka",
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            // Ustawianie elementów w odpowiednich kolumnach
            transactionRow.Children.Add(productSearchEntry);
            Grid.SetColumn(productSearchEntry, 0);

            transactionRow.Children.Add(dostawcaEntry);
            Grid.SetColumn(dostawcaEntry, 1);

            transactionRow.Children.Add(zmianaStanuEntry);
            Grid.SetColumn(zmianaStanuEntry, 2);

            transactionRow.Children.Add(notatkaEntry);
            Grid.SetColumn(notatkaEntry, 3);

            // Stos dla całego rzędu + lista rozwijana
            var transactionStack = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                Spacing = 5
            };

            transactionStack.Children.Add(transactionRow);         // Wiersz pól
            transactionStack.Children.Add(productSuggestionsList); // Lista rozwijana

            // Dodanie do głównego stosu transakcji
            TransactionsStack.Children.Add(transactionStack);
        }

        private async void AddDocument(object sender, EventArgs e)
        {
            try
            {
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;

                // Sprawdź liczbę dokumentów wystawionych w bieżącym miesiącu
                var documentsThisMonth = await _dbService.GetDocumentsForMonth(currentMonth, currentYear);
                int documentNumber = documentsThisMonth.Count + 1; // Licznik dokumentów w bieżącym miesiącu

                // Generowanie numeru dokumentu w formacie {nr}/{miesiac}/{rok}
                var generatedNumber = $"{documentNumber}/{currentMonth:D2}/{currentYear}";

                // Tworzenie dokumentu
                var dokument = new Dokumenty
                {
                    Nr_Dokumentu = generatedNumber,
                    Typ_Dokumentu = _selectedTypDokumentu,
                    Przeznaczenie = PrzeznaczenieEntry.Text,
                    Data_Wystawienia = DataWystawieniaPicker.Date,
                    Opis = string.Empty
                };

                await _dbService.CreateDokument(dokument);

                foreach (var transactionStack in TransactionsStack.Children.OfType<StackLayout>())
                {
                    // Znajdź wiersz Grid w StackLayout
                    var transactionRow = transactionStack.Children.OfType<Grid>().FirstOrDefault();
                    if (transactionRow != null)
                    {
                        // Obsługa pól
                        var productEntry = (Entry)transactionRow.Children[0];
                        var dostawcaEntry = (Entry)transactionRow.Children[1];
                        var zmianaStanuEntry = (Entry)transactionRow.Children[2];
                        var notatkaEntry = (Entry)transactionRow.Children[3];

                        // Sprawdzenie, czy produkt jest wybrany
                        if (!string.IsNullOrEmpty(productEntry.Text))
                        {
                            // Znalezienie identyfikatora produktu na podstawie tekstu
                            var produkt = _produktyList.FirstOrDefault(p => p.ToString() == productEntry.Text);
                            if (produkt != null)
                            {
                                var transakcja = new Transakcje
                                {
                                    DokumentId = dokument.Id,
                                    ProduktId = produkt.Id,
                                    Dostawca = dostawcaEntry.Text,
                                    Zmiana_Stanu = int.Parse(zmianaStanuEntry.Text),
                                    Notatka = notatkaEntry.Text
                                };

                                await _dbService.CreateTransakcja(transakcja);
                            }
                        }
                    }
                }

                // Czyszczenie formularza
                RozchodButton.BackgroundColor = PrzychodZewButton.BackgroundColor = PrzychodWewButton.BackgroundColor = Colors.LightGray;
                PrzeznaczenieEntry.Text = string.Empty;
                TransactionsStack.Children.Clear();
                _transactionIndex = 1; // Reset numeracji transakcji
                AddTransactionForm(null, null); // Dodanie pierwszej transakcji

                await DisplayAlert("Wiadomość", "Nowy dokument dodany poprawnie.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Błąd", ex.Message, "OK");
            }
        }

    }
}

