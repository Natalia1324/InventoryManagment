using Microsoft.Extensions.Logging;
using InventoryManagment.Data;
using InventoryManagment.Models;
using InventoryManagment.Views;
using Microsoft.Maui.Handlers;
using System.Diagnostics;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using Windows.Foundation;
using CommunityToolkit.Maui;

namespace InventoryManagment
{
    public static class MauiProgram
    {
        public static event TypedEventHandler<FrameworkElement, KeyRoutedEventArgs> OnKeyDown;
        public static event TypedEventHandler<FrameworkElement, KeyRoutedEventArgs> OnKeyUp;
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.ConfigureLifecycleEvents(events =>
            {
                events.AddWindows(window =>
                {
                    window.OnWindowCreated(win =>
                    {
                        if (win.Content is FrameworkElement frameworkElement)
                        {
                            frameworkElement.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((sender, e) =>
                            {
                                OnKeyDown?.Invoke(frameworkElement, e);
                            }), true);

                            frameworkElement.AddHandler(UIElement.KeyUpEvent, new KeyEventHandler((sender, e) =>
                            {
                                OnKeyUp?.Invoke(frameworkElement, e);
                            }), true);
                        }
                    });
                });
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
