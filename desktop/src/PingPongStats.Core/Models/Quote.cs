namespace PingPongStats.Core.Models;

/// <summary>One trash-talk quote for the win/confetti overlay, shown when its
/// Category matches what just happened. Persisted in the user-editable
/// quotes.xml - see QuoteCategories for the fixed set of category values the
/// app understands.</summary>
public class Quote
{
    public string Category { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

/// <summary>The fixed category identifiers quotes.xml entries must use to be
/// picked up. Anything else (typos, custom categories) is simply never
/// selected - no crash, no validation error, since the file is meant to be
/// hand-edited.</summary>
public static class QuoteCategories
{
    public const string CleanSweep = "CleanSweep";
    public const string CloseWin = "KnapperSieg";
    public const string Comeback = "Comeback";
    public const string DoublesWin = "DoppelSieg";
    public const string UnderdogWin = "UnderdogSieg";
}
