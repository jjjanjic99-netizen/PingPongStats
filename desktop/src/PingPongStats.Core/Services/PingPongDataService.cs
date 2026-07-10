using System.Text.RegularExpressions;
using PingPongStats.Core.Helpers;
using PingPongStats.Core.Models;
using PingPongStats.Core.Repositories;
using Match = PingPongStats.Core.Models.Match;

namespace PingPongStats.Core.Services;

/// <summary>
/// Single facade the ViewModels talk to for all player/match data access and
/// mutation. Holds an in-memory snapshot (refreshed via <see cref="Reload"/>)
/// so the UI can bind to simple lists, while every write goes through the
/// XML repositories' atomic, locked update sequence. This is the one place
/// business rules (validation, "can't delete a player with matches", audit
/// logging) are enforced - ViewModels never talk to repositories directly.
/// </summary>
public class PingPongDataService
{
    private static readonly Regex EmailPattern = new(@"^\S+@\S+\.\S+$", RegexOptions.Compiled);

    private readonly IPlayerRepository _playerRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly IAuditLogRepository? _auditLogRepository;

    public PingPongDataService(
        IPlayerRepository playerRepository,
        IMatchRepository matchRepository,
        IAuditLogRepository? auditLogRepository = null)
    {
        _playerRepository = playerRepository;
        _matchRepository = matchRepository;
        _auditLogRepository = auditLogRepository;
        Reload();
    }

    public IReadOnlyList<Player> Players { get; private set; } = new List<Player>();

    public IReadOnlyList<Match> Matches { get; private set; } = new List<Match>();

    /// <summary>Re-reads both XML files from disk. Called after every mutation and can
    /// also be triggered manually from Settings ("XML neu laden").</summary>
    public void Reload()
    {
        Players = _playerRepository.GetAll();
        Matches = _matchRepository.GetAll();
    }

    // ----- Players -----------------------------------------------------

