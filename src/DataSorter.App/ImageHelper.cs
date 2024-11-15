using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Imaging;

namespace DataSorter.App;

public static class ImageHelper
{
    // We init this once so that if the function is repeatedly called it isn't stressing the garbage man
    private static readonly Regex r = new Regex(":");
    

    /// <summary>
    ///     Retrieves the datetime WITHOUT loading the whole image.
    /// </summary>
    public static DateTime GetDateTakenFromImage(FileInfo fileInfo)
    {
        using (FileStream fs = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read))
        using (Image myImage = Image.FromStream(fs, false, false))
        {
            PropertyItem? propItem = null;

            try
            {
                propItem = myImage.GetPropertyItem(36867);
            }
            catch { }
            
            if (propItem != null)
            {
                string dateTaken = r.Replace(Encoding.UTF8.GetString(propItem.Value), "-", 2);
                return DateTime.Parse(dateTaken);
            }
            else
                return fileInfo.LastWriteTime;
        }
    }
}