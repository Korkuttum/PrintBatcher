using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using PrintBatcher.Models;

namespace PrintBatcher.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            PrintStatus.Done => Brushes.SeaGreen,
            PrintStatus.Printing => Brushes.DodgerBlue,
            PrintStatus.Failed => Brushes.Crimson,
            PrintStatus.Skipped => Brushes.Gray,
            _ => Brushes.Black
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
