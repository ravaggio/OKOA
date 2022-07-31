using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ctf_final.Converters
{
    class ColorByType : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.Equals("Treino") ? Application.Current.Resources["Orange"] : Application.Current.Resources["Yoga"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(Application.Current.Resources["Yoga"]) ? "Yoga" : "Treino";
        }
    }
}
