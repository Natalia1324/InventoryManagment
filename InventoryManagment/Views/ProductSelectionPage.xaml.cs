using InventoryManagment.Data;
using InventoryManagment.Models;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace InventoryManagment.Views;

public partial class ProductSelectionPage : ContentPage
{
    private readonly LocalDbService _dbService;
    private List<ProductWithStock> _productWithStock;

    public event EventHandler<Produkty> ProductSelected;

    public ProductSelectionPage()
    {
        InitializeComponent();
        _dbService = new LocalDbService();
        MauiProgram.OnKeyDown += HandleKeyDown;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        this.Focus(); // Wymuszenie fokusu na stronie
        try
        {
            if (_productWithStock == null || !_productWithStock.Any())
            {
                await LoadProductsAndStock();
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Błąd przy wyborze produktu", ex);
            await DisplayAlert("Błąd", "Nie udało się załadować listy produktów.", "OK");
        }
    }
    private void HandleKeyDown(FrameworkElement sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            Navigation.PopAsync(); // Powrót do poprzedniej strony
        }
    }

    private async Task LoadProductsAndStock()
    {
        var produkty = (await _dbService.GetProdukty())?.Where(p => !p.isDel).OrderBy(p => p.ToStringFull()).ToList();
        var transakcje = await _dbService.GetTransakcje();
        var dokumenty = await _dbService.GetDokumenty();

        if (produkty != null && transakcje != null && dokumenty != null)
        {
            _productWithStock = produkty.Select(p => new ProductWithStock
            {
                Produkt = p,
                Stock = transakcje.Where(t => t.ProduktId == p.Id)
                                  .Sum(t => GetStockChange(t, dokumenty))
            }).ToList();

            ProductListView.ItemsSource = _productWithStock;
        }
        else throw new Exception("Product Managment: Błąd pobierania bazy danych");
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

    private void OnProductTapped(object sender, ItemTappedEventArgs e)
    {
        try
        {
            if (e?.Item is ProductWithStock selectedProduct && selectedProduct.Produkt != null)
            {
                ProductSelected?.Invoke(this, selectedProduct.Produkt);
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Błąd przy wyborze produktu", ex);
        }
    }

    private ObservableCollection<ProductWithStock> _filteredProductWithStock;

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            var searchText = e.NewTextValue?.ToLower() ?? string.Empty;

            if (_productWithStock == null)
                return;

            ProductListView.ItemsSource = string.IsNullOrEmpty(searchText)
                ? _productWithStock
                : _productWithStock
                    .Where(p => p.Produkt != null && p.Produkt.ToStringFull().ToLower().Contains(searchText))
                    .ToList();
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("ProductSelection: Błąd filtrowania", ex);
        }
    }

    private void OnProductPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = new SolidColorBrush(Colors.LightBlue);
        }
    }

    private void OnProductPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = new SolidColorBrush(Colors.Transparent);
        }
    }


}
