using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
namespace InventoryManagment.Models
{
    public enum TypDokumentu
    {
        Rozchod_Zewnetrzny,
        Przychod_Zewnetrzny,
        Przychod_Wewnetrzny
    }

    [Table("Dokumenty")]
    public class Dokumenty
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }
        public string Nr_Dokumentu { get; set; } = string.Empty;
        public DateTime Data_Wystawienia { get; set; } = DateTime.Now;
        public TypDokumentu Typ_Dokumentu
        {
            get => (TypDokumentu)Typ_DokumentuInt;
            set => Typ_DokumentuInt = (int)value;
        }
        [Column("Typ_Dokumentu_Int")]
        public int Typ_DokumentuInt { get; set; }
        public string Przeznaczenie { get; set; } = string.Empty;
        public string Opis {  get; set; } = string.Empty;

    }
}
