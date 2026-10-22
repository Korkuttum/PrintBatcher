using System.Globalization;
using System.Windows.Data;
using PrintBatcher.Models;

namespace PrintBatcher.Converters;

/// <summary>Renders enum values used in the Settings screen with Turkish labels.</summary>
public class EnumDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            ColorMode.Auto => "Otomatik",
            ColorMode.Color => "Renkli",
            ColorMode.BlackAndWhite => "Siyah-Beyaz",

            PageOrientation.Auto => "Otomatik",
            PageOrientation.Portrait => "Dikey",
            PageOrientation.Landscape => "Yatay",

            DuplexMode.Simplex => "Tek Taraflı",
            DuplexMode.LongEdge => "Çift Taraflı (Uzun Kenar)",
            DuplexMode.ShortEdge => "Çift Taraflı (Kısa Kenar)",

            ScaleMode.ActualSize => "Gerçek Boyut",
            ScaleMode.FitToPage => "Sayfaya Sığdır",
            ScaleMode.Custom => "Özel Oran",

            PrintStatus.Pending => "Bekliyor",
            PrintStatus.Printing => "Gönderiliyor",
            PrintStatus.Done => "Yazdırıldı",
            PrintStatus.Failed => "Hata",
            PrintStatus.Skipped => "Atlandı",

            _ => value?.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
