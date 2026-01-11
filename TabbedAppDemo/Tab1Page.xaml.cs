using Microsoft.Maui.Controls;

namespace TabbedAppDemo
{
    public partial class Tab1Page : ContentPage
    {
        int count = 0;

        public Tab1Page()
        {
            InitializeComponent();
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterLabel.Text = $"Clicked {count} time";
            else
                CounterLabel.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterLabel.Text);
        }

        // НОВЫЙ МЕТОД ДЛЯ КНОПКИ "ПРИВЕТ МИР!"
        private async void OnHelloWorldButtonClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Сообщение", "Привет Мир!", "OK");
        }
    }
}