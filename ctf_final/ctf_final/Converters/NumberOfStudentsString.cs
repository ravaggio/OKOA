using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ctf_final.Converters
{
    class NumberOfStudentsString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.Equals("1") ? "1 aluno" : value + " alunos";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals("1 aluno") ? "1" : value.ToString()[0].ToString();
        }
    }
}

