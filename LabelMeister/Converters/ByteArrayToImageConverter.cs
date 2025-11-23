using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace LabelMeister.Converters;

public class ByteArrayToImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is byte[] imageData && imageData.Length > 0)
        {
            try
            {
                using var validationStream = new MemoryStream(imageData);
                validationStream.Position = 0;
                var decoder = new PngBitmapDecoder(validationStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                if (decoder.Frames.Count == 0 || decoder.Frames[0] == null)
                {
                    return null;
                }
                
                // Create BitmapImage from validated data
                var imageDataCopy = new byte[imageData.Length];
                Array.Copy(imageData, imageDataCopy, imageData.Length);
                
                var bitmap = new BitmapImage();
                var stream = new MemoryStream(imageDataCopy);
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

