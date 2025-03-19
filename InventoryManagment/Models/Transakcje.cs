using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagment.Models
{
    [Table("Transakcje")]
    public class Transakcje
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }
        public string Dostawca { get; set; } = string.Empty;
        public int Zmiana_Stanu { get; set; }
        public string Notatka { get; set; } = string.Empty;

        [Indexed]
        public int DokumentId { get; set; }

        // Klucz obcy do Produktów
        [Indexed]
        public int ProduktId { get; set; }

    }
}
