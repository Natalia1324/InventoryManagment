using InventoryManagment.Data;
using InventoryManagment.Models;
using Microsoft.Maui.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
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
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _dbService = App.Services.GetRequiredService<LocalDbService>();
            MauiProgram.OnKeyDown += HandleKeyDown;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                LoadDocumentDetails();
                this.Focus();
                await LoadTransactions();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading document details: {ex.Message}");
                await DisplayAlert("Błąd", "Nie udało się załadować danych dokumentu.", "OK");
            }
        }

        private void HandleKeyDown(FrameworkElement sender, KeyRoutedEventArgs e)
        {
            Debug.WriteLine($"Key Pressed: {e.Key}");

            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                Navigation.PopAsync();
            }
        }

        private string TypDokumentuToString(string typ)
        {
            return typ switch
            {
                "Rozchod_Zewnetrzny" => "Rozchód Zewnętrzny",
                "Przychod_Wewnetrzny" => "Przychód Wewnętrzny",
                "Przychod_Zewnetrzny" => "Przychód Zewnętrzny",
                _ => typ
            };
        }

        private void LoadDocumentDetails()
        {
            if (_document == null)
            {
                Debug.WriteLine("Document is null");
                return;
            }

            NrDokumentuLabel.Text = $"Nr dokumentu: {_document.Nr_Dokumentu}";
            DataWystawieniaLabel.Text = $"Data wystawienia: {_document.Data_Wystawienia:dd/MM/yyyy}";
            TypDokumentuLabel.Text = $"Typ dokumentu: {TypDokumentuToString(_document.Typ_Dokumentu.ToString() ?? "")}";
            PrzeznaczenieLabel.Text = $"Nadawca/Odbiorca: {(string.IsNullOrWhiteSpace(_document.Przeznaczenie) ? "-" : _document.Przeznaczenie)}";
        }

        private async Task LoadTransactions()
        {
            try
            {
                var allTransactions = await _dbService.GetTransakcje() ?? new List<Transakcje>();
                var allProducts = await _dbService.GetProdukty() ?? new List<Produkty>();

                var relatedTransactions = allTransactions
                    .Where(t => t?.DokumentId == _document?.Id)
                    .ToList();

                TransactionRowsStack.Children.Clear();

                for (int i = 0; i < relatedTransactions.Count; i++)
                {
                    var transaction = relatedTransactions[i];
                    var product = allProducts.FirstOrDefault(p => p?.Id == transaction?.ProduktId);

                    if (transaction == null) continue;

                    var grid = new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitionCollection
                        {
                            new ColumnDefinition { Width = new Microsoft.Maui.GridLength(2, Microsoft.Maui.GridUnitType.Star) },
                            new ColumnDefinition { Width = Microsoft.Maui.GridLength.Star },
                            new ColumnDefinition { Width = Microsoft.Maui.GridLength.Star },
                            new ColumnDefinition { Width = Microsoft.Maui.GridLength.Star }
                        },
                        Padding = new Microsoft.Maui.Thickness(5, 10),
                        Margin = new Microsoft.Maui.Thickness(0, 0),
                        BackgroundColor = i % 2 == 0 ? Colors.White : Color.FromArgb("#f0f0f0")
                    };

                    grid.Add(new Label
                    {
                        Text = product != null ? product.ToString() : "Brak produktu",
                        HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Center,
                        VerticalTextAlignment = Microsoft.Maui.TextAlignment.Center,
                        FontSize = 14
                    }, 0, 0);

                    grid.Add(new Label
                    {
                        Text = transaction.Zmiana_Stanu.ToString() ?? "N/A",
                        HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Center,
                        VerticalTextAlignment = Microsoft.Maui.TextAlignment.Center,
                        FontSize = 14
                    }, 1, 0);

                    grid.Add(new Label
                    {
                        Text = transaction.Dostawca ?? "N/A",
                        HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Center,
                        VerticalTextAlignment = Microsoft.Maui.TextAlignment.Center,
                        FontSize = 14
                    }, 2, 0);

                    grid.Add(new Label
                    {
                        Text = transaction.Notatka ?? "N/A",
                        HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Center,
                        VerticalTextAlignment = Microsoft.Maui.TextAlignment.Center,
                        FontSize = 14
                    }, 3, 0);

                    TransactionRowsStack.Children.Add(grid);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading transactions: {ex.Message}");
                await DisplayAlert("Błąd", "Nie udało się załadować transakcji.", "OK");
            }
        }
    }
}
