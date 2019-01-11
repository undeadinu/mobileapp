using AndroidColor = Android.Graphics.Color;
using Toggl.Multivac;

namespace Toggl.Giskard.Extensions
{
    public static class ColorExtensions
    {
        public static AndroidColor ToNativeColor(this Color color)
            => new AndroidColor(color.Red, color.Green, color.Blue, color.Alpha);
    }
}
