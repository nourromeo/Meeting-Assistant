using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace XlentWPF.Converters;

/// <summary>true → Visible, false → Collapsed</summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => value is Visibility.Visible;
}

/// <summary>true → Collapsed, false → Visible</summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => value is Visibility.Collapsed;
}

/// <summary>null → Collapsed, non-null → Visible</summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is null ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => throw new NotImplementedException();
}

/// <summary>null → Visible, non-null → Collapsed  (inverse)</summary>
public class NullToInvisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is null ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => throw new NotImplementedException();
}

/// <summary>Maps speaker index 1-4 to a foreground Brush.</summary>
public class SpeakerIndexToBrushConverter : IValueConverter
{
    private static readonly Brush[] _brushes =
    [
        new SolidColorBrush(Color.FromRgb(0x4F, 0x8E, 0xFF)),
        new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xA0)),
        new SolidColorBrush(Color.FromRgb(0xFF, 0xC9, 0x47)),
        new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B)),
    ];
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        int idx = value is int i ? (i - 1) : 0;
        return _brushes[idx % _brushes.Length];
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => throw new NotImplementedException();
}

/// <summary>Maps speaker index 1-4 to a semi-transparent background Brush.</summary>
public class SpeakerIndexToBackgroundConverter : IValueConverter
{
    private static readonly Brush[] _brushes =
    [
        new SolidColorBrush(Color.FromArgb(0x26, 0x4F, 0x8E, 0xFF)),
        new SolidColorBrush(Color.FromArgb(0x26, 0x00, 0xE5, 0xA0)),
        new SolidColorBrush(Color.FromArgb(0x26, 0xFF, 0xC9, 0x47)),
        new SolidColorBrush(Color.FromArgb(0x26, 0xFF, 0x6B, 0x6B)),
    ];
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        int idx = value is int i ? (i - 1) : 0;
        return _brushes[idx % _brushes.Length];
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => throw new NotImplementedException();
}
