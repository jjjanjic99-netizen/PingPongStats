using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services;

/// <summary>Picks a random quote for a category from whatever quotes.xml
/// currently contains. If the category has no entries (missing category, typo,
/// or the user deleted them all), this returns null - no quote is shown, and
/// nothing crashes.</summary>
public static class QuoteService
{
    public static string? PickRandomQuote(IReadOnlyList<Quote> quotes, string category, Func<int, int>? indexSelector = null)
    {
        if (string.IsNullOrEmpty(category)) return null;

        var matching = quotes.Where(q => q.Category == category).ToList();
        if (matching.Count == 0) return null;

        var selectIndex = indexSelector ?? (count => Random.Shared.Next(count));
        var index = selectIndex(matching.Count);
        if (index < 0 || index >= matching.Count) index = 0;

        return matching[index].Text;
    }
}
