using Microsoft.Maui.Controls;
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
    }
}