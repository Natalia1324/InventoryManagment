using InventoryManagment.Data;
using InventoryManagment.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InventoryManagment.Views
{
    public partial class ProductManagementPage : ContentPage
    {
        private readonly LocalDbService _dbService;
        private List<ProductWithStock> _productWithStock = new();

        public ProductManagementPage()
        {
            InitializeComponent();
            _dbService = App.Services.GetRequiredService<LocalDbService>();
            //LoadProductsAndStock();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadProductsAndStock();
        }

        //private async void LoadProductsAndStock()
        //{
        //    var produkty = await _dbService.GetProdukty();
        //    var transakcje = await _dbService.GetTransakcje();
        //    var dokumenty = await _dbService.GetDokumenty();

        //    _productWithStock = produkty.Select(p => new ProductWithStock
        //    {
        //        Produkt = p,
        //        Stock = transakcje.Where(t => t.ProduktId == p.Id)
        //                          .Sum(t => GetStockChange(t, dokumenty))
        //    }).ToList();

        //    RenderProductList(_productWithStock);
        //}

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

        private async void LoadProductsAndStock()
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

            RenderProductList(_productWithStock);
        }


        private void RenderProductList(List<ProductWithStock> products)
        {
            ProductRowsStack.Children.Clear();

            for (int i = 0; i < products.Count; i++)
            {
                var item = products[i];

                var grid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
                    Padding = new Thickness(5, 2),
                    BackgroundColor = i % 2 == 0 ? Colors.White : Color.FromArgb("#f0f0f0"),
                    RowSpacing = 0,
                    ColumnSpacing = 1
                };

                // Nazwa produktu
                var nameLabel = new Label
                {
                    Text = item.Produkt.ToString(),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontSize = 14
                };
                grid.Add(nameLabel, 0, 0);

                // Stan produktu
                var stockLabel = new Label
                {
                    Text = item.Stock.ToString() + " szt.",
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 20, 0),
                    FontSize = 14
                };
                grid.Add(stockLabel, 1, 0);

                // Przycisk Edytuj
                var editButton = new ImageButton
                {
                    Source = "edit.png",
                    BackgroundColor = Colors.SteelBlue,
                    CornerRadius = 5,
                    Padding = new Thickness(10, 5),
                    Margin = new Thickness(0, 0, 30, 0),
                    HorizontalOptions = LayoutOptions.Center,
                    WidthRequest = 50,
                    HeightRequest = 40
                };

                editButton.Clicked += async (s, e) =>
                {
                    await Navigation.PushAsync(new ProductEditPage(item.Produkt));
                };

                grid.Add(editButton, 2, 0);

                ProductRowsStack.Children.Add(grid);
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = e.NewTextValue?.ToLower() ?? "";
            var filtered = _productWithStock.Where(p => p.Produkt.Rozmiar.ToLower().Contains(searchText)).ToList();
            RenderProductList(filtered);
        }

        private async void OnAddProductClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ProductEditPage(null));
        }
    }

    public class ProductWithStock
    {
        public Produkty Produkt { get; set; }
        public int Stock { get; set; }
    }
}