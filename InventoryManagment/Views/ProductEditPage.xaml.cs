using InventoryManagment.Data;
using InventoryManagment.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;

namespace InventoryManagment.Views
{
    public partial class ProductEditPage : ContentPage
    {
        private readonly LocalDbService _dbService;
        private Produkty _product;
        private int _initialStock;
        private int _oldStock;

        public ProductEditPage(Produkty product)
        {
            InitializeComponent();
            _dbService = App.Services.GetRequiredService<LocalDbService>();
            _product = product ?? new Produkty();
            PopulateFields();
            MauiProgram.OnKeyDown += HandleKeyDown;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.Focus();
        }

        private void HandleKeyDown(FrameworkElement sender, KeyRoutedEventArgs e)
        {
            Debug.WriteLine($"Key Pressed: {e.Key}");

            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                Navigation.PopAsync();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            MauiProgram.OnKeyDown -= HandleKeyDown;
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
        private async void PopulateFields()
        {
            try
            {
                RozmiarEntry.Text = _product.Rozmiar;
                GruboscEntry.Text = _product.Grubosc?.ToString();
                KolorEntry.Text = _product.Kolor;
                IloscEntry.Text = _product.Ilosc_Paczka?.ToString();
                PrzeznaczenieEntry.Text = _product.Przeznaczenie;
                OpisEntry.Text = _product.Opis;

                DeleteButton.IsVisible = _product.Id != 0;

                var produkty = await _dbService.GetProdukty() ?? new List<Produkty>();
                var transakcje = await _dbService.GetTransakcje() ?? new List<Transakcje>();
                var dokumenty = await _dbService.GetDokumenty() ?? new List<Dokumenty>();

                var productWithStock = produkty.Select(p => new ProductWithStock
                {
                    Produkt = p,
                    Stock = transakcje.Where(t => t.ProduktId == p.Id)
                                      .Sum(t => GetStockChange(t, dokumenty))
                }).FirstOrDefault(p => p.Produkt.Id == _product.Id);

                _initialStock = productWithStock?.Stock ?? 0;
                StockEntry.Text = _initialStock.ToString();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"ProductEdit: Błąd podczas wczytywania danych", ex);
                await DisplayAlert("Błąd", "Nie udało się wczytać danych produktu.", "OK");
            }
        }
        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                var newProduct = new Produkty
                {
                    Rozmiar = RozmiarEntry.Text,
                    Grubosc = double.TryParse(GruboscEntry.Text, out var g) ? g : null,
                    Kolor = KolorEntry.Text,
                    Ilosc_Paczka = int.TryParse(IloscEntry.Text, out var ilosc) ? ilosc : null,
                    Przeznaczenie = PrzeznaczenieEntry.Text,
                    Opis = OpisEntry.Text,
                    isDel = false
                };

                var isOnlyStockChanged = _product.Rozmiar == newProduct.Rozmiar &&
                                         _product.Grubosc == newProduct.Grubosc &&
                                         _product.Kolor == newProduct.Kolor &&
                                         _product.Ilosc_Paczka == newProduct.Ilosc_Paczka &&
                                         _product.Przeznaczenie == newProduct.Przeznaczenie &&
                                         _product.Opis == newProduct.Opis;

                var newStock = int.TryParse(StockEntry.Text.Replace(" ", ""), out var stock) ? stock : _initialStock;
                var stockDifference = newStock - _initialStock;

                if (_product.Id != 0 && isOnlyStockChanged && stockDifference != 0)
                {
                    await UpdateStock(_product, newStock);
                    await DisplayAlert("Zmieniono", "Zaktualizowano jedynie stan magazynowy.", "OK");
                }
                else
                {
                    if (_product.Id == 0)
                    {
                        // Tworzymy nowy produkt
                        await _dbService.CreateProdukt(newProduct);
                        await TransferStock(null, newProduct);
                    }
                    else
                    {
                        // Najpierw tworzymy nowy produkt
                        await _dbService.CreateProdukt(newProduct);

                        await TransferStock(_product, newProduct);
                        // Na końcu oznaczamy stary produkt jako usunięty
                        _product.isDel = true;
                        await _dbService.UpdateProdukt(_product);

                        await DisplayAlert("Zmieniono", "Produkt został zaktualizowany.", "OK");
                    }
                }
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Błąd przy edycji produktu", ex);
                await DisplayAlert("Błąd", "Wystąpił problem podczas zapisywania zmian.", "OK");
            }
        }


        private async Task TransferStock(Produkty oldProduct, Produkty newProduct)
        {
            try
            {
                // Jeśli oldProduct == null, oznacza to nowy produkt, więc przenosimy tyle, ile wprowadzono
                var stockDifference = int.Parse(StockEntry.Text);
                if (stockDifference == 0) return;

                var dokument = new Dokumenty
                {
                    Nr_Dokumentu = "Auto-generated",
                    Typ_Dokumentu = stockDifference > 0 ? TypDokumentu.Przychod_Wewnetrzny : TypDokumentu.Rozchod_Zewnetrzny,
                    Przeznaczenie = "Wyrównanie stanu magazynowego",
                    Data_Wystawienia = DateTime.Now
                };

                await _dbService.CreateDokument(dokument);

                // Przenosimy tylko do nowego produktu
                var transakcjaIn = new Transakcje
                {
                    ProduktId = newProduct.Id, // Nowy produkt dostaje stan magazynowy
                    DokumentId = dokument.Id,
                    Zmiana_Stanu = stockDifference
                };
                await _dbService.CreateTransakcja(transakcjaIn);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Błąd przy przenoszeniu stanu magazynowego", ex);
                await DisplayAlert("Błąd", "Nie udało się przenieść stanu magazynowego.", "OK");
            }
        }

        private async Task UpdateStock(Produkty product, int newStock)
        {
            try
            {
                var stockDifference = newStock - _initialStock;
                if (stockDifference == 0) return;

                var dokument = new Dokumenty
                {
                    Nr_Dokumentu = "Auto-generated",
                    Typ_Dokumentu = stockDifference > 0 ? TypDokumentu.Przychod_Wewnetrzny : TypDokumentu.Rozchod_Zewnetrzny,
                    Przeznaczenie = "Wyrównanie Stanu Magazynowego",
                    Data_Wystawienia = DateTime.Now
                };

                await _dbService.CreateDokument(dokument);

                var transakcja = new Transakcje
                {
                    ProduktId = product.Id,
                    DokumentId = dokument.Id,
                    Zmiana_Stanu = Math.Abs(stockDifference)
                };
                await _dbService.CreateTransakcja(transakcja);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Błąd przy aktualizacji stanu magazynowego", ex);
                await DisplayAlert("Błąd", "Nie udało się zaktualizować stanu magazynowego.", "OK");
            }
        }

        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            try
            {
                var confirm = await DisplayAlert("Usuń", "Czy na pewno chcesz usunąć produkt?", "Tak", "Nie");
                if (confirm)
                {
                    _product.isDel = true;
                    await _dbService.UpdateProdukt(_product);
                    await Navigation.PopAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Błąd przy usuwaniu produktu", ex);
                await DisplayAlert("Błąd", "Nie udało się usunąć produktu.", "OK");
            }
        }
    }
}