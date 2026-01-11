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
    }
}