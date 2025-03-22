using CommunityToolkit.Maui.Views;

namespace InventoryManagment.Views;

public partial class LoadingPopup : Popup
{
        public LoadingPopup()
        {
            InitializeComponent();
        }

        //public static void ShowShellPopup()
        //{
        //    var shell = App.Current.MainPage as Shell;
        //    shell?.ShowPopup(new LoadingPopup());
        //}

        //public static void HideShellPopup()
        //{
        //    var shell = App.Current.MainPage as Shell;
        //    shell?.CurrentPage?.Close();
        //}
    }