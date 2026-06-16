using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace BananaGit.ValueConverters;

/// <summary>
/// Sets a UI elements visibility based on whether a bool is true or false
/// </summary>
public class BoolVisibilityConverter : MarkupExtension, IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return System.Convert.ToBoolean(value) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }

    public override object? ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}