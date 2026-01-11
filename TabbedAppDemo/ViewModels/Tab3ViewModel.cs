using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace TabbedAppDemo.ViewModels
{
    public partial class Tab3ViewModel : ObservableObject
    {
        public Tab3ViewModel()
        {
            // Инициализируем коллекцию
            Items = new ObservableCollection<string>
            {
                "Item 1",
                "Item 2",
                "Item 3",
                "Item 4",
                "Item 5"
            };
        }

        [ObservableProperty]
        private string _title = "Tab 3 - List View";

        [ObservableProperty]
        private string _newItemText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _items;

        [ObservableProperty]
        private string _selectedItem;

        [RelayCommand]
        private void AddItem()
        {
            if (!string.IsNullOrWhiteSpace(NewItemText))
            {
                Items.Add(NewItemText);
                NewItemText = string.Empty;
            }
        }

        [RelayCommand]
        private void RemoveItem(string item)
        {
            if (item != null)
            {
                Items.Remove(item);
            }
        }

        [RelayCommand]
        private async Task ItemSelected(string item)
        {
            if (!string.IsNullOrEmpty(item))
            {
                await Application.Current.MainPage.DisplayAlert("Selected",
                    $"You selected: {item}",
                    "OK");
            }
        }

        partial void OnSelectedItemChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                // Можно обработать изменение выбранного элемента
            }
        }
    }
}