using InventoryManagment;
using InventoryManagment.Data;
using InventoryManagment.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InventoryManagment.Views
{
    public partial class DocumentListPage : ContentPage
    {
        private readonly LocalDbService _dbService;
        private List<Dokumenty> _allDocuments = new List<Dokumenty>();

        public DocumentListPage()
        {
            InitializeComponent();
            _dbService = App.Services.GetRequiredService<LocalDbService>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Pobierz pełną listę dokumentów
            _allDocuments = await _dbService.GetDokumenty();
            DocumentListView.ItemsSource = _allDocuments;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = e.NewTextValue?.ToLower() ?? string.Empty;

            // Filtrujemy dokumenty na podstawie numeru, daty lub przeznaczenia
            DocumentListView.ItemsSource = _allDocuments
                .Where(doc =>
                    doc.Nr_Dokumentu.ToLower().Contains(searchText) ||
                    doc.Data_Wystawienia.ToString("dd/MM/yyyy").Contains(searchText) ||
                    doc.Przeznaczenie.ToLower().Contains(searchText))
                .ToList();
        }

        private async void OnDocumentSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is Dokumenty selectedDocument)
            {
                // Ustaw szczegóły dokumentu
                DocNumberLabel.Text = selectedDocument.Nr_Dokumentu;
                DocDateLabel.Text = selectedDocument.Data_Wystawienia.ToString("dd/MM/yyyy");
                DocPurposeLabel.Text = selectedDocument.Przeznaczenie;
                DocTypeLabel.Text = selectedDocument.Typ_Dokumentu.ToString();

                // Pobierz transakcje i produkty
                var transactions = await _dbService.GetTransakcjeForDokument(selectedDocument.Id);
                var products = await _dbService.GetProdukty();

                // Dołącz nazwę produktu do transakcji
                var detailedTransactions = transactions.Select(t =>
                {
                    var product = products.FirstOrDefault(p => p.Id == t.ProduktId);
                    return new
                    {
                        Produkt = product?.ToString() ?? "Brak produktu",
                        t.Dostawca,
                        t.Zmiana_Stanu,
                        t.Notatka
                    };
                }).ToList();

                // Wyświetl transakcje w liście
                TransactionListView.ItemsSource = detailedTransactions;
            }
        }

    }
}
