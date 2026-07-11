using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services;

/// <summary>
/// Decides which single quote category applies to a just-saved match, when
/// several could technically match (e.g. a doubles win that was also a
/// comeback). Priority (most narratively significant first): Comeback,
/// Underdog-Sieg, Doppel-Sieg, Clean-Sweep, Knapper Sieg. Returns
/// string.Empty if nothing qualifies (e.g. a singles win by 2 sets that
/// wasn't a comeback or upset) - QuoteService then simply shows no quote.
/// </summary>
public static class QuoteCategorySelector
{
    public const int CloseWinSetMargin = 1;

    public static string SelectCategory(bool isDoubles, bool isComeback, bool isUnderdogWin, int winnerSets, int loserSets)
    {
        if (isComeback) return QuoteCategories.Comeback;
        if (isUnderdogWin) return QuoteCategories.UnderdogWin;
        if (isDoubles) return QuoteCategories.DoublesWin;
        if (loserSets == 0) return QuoteCategories.CleanSweep;
        if (winnerSets - loserSets == CloseWinSetMargin) return QuoteCategories.CloseWin;

        return string.Empty;
    }
}
