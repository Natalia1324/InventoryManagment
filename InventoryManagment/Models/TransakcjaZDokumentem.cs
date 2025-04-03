using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagment.Models
{
    public class TransakcjaZDokumentem
    {
        public int Id { get; set; }
        public int DokumentId { get; set; }
        public int ProduktId { get; set; }
        public string Dostawca { get; set; }
        public int Zmiana_Stanu { get; set; }
        public string Notatka { get; set; }
        public DateTime Data_Wystawienia { get; set; }
        public string Przeznaczenie { get; set; }
    }


}
