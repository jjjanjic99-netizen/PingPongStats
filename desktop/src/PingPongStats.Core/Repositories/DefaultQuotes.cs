using PingPongStats.Core.Models;

namespace PingPongStats.Core.Repositories;

/// <summary>The default trash-talk quotes written to quotes.xml on first run. Purely
/// seed data for QuoteXmlRepository.EnsureSeeded - editing/removing entries in the
/// resulting XML file is the whole point of this feature.</summary>
public static class DefaultQuotes
{
    public static IReadOnlyList<Quote> All { get; } = new List<Quote>
    {
        new() { Category = QuoteCategories.CleanSweep, Text = "Nicht einen Satz abgegeben - Respekt, aber auch: autsch." },
        new() { Category = QuoteCategories.CleanSweep, Text = "Sauberer geht's nicht. Der Gegner darf jetzt lüften gehen." },
        new() { Category = QuoteCategories.CleanSweep, Text = "0 Sätze für den Verlierer - das nennt man Hausaufgaben verteilen." },
        new() { Category = QuoteCategories.CleanSweep, Text = "Blitzsauber durchgespielt. Nächstes Mal vielleicht ein Satz als Trostpreis?" },

        new() { Category = QuoteCategories.CloseWin, Text = "Hauchdünn! Da hat wohl jemand bis zuletzt gezittert." },
        new() { Category = QuoteCategories.CloseWin, Text = "Enger geht's kaum - Glückwunsch zum Krimi-Sieg." },
        new() { Category = QuoteCategories.CloseWin, Text = "Das war Nervenkitzel pur. Gewonnen ist gewonnen." },
        new() { Category = QuoteCategories.CloseWin, Text = "Um Haaresbreite! Die Schweissperlen haben sich gelohnt." },

        new() { Category = QuoteCategories.Comeback, Text = "Von wegen chancenlos - das war ein Comeback für die Geschichtsbücher!" },
        new() { Category = QuoteCategories.Comeback, Text = "Totgesagte spielen länger. Was für eine Aufholjagd!" },
        new() { Category = QuoteCategories.Comeback, Text = "Erst abgeschrieben, dann alles gedreht - Kopfkino at its best." },
        new() { Category = QuoteCategories.Comeback, Text = "Rückstand? Welcher Rückstand? Das war eine Ansage." },

        new() { Category = QuoteCategories.DoublesWin, Text = "Teamwork makes the dream work - starkes Doppel!" },
        new() { Category = QuoteCategories.DoublesWin, Text = "Zwei gegen zwei, aber es fühlte sich an wie eine Übermacht." },
        new() { Category = QuoteCategories.DoublesWin, Text = "Perfekt eingespielt - da stimmt die Chemie am Tisch." },
        new() { Category = QuoteCategories.DoublesWin, Text = "Als Team unschlagbar - so geht Doppel." },

        new() { Category = QuoteCategories.UnderdogWin, Text = "Achtung, Überraschung! Die Elo-Rangliste war heute nur eine Empfehlung." },
        new() { Category = QuoteCategories.UnderdogWin, Text = "David gegen Goliath - und David hat gewonnen. Chapeau!" },
        new() { Category = QuoteCategories.UnderdogWin, Text = "Papierform? Nie gehört. Das war ein verdienter Aussenseiter-Coup." },
        new() { Category = QuoteCategories.UnderdogWin, Text = "Favorit geschlagen - notiert euch diesen Namen." },
    };
}
