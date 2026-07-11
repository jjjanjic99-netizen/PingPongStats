using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services;

/// <summary>The five fixed time-of-day blocks per spec: before 10, 10-12, 12-14,
/// 14-17, after 17 (all boundaries based on Match.PlayedAt's local hour).</summary>
public enum TimeOfDayBlock
{
    BeforeTen,
    TenToTwelve,
    TwelveToFourteen,
    FourteenToSeventeen,
    AfterSeventeen,
}

public record TimeOfDayStats(TimeOfDayBlock Block, string Label, int Played, int Wins, int Losses, double WinRatePct);

/// <summary>
/// Win rate by time-of-day block (singles only), for the "Mein Profil" chart.
/// Blocks with fewer than MinGamesForDisplay games are still returned (the UI
/// greys them out rather than hiding them) - GetBestBlock separately requires
/// MinGamesForBestTimeCallout games before a block can be called out as
/// "your best time".
/// </summary>
public static class TimeOfDayService
{
    public const int MinGamesForDisplay = 3;
    public const int MinGamesForBestTimeCallout = 5;

    public static TimeOfDayBlock GetBlock(DateTime playedAt)
    {
        var hour = playedAt.Hour;
        if (hour < 10) return TimeOfDayBlock.BeforeTen;
        if (hour < 12) return TimeOfDayBlock.TenToTwelve;
        if (hour < 14) return TimeOfDayBlock.TwelveToFourteen;
        if (hour < 17) return TimeOfDayBlock.FourteenToSeventeen;
        return TimeOfDayBlock.AfterSeventeen;
    }

    public static string GetLabel(TimeOfDayBlock block) => block switch
    {
        TimeOfDayBlock.BeforeTen => "vor 10 Uhr",
        TimeOfDayBlock.TenToTwelve => "10-12 Uhr",
        TimeOfDayBlock.TwelveToFourteen => "12-14 Uhr",
        TimeOfDayBlock.FourteenToSeventeen => "14-17 Uhr",
        TimeOfDayBlock.AfterSeventeen => "nach 17 Uhr",
        _ => block.ToString(),
    };

    /// <summary>One entry per block, in a fixed chronological order, always five
    /// entries regardless of how many games the player has.</summary>
    public static List<TimeOfDayStats> GetStatsByBlock(IEnumerable<Match> matches, Guid playerId)
    {
        var played = StatsService.MatchesForPlayer(matches, playerId);

        return Enum.GetValues<TimeOfDayBlock>().Select(block =>
        {
            var inBlock = played.Where(m => GetBlock(m.PlayedAt) == block).ToList();
            var count = inBlock.Count;
            var wins = inBlock.Count(m => m.WinnerId == playerId);
            var winRatePct = count == 0 ? 0 : wins / (double)count * 100;
            return new TimeOfDayStats(block, GetLabel(block), count, wins, count - wins, winRatePct);
        }).ToList();
    }

    /// <summary>The block with the highest win rate, requiring at least
    /// minGames games in that block - otherwise null (no "best time" callout).</summary>
    public static TimeOfDayStats? GetBestBlock(IEnumerable<TimeOfDayStats> stats, int minGames = MinGamesForBestTimeCallout)
    {
        return stats
            .Where(s => s.Played >= minGames)
            .OrderByDescending(s => s.WinRatePct)
            .FirstOrDefault();
    }
}
