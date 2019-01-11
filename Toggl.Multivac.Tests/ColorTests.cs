using System;
using FluentAssertions;
using FsCheck.Xunit;
using Toggl.Multivac;
using Xunit;

namespace Toggl.Multivac.Tests
{
    public sealed class ColorTests
    {
        public sealed class TheHexStringConstructor
        {
            [Theory, LogIfTooSlow]
            [InlineData("000000", 0, 0, 0)]
            [InlineData("0000FF", 0, 0, 255)]
            [InlineData("00FF00", 0, 255, 0)]
            [InlineData("FF0000", 255, 0, 0)]
            [InlineData("FFFFFF", 255, 255, 255)]
            [InlineData("5FE5FF", 95, 229, 255)]
            [InlineData("C3783C", 195, 120, 60)]
            [InlineData("C3783C", 195, 120, 60)]
            [InlineData("625876", 98, 88, 118)]
            public void WorksCorrectlyWhenThereIsNoAlphaAnNoNumberSign(string hex, byte expectedRed, byte expectedGreen, byte expectedBlue)
            {
                var color = new Color(hex);

                assertExpectedValues(color, 255, expectedRed, expectedGreen, expectedBlue);
            }

            [Theory, LogIfTooSlow]
            [InlineData("#000000", 0, 0, 0)]
            [InlineData("#0000FF", 0, 0, 255)]
            [InlineData("#00FF00", 0, 255, 0)]
            [InlineData("#FF0000", 255, 0, 0)]
            [InlineData("#FFFFFF", 255, 255, 255)]
            [InlineData("#5FE5FF", 95, 229, 255)]
            [InlineData("#C3783C", 195, 120, 60)]
            [InlineData("#C3783C", 195, 120, 60)]
            [InlineData("#625876", 98, 88, 118)]
            public void WorksCorrectlyWhenThereIsNoAlphaButThereIsANumberSign(string hex, byte expectedRed, byte expectedGreen, byte expectedBlue)
            {
                var color = new Color(hex);

                assertExpectedValues(color, 255, expectedRed, expectedGreen, expectedBlue);
            }

            [Theory, LogIfTooSlow]
            [InlineData("00000000", 0, 0, 0, 0)]
            [InlineData("010000FF", 1, 0, 0, 255)]
            [InlineData("0200FF00", 2, 0, 255, 0)]
            [InlineData("FFFF0000", 255, 255, 0, 0)]
            [InlineData("AFFFFFFF", 175, 255, 255, 255)]
            [InlineData("5F5FE5FF", 95, 95, 229, 255)]
            [InlineData("CCC3783C", 204, 195, 120, 60)]
            [InlineData("BAC3783C", 186, 195, 120, 60)]
            [InlineData("CF625876", 207, 98, 88, 118)]
            public void WorksCorrectlyWhenThereIsAlphaAndNoNumberSign(string hex, byte expectedAlpha, byte expectedRed, byte expectedGreen, byte expectedBlue)
            {
                var color = new Color(hex);

                assertExpectedValues(color, expectedAlpha, expectedRed, expectedGreen, expectedBlue);
            }

            [Theory, LogIfTooSlow]
            [InlineData("#00000000", 0, 0, 0, 0)]
            [InlineData("#010000FF", 1, 0, 0, 255)]
            [InlineData("#0200FF00", 2, 0, 255, 0)]
            [InlineData("#FFFF0000", 255, 255, 0, 0)]
            [InlineData("#AFFFFFFF", 175, 255, 255, 255)]
            [InlineData("#5F5FE5FF", 95, 95, 229, 255)]
            [InlineData("#CCC3783C", 204, 195, 120, 60)]
            [InlineData("#BAC3783C", 186, 195, 120, 60)]
            [InlineData("#CF625876", 207, 98, 88, 118)]
            public void WorksCorrectlyWhenThereIsAlphaANumberSign(string hex, byte expectedAlpha, byte expectedRed, byte expectedGreen, byte expectedBlue)
            {
                var color = new Color(hex);

                assertExpectedValues(color, expectedAlpha, expectedRed, expectedGreen, expectedBlue);
            }

            [Theory, LogIfTooSlow]
            [InlineData("00000000", 0, 0, 0, 0)]
            [InlineData("#010000ff", 1, 0, 0, 255)]
            [InlineData("0200Ff00", 2, 0, 255, 0)]
            [InlineData("#ff0000", 255, 255, 0, 0)]
            [InlineData("#aFfFfFfF", 175, 255, 255, 255)]
            [InlineData("#ccc3783c", 204, 195, 120, 60)]
            [InlineData("baC3783C", 186, 195, 120, 60)]
            public void WorksCorrectlyWithLowercaseLetters(string hex, byte expectedAlpha, byte expectedRed, byte expectedGreen, byte expectedBlue)
            {
                var color = new Color(hex);

                assertExpectedValues(color, expectedAlpha, expectedRed, expectedGreen, expectedBlue);
            }

            [Theory, LogIfTooSlow]
            [InlineData("1")]
            [InlineData("12")]
            [InlineData("123")]
            [InlineData("1234")]
            [InlineData("12345")]
            [InlineData("1234567")]
            [InlineData("123456789")]
            [InlineData("#1")]
            [InlineData("#12")]
            [InlineData("#123")]
            [InlineData("#1234")]
            [InlineData("#12345")]
            [InlineData("#1234567")]
            [InlineData("#123456789")]
            public void ThrowsInvalidArgumentExceptionWhenTheStringHasWrongLength(string hex)
            {
                Action construction = () => new Color(hex);

                construction.Should().Throw<ArgumentException>();
            }

            private void assertExpectedValues(Color color, byte expectedAlpha, byte expectedRed, byte expectedGreen, byte expectedBlue)
            {
                color.Alpha.Should().Be(expectedAlpha);
                color.Red.Should().Be(expectedRed);
                color.Green.Should().Be(expectedGreen);
                color.Blue.Should().Be(expectedBlue);
            }
        }
    }
}
