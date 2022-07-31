using System;
using System.Globalization;
using Xamarin.Forms;

namespace ctf_final.Converters
{
    class PhoneNumberString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value != null)
            {
                string pVal = value.ToString();
                string formattedPhone = "Telefone: ({0}) {1}-{2}";
                var final_phone = string.IsNullOrWhiteSpace(pVal) ? "" : string.Format(formattedPhone, pVal.Substring(0, 2), pVal.Substring(2, (pVal.Length - 6)), pVal.Substring((pVal.Length - 6) + 2));
                return final_phone;
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString().Replace(")", "").Replace("(", "").Replace("-", "").Replace("Telefone: ", "").Replace(" ", "");
        }
    }
}