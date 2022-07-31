using System;
using System.Globalization;
using Xamarin.Forms;

namespace ctf_final.Converters
{
    class MakeupClassesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return "Reposições disponíveis (Treino): " + value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString().Replace("Reposições disponíveis (Treino): ", "");
        }
    }
}

