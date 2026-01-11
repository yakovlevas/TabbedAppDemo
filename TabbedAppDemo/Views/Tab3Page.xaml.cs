using Microsoft.Maui.Controls;
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
    }
}