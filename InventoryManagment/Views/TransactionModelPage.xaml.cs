using Microsoft.Maui.Controls;
using InventoryManagment.Models;
using System;
using InventoryManagment.Data;

namespace InventoryManagment.Views
{
    public partial class TransactionModelPage : ContentPage
    {
        private readonly Transakcje _transaction;
        private readonly LocalDbService _dbService;

        public bool IsSaved { get; private set; } = false;

        public TransactionModelPage(Transakcje transaction)
        {
            InitializeComponent();
            _transaction = transaction ?? new Transakcje();
            _dbService = App.Services.GetRequiredService<LocalDbService>();


        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var produkt = await _dbService.GetProduktById(_transaction.ProduktId);

            NazwaProduktu.Text = produkt.ToString();
            // Wypełnij UI danymi transakcji
            DostawcaEntry.Text = _transaction.Dostawca;
            IloscEntry.Text = _transaction.Zmiana_Stanu != 0 ? _transaction.Zmiana_Stanu.ToString() : string.Empty;
            NotatkaEditor.Text = _transaction.Notatka;
        }


        private async void OnSaveClicked(object sender, EventArgs e)
        {
            _transaction.Dostawca = DostawcaEntry.Text;
            _transaction.Zmiana_Stanu = int.TryParse(IloscEntry.Text, out var ilosc) ? ilosc : 0;
            _transaction.Notatka = NotatkaEditor.Text;

            IsSaved = true; // Flaga zapisania
            await Navigation.PopModalAsync();
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            IsSaved = false; // Anulowano
            await Navigation.PopModalAsync();
        }
    }
}
