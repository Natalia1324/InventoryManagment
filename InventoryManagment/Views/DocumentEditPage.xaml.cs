using InventoryManagment.Data;
using InventoryManagment.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace InventoryManagment.Views;

public partial class DocumentEditPage : ContentPage
{
    private readonly LocalDbService _dbService;
    private Dokumenty _document;
    private List<Transakcje> _transactions;
    private List<Produkty> _products;

    public DocumentEditPage(Dokumenty document)
    {
        InitializeComponent();
        _dbService = App.Services.GetRequiredService<LocalDbService>();
        _document = document ?? throw new ArgumentNullException(nameof(document));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDocumentData();
        await LoadTransactions();
    }
    private async void SaveChanges(object sender, EventArgs e)
    {
        try
        {
            _document.Data_Wystawienia = DataWystawieniaDatePicker.Date;
            _document.Przeznaczenie = PrzeznaczenieEntry.Text;
            _document.Typ_Dokumentu = TypDokumentuToEnum(TypDokumentuPicker.SelectedItem.ToString());

            await _dbService.UpdateDokument(_document);
            foreach (var transakcje in _transactions)
            {
                await _dbService.UpdateTransakcja(transakcje);
            }

            await DisplayAlert("Sukces", "Dokument i transakcje zostały zapisane.", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Błąd", $"Wystąpił problem podczas zapisywania: {ex.Message}", "OK");
        }
    }
    private async Task LoadDocumentData()
    {
        // Wypełnienie pól dokumentu
        DataWystawieniaDatePicker.Date = _document.Data_Wystawienia;
        PrzeznaczenieEntry.Text = _document.Przeznaczenie;

        TypDokumentuPicker.ItemsSource = new List<string>
            {
                "Rozchód Zewnętrzny", "Przychód Wewnętrzny", "Przychód Zewnętrzny"
            };
        TypDokumentuPicker.SelectedItem = TypDokumentuToString(_document.Typ_Dokumentu);
    }

    private async Task LoadTransactions()
    {
        _transactions = await _dbService.GetTransakcje();
        _products = await _dbService.GetProdukty();

        _transactions = _transactions.Where(t => t.DokumentId == _document.Id).ToList();

        RenderTransactionList();
    }

    private void RenderTransactionList()
    {
        TransactionRowsStack.Children.Clear();

        // Dodanie nagłówków
        var headerGrid = new Grid
        {
            ColumnDefinitions =
        {
            new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
            new ColumnDefinition { Width = GridLength.Star },
            new ColumnDefinition { Width = GridLength.Star },
            new ColumnDefinition { Width = GridLength.Star },
            new ColumnDefinition { Width = GridLength.Star }
        },
            Padding = new Thickness(5, 10),
            BackgroundColor = Colors.LightGray
        };

        headerGrid.Add(new Label { Text = "Nazwa produktu", FontSize = 14, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center }, 0, 0);
        headerGrid.Add(new Label { Text = "Ilość", FontSize = 14, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center }, 1, 0);
        headerGrid.Add(new Label { Text = "Dostawca", FontSize = 14, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center }, 2, 0);
        headerGrid.Add(new Label { Text = "Notatka", FontSize = 14, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center }, 3, 0);
        headerGrid.Add(new Label { Text = "Usuń", FontSize = 14, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center }, 4, 0);

        TransactionRowsStack.Children.Add(headerGrid);

        foreach (var transaction in _transactions)
        {
            var product = _products.FirstOrDefault(p => p.Id == transaction.ProduktId);
            var grid = new Grid
            {
                ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
                Padding = new Thickness(5, 10),
                BackgroundColor = Colors.White
            };

            // Picker dla produktu
            var productPicker = new Picker
            {
                ItemsSource = _products.Select(p => p.ToStringFull()).ToList(),
                SelectedItem = product?.ToStringFull(),
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center
            };
            productPicker.SelectedIndexChanged += (s, e) =>
            {
                var selectedProduct = _products.FirstOrDefault(p => p.ToStringFull() == productPicker.SelectedItem?.ToString());
                if (selectedProduct != null)
                {
                    transaction.ProduktId = selectedProduct.Id;
                }
            };
            grid.Add(productPicker, 0, 0);

            // Entry dla ilości
            var quantityEntry = new Entry
            {
                Text = transaction.Zmiana_Stanu.ToString(),
                Keyboard = Keyboard.Numeric,
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center
            };
            quantityEntry.TextChanged += (s, e) =>
            {
                if (int.TryParse(e.NewTextValue, out int newValue))
                {
                    transaction.Zmiana_Stanu = newValue;
                }
            };
            grid.Add(quantityEntry, 1, 0);

            // Entry dla dostawcy
            var supplierEntry = new Entry
            {
                Text = transaction.Dostawca,
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center

            };
            supplierEntry.TextChanged += (s, e) => transaction.Dostawca = e.NewTextValue;
            grid.Add(supplierEntry, 2, 0);

            // Entry dla notatki
            var noteEntry = new Entry
            {
                Text = transaction.Notatka,
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center
            };
            noteEntry.TextChanged += (s, e) => transaction.Notatka = e.NewTextValue;
            grid.Add(noteEntry, 3, 0);

            // Przycisk usuwania
            var deleteButton = new Button
            {
                Text = "❌",
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.Red,
                FontSize = 14
            };
            deleteButton.Clicked += (s, e) => DeleteTransaction(transaction);
            grid.Add(deleteButton, 4, 0);

            TransactionRowsStack.Children.Add(grid);
        }
    }

    private async void DeleteTransaction(Transakcje transaction)
    {
        _transactions.Remove(transaction);
        try { 
        await _dbService.DeleteTransakcja(transaction);
        RenderTransactionList();
        }
        catch (Exception e)
        {
            await DisplayAlert("Błąd", $"Nie można usunąć transakcji: {e.Message}", "OK");
        }
    }

    private string TypDokumentuToString(TypDokumentu typ)
    {
        return typ switch
        {
            TypDokumentu.Rozchod_Zewnetrzny => "Rozchód Zewnętrzny",
            TypDokumentu.Przychod_Wewnetrzny => "Przychód Wewnętrzny",
            TypDokumentu.Przychod_Zewnetrzny => "Przychód Zewnętrzny",
            _ => "Nieznany typ"
        };
    }

    private TypDokumentu TypDokumentuToEnum(string typ)
    {
        return typ switch
        {
            "Rozchód Zewnętrzny" => TypDokumentu.Rozchod_Zewnetrzny,
            "Przychód Wewnętrzny" => TypDokumentu.Przychod_Wewnetrzny,
            "Przychód Zewnętrzny" => TypDokumentu.Przychod_Zewnetrzny,
            _ => TypDokumentu.Rozchod_Zewnetrzny
        };
    }
}

