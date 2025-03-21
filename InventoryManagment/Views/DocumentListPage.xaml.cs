using InventoryManagment.Data;
using InventoryManagment.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryManagment.Views
{
    public partial class DocumentListPage : ContentPage
    {
        private readonly LocalDbService _dbService;
        private List<Dokumenty> _allDocuments = new();

        public DocumentListPage()
        {
            InitializeComponent();
            _dbService = App.Services.GetRequiredService<LocalDbService>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDocuments();
        }

        private async Task LoadDocuments()
        {
            _allDocuments = (await _dbService.GetDokumenty())
                .Where(d => d.Przeznaczenie != "Wyrównanie Stanu Magazynowego")
                .OrderByDescending(d => d.Data_Wystawienia)
                .ToList();

            RenderDocumentList(_allDocuments);
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
        private void RenderDocumentList(List<Dokumenty> documents)
        {
            DocumentRowsStack.Children.Clear();

            for (int i = 0; i < documents.Count; i++)
            {
                var doc = documents[i];

                var grid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }, // Zgodne z nagłówkami
                new ColumnDefinition { Width = GridLength.Star }, // Zgodne z nagłówkami
                new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) } // Zgodne z nagłówkami
            },
                    Padding = new Thickness(5, 2),
                    BackgroundColor = i % 2 == 0 ? Colors.White : Color.FromArgb("#f0f0f0"),
                    RowSpacing = 0,
                    ColumnSpacing = 1,
                    HeightRequest = 40 // Ustawienie takiej samej wysokości jak nagłówki
                };

                // Hover effect
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
                                        Value = Color.FromArgb("#e0e0ff")
                                    }
                                }
                            }
                        }
                    }
                });

                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => await ShowDocumentDetails(doc);
                grid.GestureRecognizers.Add(tapGesture);

                // Nr dokumentu
                var nrLabel = new Label
                {
                    Text = string.IsNullOrWhiteSpace(doc.Przeznaczenie) ? "-" : doc.Przeznaczenie,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontSize = 14
                };
                grid.Add(nrLabel, 0, 0);

                // Data
                var dateLabel = new Label
                {
                    Text = doc.Data_Wystawienia.ToString("dd/MM/yyyy"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontSize = 14
                };
                grid.Add(dateLabel, 1, 0);

                var typLabel = new Label
                {
                    Text = TypDokumentuToString(doc.Typ_Dokumentu.ToString()),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontSize = 14
                };
                grid.Add(typLabel, 2, 0);
                // Szczegóły button
                //var detailButton = new ImageButton
                //{
                //    Source = "details.png",
                //    BackgroundColor = Colors.SteelBlue,
                //    CornerRadius = 5,
                //    Padding = new Thickness(10, 5),
                //    Margin = new Thickness(0, 0, 30, 0),
                //    HorizontalOptions = LayoutOptions.Center,
                //    WidthRequest = 50,
                //    HeightRequest = 40
                //};

                // detailButton.Clicked += async (s, e) => await ShowDocumentDetails(doc);

                //grid.Add(detailButton, 2, 0);

                DocumentRowsStack.Children.Add(grid);
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = e.NewTextValue?.ToLower() ?? "";
            var filtered = _allDocuments
                .Where(doc =>
                    doc.Nr_Dokumentu.ToLower().Contains(searchText) ||
                    doc.Data_Wystawienia.ToString("dd/MM/yyyy").Contains(searchText) ||
                    doc.Przeznaczenie.ToLower().Contains(searchText))
                .ToList();

            RenderDocumentList(filtered);
        }

        private async Task ShowDocumentDetails(Dokumenty doc)
        {
            await Navigation.PushAsync(new DocumentDetailsPage(doc)); // Musisz mieć stronę do szczegółów
        }
    }
}
