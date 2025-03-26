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

        private async Task OpenTransactionModal(Produkty produkt, Transakcje existingTransaction = null)
        {
            try
            {
                var transaction = existingTransaction ?? new Transakcje { ProduktId = produkt?.Id ?? 0 };

                if (_selectedTypDokumentu is TypDokumentu.Przychod_Zewnetrzny or TypDokumentu.Przychod_Wewnetrzny)
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
                            _transactions.Add(transaction);

                        await RefreshTransactionList();
                    }
                };
            }
            catch (Exception ex)
            {
                await DisplayAlert("Błąd", $"Nie udało się otworzyć modalu: {ex.Message}", "OK");
            }
        }



        private async Task RefreshTransactionList()
        {
            try
            {
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
            catch (Exception ex)
            {
                await DisplayAlert("Błąd", $"Wystąpił problem podczas odświeżania listy transakcji: {ex.Message}", "OK");
                ErrorLogger.LogError($"DocPage: Wystąpił problem podczas odświeżania listy transakcji", ex);
            }
        }


        private void OnTypeSelected(object sender, EventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                ErrorLogger.LogError("DocPage: Problem z wyborem typu dokumentu", ex);
            }

        }

        private async void GoToProductList(object sender, EventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                ErrorLogger.LogError($"DocPage: Problem z przejsciem na liste produktów", ex);
                await DisplayAlert("Błąd", $"Wystąpił problem podczas przekierowania na strone wyboru produktów: {ex.Message}", "OK");
            }
        }

        private static int GetStockChange(Transakcje t, List<Dokumenty>? dokumenty)
        {
            try
            {
                var dokument = dokumenty?.FirstOrDefault(d => d?.Id == t?.DokumentId);
                if (dokument == null) return 0;

                return dokument.Typ_Dokumentu switch
                {
                    TypDokumentu.Przychod_Zewnetrzny => t.Zmiana_Stanu,
                    TypDokumentu.Przychod_Wewnetrzny => t.Zmiana_Stanu,
                    TypDokumentu.Rozchod_Zewnetrzny => -t.Zmiana_Stanu,
                    _ => 0
                };
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Problem z pobieraniem stanów magazynowych", ex);
            }
            return 0;
        }
        private async void AddDocument(object sender, EventArgs e)
        {
            try
            {
                if (_selectedTypDokumentu == TypDokumentu.Rozchod_Zewnetrzny)
                {
                    var produkty = await _dbService.GetProdukty();
                    var transakcje = await _dbService.GetTransakcje();
                    var dokumenty = await _dbService.GetDokumenty();

                    var productStock = produkty.ToDictionary(p => p.Id, p =>
                        transakcje?.Where(t => t.ProduktId == p.Id).Sum(t => GetStockChange(t, dokumenty))
                    );

                    List<string> outOfStockProducts = new List<string>();

                    foreach (var transakcja in _transactions)
                    {
                        if (productStock.TryGetValue(transakcja.ProduktId, out int? stock) && transakcja.Zmiana_Stanu > stock)
                        {
                            var produkt = produkty.FirstOrDefault(p => p.Id == transakcja.ProduktId);
                            outOfStockProducts.Add(produkt?.ToString() ?? "Nieznany produkt");
                        }
                    }

                    if (outOfStockProducts.Count > 0)
                    {
                        bool proceed = await DisplayAlert("Uwaga!",
                            $"Dodanie następujących produktów spowoduje ich ujemny stan magazynowy:\n\n{string.Join("\n", outOfStockProducts)}\n\nCzy chcesz kontynuować?",
                            "Tak", "Nie");

                        if (!proceed)
                        {
                            return; // Anulowanie dodawania dokumentu
                        }
                    }
                }

                var currentMonth = DataWystawieniaPicker.Date.Month;
                var currentYear = DataWystawieniaPicker.Date.Year;
                var existingDocuments = await _dbService.GetDocumentsForMonth(currentMonth, currentYear);
                var documentNumber = $"{existingDocuments?.Count + 1:D2}/{currentMonth:D2}/{currentYear}";

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
                await RefreshTransactionList();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Problem z dodaniem dokumentu", ex);
            }
            finally
            {
                await DisplayAlert("Problem!", "Dokument nie został dodany :c", "OK");
            }

        }
        

    }
}
