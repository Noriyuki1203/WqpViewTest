using System;
using System.Globalization;
using System.Windows.Data;

namespace WqpViewTest
{
    public class IntToBoolConverter : IValueConverter
    {
        // 0/1/DBNull/Nullに寛容
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return b;               // 既にboolならそのまま
            if (value == null || value == DBNull.Value) return false;

            try
            {
                var i = System.Convert.ToInt32(value);
                return i != 0;
            }
            catch
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return b ? 1 : 0;
            return 0;
        }
    }
}
