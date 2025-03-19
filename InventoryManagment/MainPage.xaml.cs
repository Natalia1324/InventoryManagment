using InventoryManagment.Data;
using Microsoft.Maui.Controls;
using InventoryManagment.Views;
namespace InventoryManagment
{
    public partial class MainPage : ContentPage
    {
        LocalDbService _localDbService;
        public MainPage(LocalDbService dbService)
        {
            InitializeComponent();
            _localDbService = dbService;
        }


        private async void GoToDocumentPage(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new DocumentPageAlt());
        }


        private async void GoToDocumentListPage(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new DocumentListPage());
        }
        private async void GoToStanMagazynu(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ProductStockPage());
        }
        private async void GoToProductListPage (object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ProductManagementPage());
        }
        private async void GoToTransactionsPage(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new TransactionsPage());
        }
    }
}
