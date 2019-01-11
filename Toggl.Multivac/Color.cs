using System;
namespace Toggl.Multivac
{
    public struct Color
    {
        public byte Alpha { get; }
        public byte Red { get; }
        public byte Green { get; }
        public byte Blue { get; }

        public Color(byte red, byte green, byte blue, byte alpha = 255)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        public Color(uint argb)
        {
            Alpha = (byte)((argb >> 24) & 255);
            Red = (byte)((argb >> 16) & 255);
            Green = (byte)((argb >> 8) & 255);
            Blue = (byte)(argb & 255);
        }

        /// <summary>
        /// Creates a Color from a hexadecimal string. Valid formats: aarrggbb, #aarrggbb, rrggbb, #rrggbb
        /// </summary>
        public Color(string hex) : this(hexStringToInt(hex))
        {
        }

        private static uint hexStringToInt(string hex)
        {
            Ensure.Argument.IsNotNullOrWhiteSpaceString(hex, nameof(hex));

            hex = hex.TrimStart('#');

            var hexLength = hex.Length;

            if (hexLength == 6)
                return 0xFF000000 + Convert.ToUInt32(hex, 16);

            if (hexLength == 8)
                return Convert.ToUInt32(hex, 16);

            throw new ArgumentException("Invalid hex string was provided. Valid formats: aarrggbb, #aarrggbb, rrggbb, #rrggbb");
        }

        public override string ToString()
        {
            return $"{{a={Alpha}, r={Red}, g={Green}, b={Blue}}}";
        }
    }
}