    public Player CreatePlayer(string displayName, string firstName, string lastName, string email)
    {
        var trimmedName = (displayName ?? string.Empty).Trim();
        if (trimmedName.Length == 0)
        {
            throw new ValidationException("Anzeigename ist erforderlich.");
        }

        var trimmedEmail = (email ?? string.Empty).Trim();
        if (trimmedEmail.Length > 0 && !EmailPattern.IsMatch(trimmedEmail))
        {
            throw new ValidationException("E-Mail-Adresse ist ungültig.");
        }

        var now = Clock.Now();
        var player = new Player
        {
            DisplayName = trimmedName,
            FirstName = (firstName ?? string.Empty).Trim(),
            LastName = (lastName ?? string.Empty).Trim(),
            Email = trimmedEmail,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _playerRepository.Update(players =>
        {
            players.Add(player);
            return players;
        });

        LogAudit("PlayerCreated", $"{player.DisplayName} ({player.Id})");
        Reload();
        return player;
    }

    public void UpdatePlayer(Guid id, string displayName, string firstName, string lastName, string email)
    {
        var trimmedName = (displayName ?? string.Empty).Trim();
        if (trimmedName.Length == 0)
        {
            throw new ValidationException("Anzeigename ist erforderlich.");
        }

        var trimmedEmail = (email ?? string.Empty).Trim();
        if (trimmedEmail.Length > 0 && !EmailPattern.IsMatch(trimmedEmail))
        {
            throw new ValidationException("E-Mail-Adresse ist ungültig.");
        }

        _playerRepository.Update(players =>
        {
            var player = players.FirstOrDefault(p => p.Id == id)
                ?? throw new NotFoundException($"Spieler wurde nicht gefunden (möglicherweise durch einen anderen Benutzer gelöscht).");

            player.DisplayName = trimmedName;
            player.FirstName = (firstName ?? string.Empty).Trim();
            player.LastName = (lastName ?? string.Empty).Trim();
            player.Email = trimmedEmail;
            player.UpdatedAt = Clock.Now();
            return players;
        });

        LogAudit("PlayerUpdated", id.ToString());
        Reload();
    }

    /// <summary>Archives or reactivates a player. Historical matches are always kept.</summary>
    public void SetPlayerActive(Guid id, bool isActive)
    {
        _playerRepository.Update(players =>
        {
            var player = players.FirstOrDefault(p => p.Id == id)
                ?? throw new NotFoundException("Spieler wurde nicht gefunden.");
            player.IsActive = isActive;
            player.UpdatedAt = Clock.Now();
            return players;
        });

        LogAudit(isActive ? "PlayerReactivated" : "PlayerArchived", id.ToString());
        Reload();
    }

    /// <summary>Permanently deletes a player. Only allowed if no match references them.</summary>
    public void DeletePlayer(Guid id)
    {
        if (Matches.Any(m => m.PlayerAId == id || m.PlayerBId == id))
        {
            throw new ValidationException(
                "Dieser Spieler kann nicht gelöscht werden, da bereits Spiele erfasst wurden. " +
                "Bitte stattdessen deaktivieren.");
        }

        _playerRepository.Update(players =>
        {
            var removed = players.RemoveAll(p => p.Id == id);
            if (removed == 0)
            {
                throw new NotFoundException("Spieler wurde nicht gefunden.");
            }
            return players;
        });

        LogAudit("PlayerDeleted", id.ToString());
        Reload();
    }

    // ----- Matches -------------------------------------------------------

    public Match CreateMatch(DateTime playedAt, Guid playerAId, Guid playerBId, int playerASets, int playerBSets, string notes)
    {
        var winnerId = ValidationService.ComputeWinnerId(playerAId, playerBId, playerASets, playerBSets);
        ValidationService.EnsurePlayersExist(playerAId, playerBId, Players);

        var now = Clock.Now();
        var match = new Match
        {
            PlayedAt = playedAt,
            PlayerAId = playerAId,
            PlayerBId = playerBId,
            PlayerASets = playerASets,
            PlayerBSets = playerBSets,
            WinnerId = winnerId,
            Notes = (notes ?? string.Empty).Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        _matchRepository.Update(matches =>
        {
            matches.Add(match);
            return matches;
        });

        LogAudit("MatchCreated", $"{playerAId} vs {playerBId} ({playerASets}:{playerBSets})");
        Reload();
        return match;
    }

    public void UpdateMatch(
        Guid id, DateTime playedAt, Guid playerAId, Guid playerBId, int playerASets, int playerBSets, string notes)
    {
        var winnerId = ValidationService.ComputeWinnerId(playerAId, playerBId, playerASets, playerBSets);
        ValidationService.EnsurePlayersExist(playerAId, playerBId, Players);

        _matchRepository.Update(matches =>
        {
            var match = matches.FirstOrDefault(m => m.Id == id)
                ?? throw new NotFoundException("Spiel wurde nicht gefunden (möglicherweise bereits gelöscht).");

            match.PlayedAt = playedAt;
            match.PlayerAId = playerAId;
            match.PlayerBId = playerBId;
            match.PlayerASets = playerASets;
            match.PlayerBSets = playerBSets;
            match.WinnerId = winnerId;
            match.Notes = (notes ?? string.Empty).Trim();
            match.UpdatedAt = Clock.Now();
            return matches;
        });

        LogAudit("MatchUpdated", id.ToString());
        Reload();
    }

    public void DeleteMatch(Guid id)
    {
        _matchRepository.Update(matches =>
        {
            var removed = matches.RemoveAll(m => m.Id == id);
            if (removed == 0)
            {
                throw new NotFoundException("Spiel wurde nicht gefunden.");
            }
            return matches;
        });

        LogAudit("MatchDeleted", id.ToString());
        Reload();
    }

    /// <summary>Overwrites all players and matches (used only by the Debug-only seed
    /// data tool). Never called from normal production UI flows.</summary>
    public void ReplaceAllData(List<Player> players, List<Match> matches)
    {
        _playerRepository.Update(_ => players);
        _matchRepository.Update(_ => matches);
        LogAudit("SeedDataGenerated", $"{players.Count} Spieler, {matches.Count} Spiele");
        Reload();
    }

    private void LogAudit(string action, string details)
    {
        if (_auditLogRepository is null) return;

        try
        {
            _auditLogRepository.Append(new AuditLogEntry
            {
                Timestamp = Clock.Now(),
                User = Environment.UserName,
                Action = action,
                Details = details,
            });
        }
        catch (Exception ex)
        {
            // Audit logging is best-effort and must never block the actual data change.
            Logger.Warn($"Audit-Log-Eintrag konnte nicht geschrieben werden: {ex.Message}");
        }
    }
}
