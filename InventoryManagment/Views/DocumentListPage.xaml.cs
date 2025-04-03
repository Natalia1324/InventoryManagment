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
        private List<Dokumenty> _displayedDocuments = new();

        private int currentOffset = 0;
        private bool isLoading = false;
        private bool hasMoreData = true;
        private const int PageSize = 50;

        public DocumentListPage()
        {
            InitializeComponent();
            _dbService = App.Services.GetRequiredService<LocalDbService>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                await LoadDocuments();
                LoadNextPage(); // Załaduj pierwsze 50 rekordów
            }
            catch (Exception ex)
            {
                await DisplayAlert("Błąd", $"Nie udało się załadować dokumentów: {ex.Message}", "OK");
            }
        }

        private async Task LoadDocuments()
        {
            try
            {
                isLoading = true;
                _allDocuments.Clear();
                _displayedDocuments.Clear();
                currentOffset = 0;
                hasMoreData = true;

                var documents = await _dbService.GetDokumenty();
                _allDocuments = documents?
                    .Where(d => d?.Przeznaczenie != "Wyrównanie Stanu Magazynowego")
                    .OrderByDescending(d => d?.Data_Wystawienia)
                    .ToList() ?? new List<Dokumenty>();

                LoadNextPage();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Błąd", $"Wystąpił problem podczas pobierania dokumentów: {ex.Message}", "OK");
                _allDocuments = new List<Dokumenty>();
            }
            finally
            {
                isLoading = false;
            }
        }

        private void LoadNextPage()
        {
            if (!hasMoreData || isLoading) return;

            var nextBatch = _allDocuments.Skip(currentOffset).Take(PageSize).ToList();
            _displayedDocuments.AddRange(nextBatch);
            currentOffset += nextBatch.Count;
            hasMoreData = nextBatch.Count == PageSize; // Jeśli mniej niż PageSize, nie ma więcej danych

            RenderDocumentList(_displayedDocuments);
        }

        private void RenderDocumentList(List<Dokumenty> documents)
        {
            DocumentRowsStack.Children.Clear();

            if (documents == null || documents.Count == 0)
            {
                DocumentRowsStack.Children.Add(new Label
                {
                    Text = "Brak dokumentów do wyświetlenia.",
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontSize = 16,
                    TextColor = Colors.Gray
                });
                return;
            }

            for (int i = 0; i < documents.Count; i++)
            {
                var doc = documents[i];

                var grid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }
                    },
                    Padding = new Thickness(5, 2),
                    BackgroundColor = i % 2 == 0 ? Colors.White : Color.FromArgb("#f0f0f0"),
                    RowSpacing = 0,
                    ColumnSpacing = 1,
                    HeightRequest = 40
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
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => await ShowDocumentDetails(doc);
                grid.GestureRecognizers.Add(tapGesture);

                grid.Add(new Label
                {
                    Text = string.IsNullOrWhiteSpace(doc.Przeznaczenie) ? "-" : doc.Przeznaczenie,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontSize = 14
                }, 0, 0);

                grid.Add(new Label
                {
                    Text = doc.Data_Wystawienia.ToString("dd/MM/yyyy"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontSize = 14
                }, 1, 0);

                grid.Add(new Label
                {
                    Text = TypDokumentuToString(doc.Typ_Dokumentu.ToString()),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontSize = 14
                }, 2, 0);

                DocumentRowsStack.Children.Add(grid);
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = e.NewTextValue?.ToLower() ?? "";
            var filtered = _allDocuments
                .Where(doc =>
                    doc.Nr_Dokumentu?.ToLower().Contains(searchText) == true ||
                    doc.Data_Wystawienia.ToString("dd/MM/yyyy").Contains(searchText) ||
                    doc.Przeznaczenie?.ToLower().Contains(searchText) == true)
                .ToList();

            _displayedDocuments.Clear();
            currentOffset = 0;
            hasMoreData = true;
            _displayedDocuments.AddRange(filtered.Take(PageSize));
            currentOffset = _displayedDocuments.Count;
            hasMoreData = filtered.Count > currentOffset;

            RenderDocumentList(_displayedDocuments);
        }

        private void OnScrolled(object sender, ScrolledEventArgs e)
        {
            var scrollView = (ScrollView)sender;
            if (scrollView.ScrollY >= scrollView.ContentSize.Height - scrollView.Height - 20)
            {
                LoadNextPage();
            }
        }

        private async Task ShowDocumentDetails(Dokumenty doc)
        {
            try
            {
                if (doc == null)
                {
                    await DisplayAlert("Błąd", "Nie można otworzyć szczegółów, ponieważ dokument nie istnieje.", "OK");
                    return;
                }

                await Navigation.PushAsync(new DocumentDetailsPage(doc));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Błąd", $"Nie udało się otworzyć szczegółów dokumentu: {ex.Message}", "OK");
            }
        }

        private string TypDokumentuToString(string? typ)
        {
            return typ switch
            {
                "Rozchod_Zewnetrzny" => "Rozchód Zewnętrzny",
                "Przychod_Wewnetrzny" => "Przychód Wewnętrzny",
                "Przychod_Zewnetrzny" => "Przychód Zewnętrzny",
                _ => typ ?? "Nieznany typ"
            };
        }
    }
}
