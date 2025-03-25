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
        if (_productWithStock == null || !_productWithStock.Any())
        {
            await LoadProductsAndStock();
        }
    }
    private void HandleKeyDown(FrameworkElement sender, KeyRoutedEventArgs e)
    {
        Debug.WriteLine($"Key Pressed: {e.Key}");

        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            Navigation.PopAsync(); // Powrót do poprzedniej strony
        }
    }

    //protected override async void OnAppearing()
    //{
    //    base.OnAppearing();
    //    if (_productWithStock == null || !_productWithStock.Any())
    //    {
    //        await LoadProductsAndStock();
    //    }
    //}

    private async Task LoadProductsAndStock()
    {
        var produkty = (await _dbService.GetProdukty()).Where(p => !p.isDel).ToList();
        var transakcje = await _dbService.GetTransakcje();
        var dokumenty = await _dbService.GetDokumenty();

        _productWithStock = produkty.Select(p => new ProductWithStock
        {
            Produkt = p,
            Stock = transakcje.Where(t => t.ProduktId == p.Id)
                              .Sum(t => GetStockChange(t, dokumenty))
        }).ToList();

        ProductListView.ItemsSource = _productWithStock;

    }
    private int GetStockChange(Transakcje transakcja, List<Dokumenty> dokumenty)
    {
        var dokument = dokumenty.FirstOrDefault(d => d.Id == transakcja.DokumentId);
        if (dokument == null) return 0;

        // Zakładając, że dokumenty mają typ określający, czy dodają, czy odejmują ze stanu magazynowego
        return dokument.Typ_Dokumentu == TypDokumentu.Rozchod_Zewnetrzny ? -transakcja.Zmiana_Stanu : transakcja.Zmiana_Stanu;
    }

    private void OnProductTapped(object sender, ItemTappedEventArgs e)
    {
        if (e.Item is ProductWithStock selectedProduct)
        {
            ProductSelected?.Invoke(this, selectedProduct.Produkt);
            //Navigation.PopAsync();
        }
    }

    private ObservableCollection<ProductWithStock> _filteredProductWithStock;

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue?.ToLower() ?? string.Empty;

        if (!string.IsNullOrEmpty(searchText))
        {
            ProductListView.ItemsSource = _productWithStock
                .Where(p => p.Produkt.ToString().ToLower().Contains(searchText))
                .ToList();  // Konwersja do nowej listy zapobiega zapętleniu
        }
        else
        {
            ProductListView.ItemsSource = _productWithStock;  // Przywrócenie pełnej listy
        }
    }


}
