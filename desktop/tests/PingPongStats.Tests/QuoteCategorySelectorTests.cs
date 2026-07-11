using PingPongStats.Core.Models;
using PingPongStats.Core.Services;
using Xunit;

namespace PingPongStats.Tests;

public class QuoteCategorySelectorTests
{
    [Fact]
    public void SelectCategory_ComebackTakesPriorityOverEverythingElse()
    {
        var category = QuoteCategorySelector.SelectCategory(
            isDoubles: true, isComeback: true, isUnderdogWin: true, winnerSets: 3, loserSets: 0);

        Assert.Equal(QuoteCategories.Comeback, category);
    }

    [Fact]
    public void SelectCategory_UnderdogTakesPriorityOverDoublesAndScoreShape()
    {
        var category = QuoteCategorySelector.SelectCategory(
            isDoubles: true, isComeback: false, isUnderdogWin: true, winnerSets: 3, loserSets: 0);

        Assert.Equal(QuoteCategories.UnderdogWin, category);
    }

    [Fact]
    public void SelectCategory_DoublesWinsWhenNoComebackOrUnderdog()
    {
        var category = QuoteCategorySelector.SelectCategory(
            isDoubles: true, isComeback: false, isUnderdogWin: false, winnerSets: 3, loserSets: 1);

        Assert.Equal(QuoteCategories.DoublesWin, category);
    }

    [Fact]
    public void SelectCategory_CleanSweepForSinglesShutout()
    {
        var category = QuoteCategorySelector.SelectCategory(
            isDoubles: false, isComeback: false, isUnderdogWin: false, winnerSets: 3, loserSets: 0);

        Assert.Equal(QuoteCategories.CleanSweep, category);
    }

    [Fact]
    public void SelectCategory_CloseWinForOneSetMargin()
    {
        var category = QuoteCategorySelector.SelectCategory(
            isDoubles: false, isComeback: false, isUnderdogWin: false, winnerSets: 3, loserSets: 2);

        Assert.Equal(QuoteCategories.CloseWin, category);
    }

    [Fact]
    public void SelectCategory_ReturnsEmptyWhenNothingMatches()
    {
        var category = QuoteCategorySelector.SelectCategory(
            isDoubles: false, isComeback: false, isUnderdogWin: false, winnerSets: 3, loserSets: 1);

        Assert.Equal(string.Empty, category);
    }
}
