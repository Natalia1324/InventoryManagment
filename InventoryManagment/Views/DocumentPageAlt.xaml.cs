using InventoryManagment.Data;
using InventoryManagment.Models;
using InventoryManagment;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;

namespace InventoryManagment.Views
{
    public partial class DocumentPageAlt : ContentPage
    {
        private readonly LocalDbService _dbService;
        private TypDokumentu _selectedTypDokumentu;
        private List<Transakcje> _transactions = new List<Transakcje>();
        private bool _isInitialized = false;

        public DocumentPageAlt()
        {
            InitializeComponent();
            _dbService = App.Services.GetRequiredService<LocalDbService>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (!_isInitialized)
            {
                _isInitialized = true;
                await InitializeAsync();
            }
        }

        private Task InitializeAsync()
        {
            // Możesz umieścić tutaj asynchroniczną logikę inicjalizacyjną, jeśli jest potrzebna
            return Task.CompletedTask;
        }

        private async Task OpenTransactionModal(Produkty produkt, Transakcje existingTransaction = null)
        {
            var transaction = existingTransaction ?? new Transakcje
            {
                ProduktId = produkt?.Id ?? 0
            };

            // Uzupełniamy Dostawcę, jeśli typ dokumentu to przychód
            if (_selectedTypDokumentu == TypDokumentu.Przychod_Zewnetrzny ||
                _selectedTypDokumentu == TypDokumentu.Przychod_Wewnetrzny)
            {
                transaction.Dostawca = PrzeznaczenieEntry.Text;
            }
            await Navigation.PopToRootAsync();

            var modalPage = new TransactionModelPage(transaction);
            await Navigation.PushModalAsync(modalPage);

            modalPage.Disappearing += async (s, e) =>
            {
                if (modalPage.IsSaved)
                {
                    if (existingTransaction == null)
                    {
                        _transactions.Add(transaction);
                    }
                    await RefreshTransactionList();
                }
            };
        }



        private async Task RefreshTransactionList()
        {
            Console.WriteLine($"Refreshing Transactions: {_transactions.Count}");
            TransactionsGrid.Children.Clear();
            TransactionsGrid.RowDefinitions.Clear();

            if (_transactions.Count > 0)
            {
                // Dodaj nagłówki tylko jeśli lista transakcji nie jest pusta
                TransactionsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var headers = new[] { "Produkt", "Dostawca", "Ilość", "Notatka", "Usuń" };

                for (int col = 0; col < headers.Length; col++)
                {
                    var headerLabel = new Label
                    {
                        Text = headers[col],
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center
                    };
                    Grid.SetRow(headerLabel, 0);
                    Grid.SetColumn(headerLabel, col);
                    TransactionsGrid.Children.Add(headerLabel);
                }
            }

            int row = 1;

            foreach (var transaction in _transactions)
            {
                TransactionsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var product = await _dbService.GetProduktById(transaction.ProduktId);
                var productName = product?.ToString() ?? "Produkt nieznany";

                // Utwórz Label'e z centrowaniem
                Label CreateCenteredLabel(string text) => new Label
                {
                    Text = text,
                    FontSize = 14,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                };

                var productLabel = CreateCenteredLabel(productName);
                var dostawcaLabel = CreateCenteredLabel(transaction.Dostawca);
                var iloscLabel = CreateCenteredLabel($"{transaction.Zmiana_Stanu} szt.");
                var notatkaLabel = CreateCenteredLabel(transaction.Notatka);

                // Klikalność – otwórz modal do edycji
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => await OpenTransactionModal(null, transaction);
                productLabel.GestureRecognizers.Add(tapGesture);
                dostawcaLabel.GestureRecognizers.Add(tapGesture);
                iloscLabel.GestureRecognizers.Add(tapGesture);
                notatkaLabel.GestureRecognizers.Add(tapGesture);

                // Dodaj do grid'a, ustaw pozycje
                Grid.SetRow(productLabel, row);
                Grid.SetColumn(productLabel, 0);
                TransactionsGrid.Children.Add(productLabel);

                Grid.SetRow(dostawcaLabel, row);
                Grid.SetColumn(dostawcaLabel, 1);
                TransactionsGrid.Children.Add(dostawcaLabel);

                Grid.SetRow(iloscLabel, row);
                Grid.SetColumn(iloscLabel, 2);
                TransactionsGrid.Children.Add(iloscLabel);

                Grid.SetRow(notatkaLabel, row);
                Grid.SetColumn(notatkaLabel, 3);
                TransactionsGrid.Children.Add(notatkaLabel);

                // Przycisk 'X' – centrowany
                var deleteButton = new Button
                {
                    Text = "X",
                    BackgroundColor = Colors.LightCoral,
                    TextColor = Colors.White,
                    WidthRequest = 40,
                    HeightRequest = 40,
                    CornerRadius = 15,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };

                int indexToRemove = row - 1;
                deleteButton.Clicked += (s, e) =>
                {
                    _transactions.RemoveAt(indexToRemove);
                    _ = RefreshTransactionList();
                };

                Grid.SetRow(deleteButton, row);
                Grid.SetColumn(deleteButton, 4);
                TransactionsGrid.Children.Add(deleteButton);

                row++;
            }
        }


