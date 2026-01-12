using Microsoft.Maui.Controls;
using TabbedAppDemo.ViewModels;

namespace TabbedAppDemo.Views
{
    public partial class Tab4Page : ContentPage
    {
        public Tab4Page(Tab4ViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Вызываем метод OnAppearing во ViewModel
            if (BindingContext is Tab4ViewModel viewModel)
            {
                viewModel.OnAppearing();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Вызываем метод OnDisappearing во ViewModel
            if (BindingContext is Tab4ViewModel viewModel)
            {
                viewModel.OnDisappearing();
            }
        }
    }
}