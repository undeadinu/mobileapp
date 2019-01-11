using CoreGraphics;
using Toggl.Multivac;
using UIKit;

namespace Toggl.Daneel.Extensions
{
    public static class ColorExtensions
    {
        public static string ToHexColor(this CGColor cgColor)
        {
            var r = (int)(cgColor.Components[0] * 255);
            var g = (int)(cgColor.Components[1] * 255);
            var b = (int)(cgColor.Components[2] * 255);

            return $"#{r:X02}{g:X02}{b:X02}";
        }

        public static UIColor ToNativeColor(this Color color)
            => UIColor.FromRGBA(color.Red, color.Green, color.Blue, color.Alpha);
    }
}
