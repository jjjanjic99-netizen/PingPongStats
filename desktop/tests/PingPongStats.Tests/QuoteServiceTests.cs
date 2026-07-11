using PingPongStats.Core.Models;
using PingPongStats.Core.Services;
using Xunit;

namespace PingPongStats.Tests;

public class QuoteServiceTests
{
    [Fact]
    public void PickRandomQuote_ReturnsNull_WhenCategoryHasNoEntries()
    {
        var quotes = new List<Quote> { new() { Category = QuoteCategories.Comeback, Text = "A" } };

        var result = QuoteService.PickRandomQuote(quotes, QuoteCategories.CleanSweep);

        Assert.Null(result);
    }

    [Fact]
    public void PickRandomQuote_ReturnsNull_WhenNoQuotesAtAll()
    {
        Assert.Null(QuoteService.PickRandomQuote(new List<Quote>(), QuoteCategories.Comeback));
    }

    [Fact]
    public void PickRandomQuote_ReturnsNull_WhenCategoryIsEmpty()
    {
        var quotes = new List<Quote> { new() { Category = QuoteCategories.Comeback, Text = "A" } };
        Assert.Null(QuoteService.PickRandomQuote(quotes, string.Empty));
    }

    [Fact]
    public void PickRandomQuote_ReturnsTheOnlyMatchingQuote()
    {
        var quotes = new List<Quote>
        {
            new() { Category = QuoteCategories.Comeback, Text = "Comeback-Spruch" },
            new() { Category = QuoteCategories.CleanSweep, Text = "Anderer Spruch" },
        };

        var result = QuoteService.PickRandomQuote(quotes, QuoteCategories.Comeback);

        Assert.Equal("Comeback-Spruch", result);
    }

    [Fact]
    public void PickRandomQuote_OnlyEverPicksFromTheRequestedCategory()
    {
        var quotes = new List<Quote>
        {
            new() { Category = QuoteCategories.Comeback, Text = "C1" },
            new() { Category = QuoteCategories.Comeback, Text = "C2" },
            new() { Category = QuoteCategories.CleanSweep, Text = "Sweep" },
        };

        for (var i = 0; i < 20; i++)
        {
            var result = QuoteService.PickRandomQuote(quotes, QuoteCategories.Comeback);
            Assert.Contains(result, new[] { "C1", "C2" });
        }
    }

    [Fact]
    public void PickRandomQuote_UsesProvidedIndexSelector()
    {
        var quotes = new List<Quote>
        {
            new() { Category = QuoteCategories.Comeback, Text = "First" },
            new() { Category = QuoteCategories.Comeback, Text = "Second" },
        };

        Assert.Equal("First", QuoteService.PickRandomQuote(quotes, QuoteCategories.Comeback, _ => 0));
        Assert.Equal("Second", QuoteService.PickRandomQuote(quotes, QuoteCategories.Comeback, _ => 1));
    }
}