        private void OnTypeSelected(object sender, EventArgs e)
        {
            RozchodButton.BackgroundColor = Colors.DarkGray;
            PrzychodZewButton.BackgroundColor = Colors.DarkGray;
            PrzychodWewButton.BackgroundColor = Colors.DarkGray;

            var button = (Button)sender;
            button.BackgroundColor = Colors.DarkBlue;

            _selectedTypDokumentu = button == RozchodButton ? TypDokumentu.Rozchod_Zewnetrzny :
                                    button == PrzychodZewButton ? TypDokumentu.Przychod_Zewnetrzny :
                                    TypDokumentu.Przychod_Wewnetrzny;


            if (_selectedTypDokumentu == TypDokumentu.Przychod_Wewnetrzny)
            {
                PrzezFrame.IsVisible = true;
                PrzezImage.IsVisible = true;
                PrzeznaczenieEntry.IsVisible = true;
                PrzeznaczenieEntry.Text = "Helplast";
                PrzeznaczenieEntry.Placeholder = "Dostawca";

            }
            if (_selectedTypDokumentu == TypDokumentu.Rozchod_Zewnetrzny)
            {
                PrzezFrame.IsVisible = true;
                PrzezImage.IsVisible = true;
                PrzeznaczenieEntry.IsVisible = true;
                PrzeznaczenieEntry.Text = "";
                PrzeznaczenieEntry.Placeholder = "Odbiorca";
                

            }
            if (_selectedTypDokumentu == TypDokumentu.Przychod_Zewnetrzny)
            {
                PrzeznaczenieEntry.Placeholder = "Dostawca";
                PrzeznaczenieEntry.Text = "";
            }

        }
        //public async void HandleSelectedProduct(Produkty selectedProduct)
        //{
        //    await OpenTransactionModal(selectedProduct);
        //}


        private async void GoToProductList(object sender, EventArgs e)
        {
            var productSelectionPage = new ProductSelectionPage();

            // Subskrybujemy zdarzenie `ProductSelected`
            productSelectionPage.ProductSelected += async (s, selectedProduct) =>
            {
                Debug.WriteLine("Czekam na wybór!");
                await OpenTransactionModal(selectedProduct);
                Debug.WriteLine("Wybranooo");
            };

            // Przejście do strony wyboru produktu
            await Navigation.PushAsync(productSelectionPage);
        }


        private async void AddDocument(object sender, EventArgs e)
        {
            var currentMonth = DataWystawieniaPicker.Date.Month;
            var currentYear = DataWystawieniaPicker.Date.Year;

            // Pobranie liczby dokumentów w bieżącym miesiącu i roku
            var existingDocuments = await _dbService.GetDocumentsForMonth(currentMonth, currentYear);
            var documentCount = existingDocuments.Count;

            // Wygenerowanie numeru dokumentu
            var documentNumber = $"{documentCount + 1:D2}/{currentMonth:D2}/{currentYear}";

            var dokument = new Dokumenty
            {
                Nr_Dokumentu = documentNumber,
                Typ_Dokumentu = _selectedTypDokumentu,
                Przeznaczenie = PrzeznaczenieEntry.Text,
                Data_Wystawienia = DataWystawieniaPicker.Date
            };

            await _dbService.CreateDokument(dokument);

            foreach (var transakcja in _transactions)
            {
                transakcja.DokumentId = dokument.Id;
                await _dbService.CreateTransakcja(transakcja);
            }

            await DisplayAlert("Sukces", "Dokument został dodany!", "OK");
            _transactions.Clear();
            await RefreshTransactionList(); // Dodano

        }
    }
}
