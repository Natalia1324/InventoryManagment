using CommunityToolkit.Maui.Views;

namespace InventoryManagment.Views;

public partial class LoadingPopup : Popup
{
        public LoadingPopup(string nazwa)
        {
            InitializeComponent();
            Label.Text = nazwa;
    }
    }