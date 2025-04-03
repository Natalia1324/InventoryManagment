using InventoryManagment.Data;
using InventoryManagment.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

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
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                await LoadProductsAndStock(true);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Product Managment: problem z załadowaniem strony", ex);
                await DisplayAlert("Błąd", "Nie udało się załadować strony.", "OK");
            }
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

        private int currentOffset = 0;
        private bool isLoading = false;
        private bool hasMoreData = true;

        private async Task LoadProductsAndStock(bool reset = false)
        {
            if (isLoading || !hasMoreData) {
                return; 
            }
            isLoading = true;

            try
            {
                if (reset)
                {
                    currentOffset = 0;
                    hasMoreData = true;
                    _productWithStock.Clear();
                }
                var produkty = (await _dbService.GetProduktyPaginated(currentOffset, 100))?.Where(p => !p.isDel).ToList() ?? new List<Produkty>();
                if (produkty == null || produkty.Count == 0)
                {
                    hasMoreData = false;
                }
                else
                {
                    var transakcje = await _dbService.GetTransakcje();
                    var dokumenty = await _dbService.GetDokumenty();

                    var newProducts = produkty.Where(p => !p.isDel)
                                              .OrderBy(p => p.ToString())
                                              .Select(p => new ProductWithStock
                                              {
                                                  Produkt = p,
                                                  Stock = transakcje.Where(t => t.ProduktId == p.Id)
                                                                    .Sum(t => GetStockChange(t, dokumenty))
                                              }).ToList();

                    _productWithStock.AddRange(newProducts);
                    currentOffset += produkty.Count;
                }

                RenderProductList(_productWithStock);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Product Management: Błąd pobierania danych", ex);
                await DisplayAlert("Błąd", "Nie udało się załadować produktów.", "OK");
            }
            finally
            {
                isLoading = false;
            }
        }

        private async void OnScrolled(object sender, ScrolledEventArgs e)
        {
            var scrollView = (ScrollView)sender;
            if (scrollView.ScrollY >= scrollView.ContentSize.Height - scrollView.Height - 20)
            {
                await LoadProductsAndStock();
            }
        }
        
        private void RenderProductList(List<ProductWithStock> products)
        {
            try
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
                new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
                        Padding = new Thickness(5, 2),
                        BackgroundColor = i % 2 == 0 ? Colors.White : Color.FromArgb("#f0f0f0"),
                        RowSpacing = 0,
                        ColumnSpacing = 1
                    };
                    VisualStateManager.SetVisualStateGroups(grid, new VisualStateGroupList
        {
            new VisualStateGroup
            {
                States =
                {
                    new VisualState
                    {
                        Name = "Normal",
                        Setters =
                        {
                            new Setter
                            {
                                Property = Grid.BackgroundColorProperty,
                                Value = i % 2 == 0 ? Colors.White : Color.FromArgb("#f0f0f0")
                            }
                        }
                    },
                    new VisualState
                    {
                        Name = "PointerOver",
                        Setters =
                        {
                            new Setter
                            {
                                Property = Grid.BackgroundColorProperty,
                                Value = Color.FromArgb("#e0e0ff") // Highlight color on hover
                            }

                        }
                    }
                }
            }
        });
                    // Dodanie obsługi kliknięcia
                    var tapGesture = new TapGestureRecognizer();
                    tapGesture.Tapped += async (s, e) =>
                    {
                        await Navigation.PushAsync(new TransactionForProductPage(item.Produkt.Id));

                    };
                    grid.GestureRecognizers.Add(tapGesture);

                    // Nazwa produktu
                    var nameLabel = new Label
                    {
                        Text = item.Produkt.ToString(),
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center,
                        FontSize = 14
                    };
                    grid.Add(nameLabel, 0, 0);
                    // Kolumna Przeznaczenie + Opis

                    // Przeznaczenie (większy tekst)
                    var destinationLabel = new Label
                    {
                        Text = item.Produkt.Przeznaczenie,
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center,
                        FontSize = 14
                    };

   


                    // Dodajemy StackLayout do Grida w kolumnie 1
                    grid.Add(destinationLabel, 1, 0);
                    // Opis (mniejszy, szary tekst)
                    var descriptionLabel = new Label
                    {
                        Text = item.Produkt.Opis,
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center,
                        FontSize = 14
                    };
                    grid.Add(descriptionLabel, 2, 0);

                    // Stan produktu
                    var stockLabel = new Label
                    {
                        Text = item.Stock.ToString() + " szt.",
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 0, 20, 0),
                        FontSize = 14
                    };
                    grid.Add(stockLabel, 3, 0);

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

                    grid.Add(editButton, 4, 0);

                    ProductRowsStack.Children.Add(grid);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Product Managment: Problem z renderowaniem strony", ex);
            }
        }


        private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var searchText = e.NewTextValue?.ToLower() ?? "";
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    await LoadProductsAndStock(reset: true);
                    return;
                }

                var produkty = await _dbService.SearchProdukty(searchText, 100);
                var transakcje = await _dbService.GetTransakcje();
                var dokumenty = await _dbService.GetDokumenty();

                var filtered = produkty.Select(p => new ProductWithStock
                {
                    Produkt = p,
                    Stock = transakcje.Where(t => t.ProduktId == p.Id)
                                      .Sum(t => GetStockChange(t, dokumenty))
                }).ToList();

                RenderProductList(filtered);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas filtrowania produktów: {ex.Message}");
                await DisplayAlert("Błąd", "Wystąpił problem podczas wyszukiwania.", "OK");
            }
        }


        private async void OnAddProductClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ProductEditPage(null));
        }
    }

    public class ProductWithStock
    {
        public Produkty? Produkt { get; set; }
        public int Stock { get; set; }
    }
}