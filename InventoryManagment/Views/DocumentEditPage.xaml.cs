using InventoryManagment.Data;
using InventoryManagment.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;


namespace InventoryManagment.Views;

public partial class DocumentEditPage : ContentPage
{
    private readonly LocalDbService _dbService;
    private Dokumenty _document;
    private List<Transakcje> _transactions = new();
    private bool transactions_loaded = false;
    private List<Produkty> _products;
    private List<Transakcje> _deletedTransactions = new(); // Nowe pole

    public DocumentEditPage(Dokumenty document)
    {
        InitializeComponent();
        _dbService = App.Services.GetRequiredService<LocalDbService>();

        _document = document ?? throw new ArgumentNullException(nameof(document));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Debug.WriteLine("I appeared!!!");
        await LoadTransactions();
        await LoadDocumentData();
        Debug.WriteLine("I appeared!!!");
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
                if (transakcje.Id == 0) // nowa transakcja
                    await _dbService.CreateTransakcja(transakcje);
                else
                    await _dbService.UpdateTransakcja(transakcje);
            }

            foreach (var deleted in _deletedTransactions)
            {
                await _dbService.DeleteTransakcja(deleted);
            }

            _deletedTransactions.Clear();

            await DisplayAlert("Sukces", "Dokument i transakcje zostały zapisane.", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Błąd", $"Wystąpił problem podczas zapisywania: {ex.Message}", "OK");
        }
    }

    private async void AddTransaction(object sender, EventArgs e)
    {
        Debug.WriteLine("Dodajemy transakcje!");
        try
        {
            var productSelectionPage = new ProductSelectionPage();

            productSelectionPage.ProductSelected += async (s, selectedProduct) =>
            {
                Debug.WriteLine("Wybrano produkt" + selectedProduct.ToStringFull());
                await OpenTransactionModal(selectedProduct);
            };

            Debug.WriteLine("dodano! yay");
            await Navigation.PushAsync(productSelectionPage);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Błąd", $"Wystąpił problem podczas wyboru produktu: {ex.Message}", "OK");
        }
    }

    private async Task OpenTransactionModal(Produkty produkt, Transakcje existingTransaction = null)
    {
        Debug.WriteLine($"OpenTransactionModal: {produkt?.ToString()}");
        try
        {
            var transaction = existingTransaction ?? new Transakcje
            {
                ProduktId = produkt?.Id ?? 0,
                DokumentId = _document.Id // Powiąż z aktualnym dokumentem
            };

            // Automatyczne ustawienie dostawcy na podstawie przeznaczenia
            if (_document.Typ_Dokumentu is TypDokumentu.Przychod_Wewnetrzny or TypDokumentu.Przychod_Zewnetrzny)
            {
                transaction.Dostawca = PrzeznaczenieEntry.Text;
            }

            var modalPage = new TransactionModelPage(transaction);
            await Navigation.PushModalAsync(modalPage);
            Debug.WriteLine("Modal page opened");
            modalPage.Disappearing += async (s, e) =>
            {
                Debug.WriteLine("Modal page closed");
                if (modalPage.IsSaved)
                {
                    if (existingTransaction == null)
                    {
                        //await _dbService.CreateTransakcja(transaction); // dodaj do bazy
                        _transactions.Add(transaction); // dodaj lokalnie
                    }
                    await Navigation.PopAsync();
                    Debug.WriteLine("popped");
                    //RenderTransactionList(); // odśwież listę
                    //Debug.WriteLine("list rendered");
                }
            };
            Debug.WriteLine("modal disapeared");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Błąd", $"Nie udało się otworzyć modalu transakcji: {ex.Message}", "OK");
        }
    }

    private async Task LoadDocumentData()
    {
        Debug.WriteLine("LoadDocumentData");
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
        Debug.WriteLine("LoadTransactions");

        if (!_transactions.Any()) { 

        _transactions = await _dbService.GetTransakcje();
        _products = await _dbService.GetProdukty();

        _transactions = _transactions.Where(t => t.DokumentId == _document.Id).ToList();

        }
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
            new ColumnDefinition { Width = GridLength.Auto }
        },
            Padding = new Thickness(5, 10),
            BackgroundColor = Colors.LightGray
        };

        foreach (var transaction in _transactions)
        {
            var product = _products.FirstOrDefault(p => p.Id == transaction.ProduktId);

            var activeProducts = _products.Where(p => !p.isDel).ToList();
            var productDescriptions = activeProducts.Select(p => p.ToStringWithDesc()).ToList();

            // Dodaj placeholder, jeśli obecny produkt jest usunięty
            string deletedPlaceholder = null;
            if (product != null && product.isDel)
            {
                deletedPlaceholder = product.ToStringFull() + " (USUNIĘTO)";
                productDescriptions.Insert(0, deletedPlaceholder);
            }


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
                ItemsSource = productDescriptions,
                SelectedItem = product != null && product.isDel ? deletedPlaceholder : product?.ToStringWithDesc(),
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center
            };

            productPicker.SelectedIndexChanged += (s, e) =>
            {
                var selectedText = productPicker.SelectedItem?.ToString();
                var selectedProduct = activeProducts.FirstOrDefault(p => p.ToStringWithDesc() == selectedText);
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
            _deletedTransactions.Add(transaction); // dodaj do usunięcia później
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

