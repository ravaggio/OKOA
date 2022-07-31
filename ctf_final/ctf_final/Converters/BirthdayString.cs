using System;
using System.Globalization;
using Xamarin.Forms;

namespace ctf_final.Converters
{
    class BirthdayString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string r = value.ToString();
            if (string.IsNullOrWhiteSpace(r))
                return "Data de nascimento inválida";

            return string.Format("Data de nascimento - {0}/{1}/{2}", r.Substring(0, 2), r.Substring(2, 2), r.Substring(4));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString().Replace("/", "").Remove(0, 21);
        }
    }
}
