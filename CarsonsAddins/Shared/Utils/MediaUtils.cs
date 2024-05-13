using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins.Utils
{
    public static class MediaUtils
    {
        /// <summary>
        /// Retrieves the image at the file location as a Resource Stream and converts it to a Bitmap. This is used to save images and icons to the compiled addin.
        /// </summary>
        /// <param name="assembly">The Addin Assembly</param>
        /// <param name="imagePath">The filepath the image is located</param>
        /// <returns>The image as a Bitmap</returns>
        public static System.Windows.Media.Imaging.BitmapSource GetImage(Assembly assembly, string imagePath)
        {
            try
            {
                Stream s = assembly.GetManifestResourceStream(imagePath);
                return System.Windows.Media.Imaging.BitmapFrame.Create(s);
            }
            catch
            {
                return null;
            }
        }


        /// <summary>
        /// Used to get a reference to the active Foreground Window for actions such as setting window focus to trigger Revit's ExternalEvents.
        /// </summary>
        /// <returns>Pointer referencing the active Foregound Window</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Used to set the active Foreground Window for actions such as immediately triggering Revit's ExternalEvents
        /// </summary>
        /// <param name="Pointer to the Window that will be set as the active Foreground Window"></param>
        /// <returns>Pointer referenceing the Window that focus will shift to</returns>
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(
          IntPtr hWnd);

    }
}
