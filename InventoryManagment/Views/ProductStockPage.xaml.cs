
using InventoryManagment.Data;
using InventoryManagment.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryManagment.Views
{
    public partial class ProductStockPage : ContentPage
    {
        private readonly LocalDbService _dbService;

        public ProductStockPage()
        {
            InitializeComponent();
            _dbService = App.Services.GetRequiredService<LocalDbService>();
            LoadProductStock();
        }

        private async void LoadProductStock()
        {
            var produkty = await _dbService.GetProdukty();
            var transakcje = await _dbService.GetTransakcje();
            var dokumenty = await _dbService.GetDokumenty();

            var stockList = produkty.Select(p => new
            {
                ProductName = $"{p.Rozmiar} {p.Grubosc} {p.Kolor}".Replace("  ", " ").Trim(),
                Stock = transakcje.Where(t => t.ProduktId == p.Id)
                                 .Sum(t => GetStockChange(t, dokumenty))
            }).ToList();

            ProductStockListView.ItemsSource = stockList;
        }

        private int GetStockChange(Transakcje transakcja, List<Dokumenty> dokumenty)
        {
            var dokument = dokumenty.FirstOrDefault(d => d.Id == transakcja.DokumentId);
            if (dokument == null) return 0;

            return dokument.Typ_Dokumentu switch
            {
                TypDokumentu.Przychod_Zewnetrzny => transakcja.Zmiana_Stanu,
                TypDokumentu.Przychod_Wewnetrzny => transakcja.Zmiana_Stanu,
                TypDokumentu.Rozchod_Zewnetrzny => -transakcja.Zmiana_Stanu,
                _ => 0
            };
        }
    }
}
