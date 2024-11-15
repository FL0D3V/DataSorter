using System.Text;

namespace DataSorter.App;

public static class TimeSpanHelper
{
    public static string ToPrettyFormat(this TimeSpan span) {
        if (span == TimeSpan.Zero) return "0 seconds";

        var formats = new List<string>();
        
        if (span.Days > 0)
            formats.Add(string.Format("{0} day{1}", span.Days, span.Days > 1 ? "s" : String.Empty));
        if (span.Hours > 0)
            formats.Add(string.Format("{0} hour{1}", span.Hours, span.Hours > 1 ? "s" : String.Empty));
        if (span.Minutes > 0)
            formats.Add(string.Format("{0} minute{1}", span.Minutes, span.Minutes > 1 ? "s" : String.Empty));
        if (span.Seconds > 0)
            formats.Add(string.Format("{0} second{1}", span.Seconds, span.Seconds > 1 ? "s" : String.Empty));
        if (span.Milliseconds > 0)
            formats.Add(string.Format("{0} millisecond{1}", span.Milliseconds, span.Milliseconds > 1 ? "s" : String.Empty));
        
        return formats
            .Aggregate("", (last, current)
                => string.IsNullOrWhiteSpace(last) ? current : last + ", " + current);
    }
}