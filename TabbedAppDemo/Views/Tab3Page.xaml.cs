using TabbedAppDemo.ViewModels;

namespace TabbedAppDemo.Views
{
    public partial class Tab3Page : ContentPage
    {
        public Tab3Page(Tab3ViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // При появлении страницы обновляем данные
            if (BindingContext is Tab3ViewModel viewModel)
            {
                // Асинхронно проверяем подключение и загружаем портфель
                Task.Run(async () =>
                {
                    await viewModel.CheckConnectionAndLoadPortfolioAsync();
                });
            }
        }
    }
}