using TabbedAppDemo.ViewModels;

namespace TabbedAppDemo.Views
{
    public partial class Tab2Page : ContentPage
    {
        public Tab2Page(Tab2ViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is Tab2ViewModel viewModel)
            {
                await viewModel.OnAppearing();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (BindingContext is Tab2ViewModel viewModel)
            {
                // Можно добавить логику при скрытии страницы
            }
        }
    }
}