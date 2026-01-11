using CommunityToolkit.Maui;
using TabbedAppDemo.Services;
using TabbedAppDemo.ViewModels;
using TabbedAppDemo.Views;

namespace TabbedAppDemo
{
    public static class MauiProgram
    {
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

            // Регистрация сервисов
            builder.Services.AddSingleton<IDialogService, DialogService>();

            // Регистрация ViewModels
            builder.Services.AddTransient<Tab1ViewModel>();
            builder.Services.AddTransient<Tab2ViewModel>();
            builder.Services.AddTransient<Tab3ViewModel>();

            // Регистрация страниц
            builder.Services.AddTransient<Tab1Page>();
            builder.Services.AddTransient<Tab2Page>();
            builder.Services.AddTransient<Tab3Page>();

            return builder.Build();
        }
    }
}