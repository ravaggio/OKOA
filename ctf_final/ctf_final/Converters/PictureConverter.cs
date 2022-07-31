using System;
using System.Globalization;
using Xamarin.Forms;

using static ctf_final.AppController;

namespace ctf_final.Converters
{
    class PictureConverter : IValueConverter
    {
        string final_value;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            final_value = value.ToString();
            return value.Equals("") ? SharedUtilities.DefaultPictureToken : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(SharedUtilities.DefaultPictureToken) ? "" : final_value;
        }
    }
}