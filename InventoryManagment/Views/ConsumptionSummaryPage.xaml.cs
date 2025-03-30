using InventoryManagment.Data;
using InventoryManagment.Models;
using Microsoft.Maui.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryManagment.Views;

public partial class ConsumptionSummaryPage : ContentPage
{

    private readonly LocalDbService _dbService;
    private List<Transakcje> _consumptionTransactions = new();
    private List<Dokumenty> _documents = new();
    private Produkty _product;
    private int _productId;

    public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(-1);
    public DateTime EndDate { get; set; } = DateTime.Now;

    public ConsumptionSummaryPage(int productId)
    {
        InitializeComponent();
        _dbService = App.Services.GetRequiredService<LocalDbService>();
        BindingContext = this;
        _productId = productId;
        MauiProgram.OnKeyDown += HandleKeyDown;

    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        this.Focus();

        await LoadConsumptionData();
    }
    private void HandleKeyDown(FrameworkElement sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            Navigation.PopAsync();
        }
    }
    private async Task LoadConsumptionData()
    {
        var products = await _dbService.GetProdukty();
        var transactions = await _dbService.GetTransakcje();
        _documents = await _dbService.GetDokumenty();

        if (products == null || transactions == null || _documents == null) return;

        _product = products.FirstOrDefault(p => p.Id == _productId);
        if (_product != null)
        {
            HeaderLabel.Text = $"Podsumowanie rozchodów dla {_product}";
        }

        _consumptionTransactions = transactions
            .Where(t => t.ProduktId == _productId && IsConsumptionTransaction(t))
            .ToList();

        FilterAndDisplayTransactions();
    }

    private bool IsConsumptionTransaction(Transakcje transaction)
    {
        var doc = _documents.FirstOrDefault(d => d.Id == transaction.DokumentId);
        return doc != null && doc.Typ_Dokumentu == TypDokumentu.Rozchod_Zewnetrzny;
    }

    private async Task FilterAndDisplayTransactions()
    {
        await Task.Delay(100); // Opcjonalne opóźnienie, jeśli potrzebne do aktualizacji UI

        var filteredTransactions = _consumptionTransactions
            .Where(t =>
            {
                var document = _documents.FirstOrDefault(d => d.Id == t.DokumentId);
                return document != null && document.Data_Wystawienia >= StartDate && document.Data_Wystawienia <= EndDate;
            })
            .OrderByDescending(t => _documents.FirstOrDefault(d => d.Id == t.DokumentId)?.Data_Wystawienia)
            .ToList();

        RenderTransactionList(filteredTransactions);
        UpdateTotalConsumption(filteredTransactions);
    }
    private void RenderTransactionList(List<Transakcje> transactions)
    {
        ConsumptionRowsStack.Children.Clear();

        bool isAlternate = false; // Flaga dla zmiany koloru co drugi rekord

        foreach (var transaction in transactions)
        {
            var document = _documents.FirstOrDefault(d => d.Id == transaction.DokumentId);
            string dateText = document?.Data_Wystawienia.ToString("dd/MM/yyyy") ?? "Brak daty";

            var grid = new Grid
            {
                ColumnDefinitions =
            {
                new ColumnDefinition { Width = new Microsoft.Maui.GridLength(2, Microsoft.Maui.GridUnitType.Star) },
                new ColumnDefinition { Width = Microsoft.Maui.GridLength.Star }
            },
                Padding = new Microsoft.Maui.Thickness(5, 2),
                BackgroundColor = isAlternate ? Color.FromArgb("#f0f0f0") : Colors.White // Ustawienie koloru
            };

            var dateLabel = new Label
            {
                Text = dateText,
                HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Center,
                VerticalTextAlignment = Microsoft.Maui.TextAlignment.Center
            };
            grid.Add(dateLabel, 0, 0);

            var amountLabel = new Label
            {
                Text = $"{transaction.Zmiana_Stanu} szt.",
                HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Center,
                VerticalTextAlignment = Microsoft.Maui.TextAlignment.Center
            };
            grid.Add(amountLabel, 1, 0);

            ConsumptionRowsStack.Children.Add(grid);

            // Przełącz flagę dla koloru
            isAlternate = !isAlternate;
        }
    }

    private void UpdateTotalConsumption(List<Transakcje> transactions)
    {
        int total = transactions.Sum(t => t.Zmiana_Stanu);
        TotalConsumptionLabel.Text = $"Łączna ilość: {total} szt.";
    }

    private async void OnFilterClicked(object sender, EventArgs e)
    {
        StartDate = StartDatePicker.Date;
        EndDate = EndDatePicker.Date;
        await FilterAndDisplayTransactions();
    }
}
