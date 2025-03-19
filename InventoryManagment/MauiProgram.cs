using Microsoft.Extensions.Logging;
using InventoryManagment.Data;
using Microsoft.EntityFrameworkCore;
using InventoryManagment.Models;
using InventoryManagment.Views;

namespace InventoryManagment
{
    public static class MauiProgram
    {

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<LocalDbService>();
            //builder.Services.AddTransient<DocumentPageAlt>();
            //builder.Services.AddTransient<DocumentListPage>();
            //builder.Services.AddTransient<ProductManagementPage>();
            //builder.Services.AddTransient<TransactionsPage>();
            // Rejestracja strony
            builder.Services.AddTransient<MainPage>();



#if DEBUG
            builder.Logging.AddDebug();
#endif
            var app = builder.Build();

            // Rejestracja kontenera usług w App
            App.Services = app.Services;

            return app;
        }
    }
}
