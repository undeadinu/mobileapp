using System;
using System.Globalization;
using MvvmCross.Converters;

namespace Toggl.Foundation.MvvmCross.Converters
{
    public abstract class BaseReportPercentageLabelValueConverter<TString>
    {
        public TString Convert(float? value)
        {
            var isDisabled = value == null;
            var actualValue = isDisabled ? 0 : value.Value;

            var percentage = $"{actualValue.ToString("0.00")}%";

            return GetFormattedString(percentage, isDisabled);
        }

        protected abstract TString GetFormattedString(string percentage, bool isDisabled);
    }
}
