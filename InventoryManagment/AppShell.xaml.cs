using System.Diagnostics;
using InventoryManagment.Views;
namespace InventoryManagment
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            try
            {
                InitializeComponent();
                //Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
                //Routing.RegisterRoute(nameof(DocumentPageAlt), typeof(DocumentPageAlt));
                //Routing.RegisterRoute(nameof(DocumentListPage), typeof(DocumentListPage));
                //Routing.RegisterRoute(nameof(ProductManagementPage), typeof(ProductManagementPage));
                //Routing.RegisterRoute(nameof(TransactionsPage), typeof(TransactionsPage));

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppShell Error] {ex.Message}");
                throw; // Przepuść dalej, aby debugger złapał
            }
        }
    }
}
