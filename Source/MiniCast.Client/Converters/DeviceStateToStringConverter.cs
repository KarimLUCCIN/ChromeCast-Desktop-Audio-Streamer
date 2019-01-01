using ChromeCast.Library.Communication;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MiniCast.Client.Converters
{
    public class DeviceStateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DeviceState)
            {
                return ((DeviceState)value).ToString();
            }
            else
            {
                return "Unknown";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is string)
            {
                DeviceState result;
                if(Enum.TryParse((string)value, out result))
                {
                    return result;
                }
                else
                {
                    return DeviceState.NotConnected;
                }
            }
            else
            {
                return DeviceState.NotConnected;
            }
        }
    }
}
