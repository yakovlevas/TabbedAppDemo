using Microsoft.Maui.Controls;
using TabbedAppDemo.ViewModels;

namespace TabbedAppDemo.Views
{
    public partial class Tab1Page : ContentPage
    {
        public Tab1Page(Tab1ViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel; // ViewModel внедряется через DI
        }
    }
}