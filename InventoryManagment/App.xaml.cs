using System.Diagnostics;

namespace InventoryManagment
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; set; }

        public App(MainPage mainPage)
        {
            try { 
            InitializeComponent();
            Application.Current.UserAppTheme = AppTheme.Light;
            this.RequestedThemeChanged += (s, e) => { Application.Current.UserAppTheme = AppTheme.Light; };
            //MainPage = new NavigationPage(mainPage);

            MainPage = new AppShell
            {
                Padding = new Thickness(0, 2000, 0, 0) // 20 jednostek od góry
            }; 
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppShell Error] {ex.Message}");
                throw; // Przepuść dalej, aby debugger złapał
            }
        }
    }
}
