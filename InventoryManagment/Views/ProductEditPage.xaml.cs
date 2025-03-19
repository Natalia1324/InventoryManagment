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

        public ProductEditPage(Produkty product)
        {
            InitializeComponent();
            _dbService = App.Services.GetRequiredService<LocalDbService>();
            _product = product ?? new Produkty();
            PopulateFields();
        }

        private void PopulateFields()
        {
            RozmiarEntry.Text = _product.Rozmiar;
            GruboscEntry.Text = _product.Grubosc?.ToString();
            KolorEntry.Text = _product.Kolor;
            IloscEntry.Text = _product.Ilosc_Paczka?.ToString();
            PrzeznaczenieEntry.Text = _product.Przeznaczenie;
            OpisEntry.Text = _product.Opis;

            DeleteButton.IsVisible = _product.Id != 0;
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            _product.Rozmiar = RozmiarEntry.Text;
            _product.Grubosc = double.TryParse(GruboscEntry.Text, out var g) ? g : null;
            _product.Kolor = KolorEntry.Text;
            _product.Ilosc_Paczka = int.TryParse(IloscEntry.Text, out var ilosc) ? ilosc : null;
            _product.Przeznaczenie = PrzeznaczenieEntry.Text;
            _product.Opis = OpisEntry.Text;

            if (_product.Id == 0)
            {
                await _dbService.CreateProdukt(_product);
                await DisplayAlert("Dodano", "Produkt został dodany.", "OK");
            }
            else
            {
                await _dbService.UpdateProdukt(_product);
                await DisplayAlert("Zapisano", "Produkt został zaktualizowany.", "OK");
            }

            await Navigation.PopAsync();
        }

        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Usuń", "Czy na pewno chcesz usunąć produkt?", "Tak", "Nie");
            if (confirm)
            {
                await _dbService.DeleteProdukt(_product.Id);
                await Navigation.PopAsync();
            }
        }
    }
}
