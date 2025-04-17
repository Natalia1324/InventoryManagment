using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagment.Models
{
    [Table("Produkty")]
    public class Produkty
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }
        public string Rozmiar { get; set; } = string.Empty;
        public double? Grubosc { get; set; }
        public string Kolor { get; set; } = string.Empty;
        public int? Ilosc_Paczka { get; set; }
        public string Przeznaczenie { get; set; } = string.Empty;
        public string Opis { get; set; } = string.Empty;
        public bool isDel {  get; set; } = false;


        public override string ToString()
        {
            return $"{Rozmiar} {Kolor} {(Grubosc.HasValue ? Grubosc.Value.ToString("0.###") : "")} {(Ilosc_Paczka.HasValue ? "A" : "")}{Ilosc_Paczka}";

        }
        public string ToStringFull()
        {
            return $"{Rozmiar} {Kolor} {(Grubosc.HasValue ? Grubosc.Value.ToString("0.###") : "")} {(Ilosc_Paczka.HasValue ? "A" : "")}{Ilosc_Paczka} {Przeznaczenie}";

        }

        public string ToStringWithDesc()
        {
            var gruboscStr = Grubosc.HasValue ? $" {Grubosc.Value:0.###}" : "";
            var iloscStr = Ilosc_Paczka.HasValue ? $" A{Ilosc_Paczka}" : "";
            var opisStr = string.IsNullOrWhiteSpace(Opis) ? "" : $" ({Opis})";
            return $"{Rozmiar} {Kolor} {gruboscStr} {iloscStr} {Przeznaczenie} {opisStr}";
        }
    }
}
