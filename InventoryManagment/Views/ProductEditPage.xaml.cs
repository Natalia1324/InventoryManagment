using InventoryManagment.Data;
using InventoryManagment.Models;
using Microsoft.Maui.Controls;
using System;

namespace InventoryManagment.Views
{
    public partial class ProductEditPage : ContentPage
    {
        private readonly LocalDbService _dbService;
        private Produkty _product;
        private int _initialStock;

        public ProductEditPage(Produkty product)
        {
            InitializeComponent();
            _dbService = App.Services.GetRequiredService<LocalDbService>();
            _product = product ?? new Produkty();
            PopulateFields();
        }
        private int GetStockChange(Transakcje t, List<Dokumenty> dokumenty)
        {
            var dokument = dokumenty.FirstOrDefault(d => d.Id == t.DokumentId);
            if (dokument == null) return 0;

            return dokument.Typ_Dokumentu switch
            {
                TypDokumentu.Przychod_Zewnetrzny => t.Zmiana_Stanu,
                TypDokumentu.Przychod_Wewnetrzny => t.Zmiana_Stanu,
                TypDokumentu.Rozchod_Zewnetrzny => -t.Zmiana_Stanu,
                _ => 0
            };
        }
        private async void PopulateFields()
        {
            RozmiarEntry.Text = _product.Rozmiar;
            GruboscEntry.Text = _product.Grubosc?.ToString();
            KolorEntry.Text = _product.Kolor;
            IloscEntry.Text = _product.Ilosc_Paczka?.ToString();
            PrzeznaczenieEntry.Text = _product.Przeznaczenie;
            OpisEntry.Text = _product.Opis;
            
            DeleteButton.IsVisible = _product.Id != 0;

            var produkty = await _dbService.GetProdukty();
            var transakcje = await _dbService.GetTransakcje();
            var dokumenty = await _dbService.GetDokumenty();

            var productWithStock = produkty.Select(p => new ProductWithStock
            {
                Produkt = p,
                Stock = transakcje.Where(t => t.ProduktId == p.Id)
                                  .Sum(t => GetStockChange(t, dokumenty))
            }).FirstOrDefault(p => p.Produkt.Id == _product.Id);

            _initialStock = productWithStock?.Stock ?? 0;

            StockEntry.Text = _initialStock.ToString();
        }

        private async void OnSaveClicked(object sender, EventArgs e)
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

            if (_product.Id == 0)
            {
                await _dbService.CreateProdukt(newProduct);
                await DisplayAlert("Dodano", "Produkt został dodany.", "OK");
            }
            else
            {
                await UpdateStock();
                _product.Rozmiar = newProduct.Rozmiar;
                _product.Grubosc = newProduct.Grubosc;
                _product.Kolor = newProduct.Kolor;
                _product.Ilosc_Paczka = newProduct.Ilosc_Paczka;
                _product.Przeznaczenie = newProduct.Przeznaczenie;
                _product.Opis = newProduct.Opis;
                await _dbService.UpdateProdukt(_product);
                await DisplayAlert("Zapisano", "Produkt został zaktualizowany.", "OK");
            }

            await Navigation.PopAsync();
        }

        private async Task UpdateStock()
        {
            var newStock = int.TryParse(StockEntry.Text.Replace(" ", ""), out var stock) ? stock : _initialStock;

            var stockDifference = newStock - _initialStock;

            if (stockDifference != 0)
            {
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                var existingDocuments = await _dbService.GetDocumentsForMonth(currentMonth, currentYear);
                var documentCount = existingDocuments.Count;
                var documentNumber = $"{documentCount + 1:D2}/{currentMonth:D2}/{currentYear}";

                var dokument = new Dokumenty
                {
                    Nr_Dokumentu = documentNumber,
                    Typ_Dokumentu = stockDifference > 0 ? TypDokumentu.Przychod_Wewnetrzny : TypDokumentu.Rozchod_Zewnetrzny,
                    Przeznaczenie = "Wyrównanie Stanu Magazynowego",
                    Data_Wystawienia = DateTime.Now
                };

                await _dbService.CreateDokument(dokument);

                var transakcja = new Transakcje
                {
                    ProduktId = _product.Id,
                    DokumentId = dokument.Id,
                    Zmiana_Stanu = stockDifference
                };

                await _dbService.CreateTransakcja(transakcja);
            }
        }

        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Usuń", "Czy na pewno chcesz usunąć produkt?", "Tak", "Nie");
            if (confirm)
            {
                _product.isDel = true;
                await _dbService.UpdateProdukt(_product);
                await Navigation.PopAsync();
            }
        }
    }
}
