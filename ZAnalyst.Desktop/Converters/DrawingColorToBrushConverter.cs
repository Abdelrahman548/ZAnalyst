using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using AvaloniaColor = Avalonia.Media.Color;

namespace ZAnalyst.Desktop.Converters;

public sealed class DrawingColorToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not System.Drawing.Color color)
            return Brushes.Transparent;

        return new SolidColorBrush(AvaloniaColor.FromArgb(color.A, color.R, color.G, color.B));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
