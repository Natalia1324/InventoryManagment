using InventoryManagment.Data;
using InventoryManagment.Models;
using Microsoft.Maui.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryManagment.Views
{
    public partial class DocumentDetailsPage : ContentPage
    {
        private readonly Dokumenty _document;
        private readonly LocalDbService _dbService;

        public DocumentDetailsPage(Dokumenty document)
        {
            InitializeComponent();
            _document = document;
            _dbService = App.Services.GetRequiredService<LocalDbService>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            LoadDocumentDetails();
            await LoadTransactions();
        }
        private string TypDokumentuToString(string typ)
        {
            return typ switch
            {
                "Rozchod_Zewnetrzny" => "Rozchód Zewnętrzny",
                "Przychod_Wewnetrzny" => "Przychód Wewnętrzny",
                "Przychod_Zewnetrzny" => "Przychód Zewnętrzny",
                _ => typ // Domyślnie zwraca oryginalną wartość
            };
        }
        private void LoadDocumentDetails()
        {
            NrDokumentuLabel.Text = $"Nr dokumentu: {_document.Nr_Dokumentu}";
            DataWystawieniaLabel.Text = $"Data wystawienia: {_document.Data_Wystawienia:dd/MM/yyyy}";
            TypDokumentuLabel.Text = $"Typ dokumentu: {TypDokumentuToString(_document.Typ_Dokumentu.ToString())}";
            PrzeznaczenieLabel.Text = $"Nadawca/Odbiorca: {(string.IsNullOrWhiteSpace(_document.Przeznaczenie) ? "-" : _document.Przeznaczenie)}";
        }

        private async Task LoadTransactions()
        {
            var allTransactions = await _dbService.GetTransakcje();
            var allProducts = await _dbService.GetProdukty();

            var relatedTransactions = allTransactions
                .Where(t => t.DokumentId == _document.Id)
                .ToList();

            TransactionRowsStack.Children.Clear();

            for (int i = 0; i < relatedTransactions.Count; i++)
            {
                var transaction = relatedTransactions[i];
                var product = allProducts.FirstOrDefault(p => p.Id == transaction.ProduktId);

                var grid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Star }
                    },
                    Padding = new Thickness(5,10),
                    Margin = new Thickness(0,0),
                    BackgroundColor = i % 2 == 0 ? Colors.White : Color.FromArgb("#f0f0f0")
                };

                grid.Add(new Label
                {
                    Text = product != null ? product.ToString() : "Brak produktu",
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontSize = 14
                }, 0, 0);

                grid.Add(new Label
                {
                    Text = transaction.Zmiana_Stanu.ToString(),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontSize = 14
                }, 1, 0);

                grid.Add(new Label
                {
                    Text = transaction.Dostawca,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontSize = 14
                }, 2, 0);

                grid.Add(new Label
                {
                    Text = transaction.Notatka,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontSize = 14
                }, 3, 0);

                TransactionRowsStack.Children.Add(grid);
            }
        }
    }
}
