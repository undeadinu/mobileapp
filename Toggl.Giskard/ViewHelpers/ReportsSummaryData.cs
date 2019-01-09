using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Toggl.Foundation.Reports;

namespace Toggl.Giskard.ViewHelpers
{
    public struct ReportsSummaryData
    {
        public readonly IReadOnlyList<ChartSegment> Segments;
        public readonly bool ShowEmptyState;
        public readonly TimeSpan TotalTime;
        public readonly bool TotalTimeIsZero;
        public readonly float BillablePercentage;

        private ReportsSummaryData(IReadOnlyList<ChartSegment> segments, bool showEmptyState, TimeSpan totalTime, bool totalTimeIsZero, float billablePercentage)
        {
            Segments = segments;
            ShowEmptyState = showEmptyState;
            TotalTime = totalTime;
            TotalTimeIsZero = totalTimeIsZero;
            BillablePercentage = billablePercentage;
        }

        public static ReportsSummaryData Empty()
        {
            return new ReportsSummaryData(ImmutableList<ChartSegment>.Empty, false, TimeSpan.Zero, true, 0f);
        }

        public static ReportsSummaryData Create(IReadOnlyList<ChartSegment> segments, bool showEmptyState, TimeSpan totalTime, bool totalTimeIsZero, float? billablePercentage)
        {
            return new ReportsSummaryData(segments, showEmptyState, totalTime, totalTimeIsZero, billablePercentage ?? 0f);
        }
    }
}
