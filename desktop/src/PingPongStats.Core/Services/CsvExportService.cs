using System.Globalization;
using System.Text;
using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services;

/// <summary>
/// Exports players, matches, and dashboard statistics as Excel-compatible CSV
/// (semicolon-delimited, UTF-8). Semicolons are used instead of commas
/// because Excel in German locales treats comma as the decimal separator.
/// </summary>
public static class CsvExportService
{
    private const string Separator = ";";

    public static string ExportPlayers(IEnumerable<Player> players)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(Separator, "DisplayName", "FirstName", "LastName", "Email", "IsActive", "CreatedAt"));
        foreach (var p in players.OrderBy(p => p.DisplayName))
        {
            sb.AppendLine(string.Join(Separator,
                Escape(p.DisplayName), Escape(p.FirstName), Escape(p.LastName), Escape(p.Email),
                p.IsActive ? "Ja" : "Nein", p.CreatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }

    public static string ExportMatches(IEnumerable<Match> matches, IReadOnlyDictionary<Guid, Player> playersById)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(Separator, "PlayedAt", "PlayerA", "PlayerB", "PlayerASets", "PlayerBSets", "Winner", "Notes"));

        foreach (var m in matches.OrderByDescending(m => m.PlayedAt))
        {
            var playerA = playersById.GetValueOrDefault(m.PlayerAId)?.DisplayName ?? "?";
            var playerB = playersById.GetValueOrDefault(m.PlayerBId)?.DisplayName ?? "?";
            var winner = playersById.GetValueOrDefault(m.WinnerId)?.DisplayName ?? "?";

            sb.AppendLine(string.Join(Separator,
                m.PlayedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                Escape(playerA), Escape(playerB),
                m.PlayerASets.ToString(CultureInfo.InvariantCulture), m.PlayerBSets.ToString(CultureInfo.InvariantCulture),
                Escape(winner), Escape(m.Notes)));
        }

        return sb.ToString();
    }

    public static string ExportStatistics(IEnumerable<PlayerStatsSummary> summaries)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(Separator,
            "DisplayName", "Played", "Wins", "Losses", "WinRatePct", "WinRateLast30dPct",
            "CurrentStreak", "LongestWinStreak", "AvgSetsWonPerMatch", "EloRating"));

        foreach (var s in summaries.OrderByDescending(s => s.EloRating))
        {
            var streakLabel = s.CurrentStreak.Type switch
            {
                StreakType.Win => $"+{s.CurrentStreak.Length}",
                StreakType.Loss => $"-{s.CurrentStreak.Length}",
                _ => "0",
            };

            sb.AppendLine(string.Join(Separator,
                Escape(s.DisplayName),
                s.Played.ToString(CultureInfo.InvariantCulture),
                s.Wins.ToString(CultureInfo.InvariantCulture),
                s.Losses.ToString(CultureInfo.InvariantCulture),
                s.WinRatePct.ToString("F1", CultureInfo.InvariantCulture),
                s.WinRateLast30dPct.ToString("F1", CultureInfo.InvariantCulture),
                streakLabel,
                s.LongestWinStreak.ToString(CultureInfo.InvariantCulture),
                s.AvgSetsWonPerMatch.ToString("F2", CultureInfo.InvariantCulture),
                s.EloRating.ToString("F0", CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }

    private static string Escape(string value)
    {
        if (value.Contains(Separator) || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
