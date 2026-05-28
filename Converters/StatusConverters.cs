using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace JobTrackerWPF.Converters
{
    [ValueConversion(typeof(string), typeof(SolidColorBrush))]
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value?.ToString()) switch
            {
                "Scheduled"   => new SolidColorBrush(Color.FromRgb(56, 138, 221)),
                "Interviewed" => new SolidColorBrush(Color.FromRgb(186, 117, 23)),
                "Next Round"  => new SolidColorBrush(Color.FromRgb(99, 153, 34)),
                "Offer"       => new SolidColorBrush(Color.FromRgb(29, 158, 117)),
                "Rejected"    => new SolidColorBrush(Color.FromRgb(162, 45, 45)),
                _             => new SolidColorBrush(Color.FromRgb(136, 135, 128)),
            };
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(string), typeof(SolidColorBrush))]
    public class StatusToFgConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
