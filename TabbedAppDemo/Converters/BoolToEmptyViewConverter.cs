using System.Globalization;

namespace TabbedAppDemo.Converters
{
    /// <summary>
    /// Конвертер для отображения текста в зависимости от состояния подключения
    /// </summary>
    public class BoolToEmptyViewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isConnected)
            {
                return isConnected ? "Сделок нет" : "Подключение не установлено";
            }
            return "Сделок нет";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер для преобразования количества элементов в высоту
    /// </summary>
    public class CountToHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count && count > 0)
            {
                // Приблизительная высота: 70px на элемент + 10px отступы
                // Ограничиваем максимальную высоту 400px для предотвращения проблем с производительностью
                return Math.Min(count * 80, 400);
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // УДАЛИТЬ InverseBooleanConverter отсюда, если он уже есть в другом файле
}