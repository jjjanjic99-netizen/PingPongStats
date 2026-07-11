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
    private readonly IDoubleMatchRepository _doubleMatchRepository;
    private readonly IAuditLogRepository? _auditLogRepository;
    private readonly IQuoteRepository? _quoteRepository;

    public PingPongDataService(
        IPlayerRepository playerRepository,
        IMatchRepository matchRepository,
        IDoubleMatchRepository doubleMatchRepository,
        IAuditLogRepository? auditLogRepository = null,
        IQuoteRepository? quoteRepository = null)
    {
        _playerRepository = playerRepository;
        _matchRepository = matchRepository;
        _doubleMatchRepository = doubleMatchRepository;
        _auditLogRepository = auditLogRepository;
        _quoteRepository = quoteRepository;
        Reload();
    }

    public IReadOnlyList<Player> Players { get; private set; } = new List<Player>();

    public IReadOnlyList<Match> Matches { get; private set; } = new List<Match>();

    public IReadOnlyList<DoubleMatch> DoubleMatches { get; private set; } = new List<DoubleMatch>();

    /// <summary>Trash-talk quotes from quotes.xml (Phase 8). Empty if no
    /// IQuoteRepository was supplied, e.g. in tests that don't need it.</summary>
    public IReadOnlyList<Quote> Quotes { get; private set; } = new List<Quote>();

    /// <summary>Re-reads all XML files from disk. Called after every mutation and can
    /// also be triggered manually from Settings ("XML neu laden").</summary>
    public void Reload()
    {
        Players = _playerRepository.GetAll();
        Matches = _matchRepository.GetAll();
        DoubleMatches = _doubleMatchRepository.GetAll();
        Quotes = _quoteRepository?.GetAll() ?? new List<Quote>();
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

    /// <summary>Sets or clears (pass empty string) a player's avatar file name. The
    /// actual image file is processed/saved separately by IAvatarImageService (WPF
    /// side) before this is called - this just records the resulting file name.</summary>
    public void SetPlayerAvatar(Guid id, string avatarFileName)
    {
        _playerRepository.Update(players =>
        {
            var player = players.FirstOrDefault(p => p.Id == id)
                ?? throw new NotFoundException("Spieler wurde nicht gefunden.");
            player.AvatarFileName = avatarFileName ?? string.Empty;
            player.UpdatedAt = Clock.Now();
            return players;
        });

        LogAudit("PlayerAvatarChanged", id.ToString());
        Reload();
    }

    /// <summary>Sets a player's login PIN (exactly 4 digits, stored as a PBKDF2 hash +
    /// salt, never in plaintext), or clears it when pin is null/empty. This is a
    /// convenience against accidentally opening someone else's profile, not real
    /// security - the underlying XML files remain plainly readable.</summary>
    public void SetPlayerPin(Guid id, string? pin)
    {
        if (!string.IsNullOrEmpty(pin) && !PinService.IsValidPinFormat(pin))
        {
            throw new ValidationException("Der PIN muss aus genau 4 Ziffern bestehen.");
        }

        _playerRepository.Update(players =>
        {
            var player = players.FirstOrDefault(p => p.Id == id)
                ?? throw new NotFoundException("Spieler wurde nicht gefunden.");

            if (string.IsNullOrEmpty(pin))
            {
                player.PinHash = string.Empty;
                player.PinSalt = string.Empty;
            }
            else
            {
                var (hash, salt) = PinService.HashPin(pin);
                player.PinHash = hash;
                player.PinSalt = salt;
            }

            player.UpdatedAt = Clock.Now();
            return players;
        });

        LogAudit("PlayerPinChanged", id.ToString());
        Reload();
    }

    /// <summary>True if the player has a PIN set (login requires entering it).</summary>
    public bool PlayerHasPin(Guid id) =>
        Players.FirstOrDefault(p => p.Id == id) is { } player && !string.IsNullOrEmpty(player.PinHash);

    /// <summary>Verifies a login PIN attempt for a player. Returns false (rather than
    /// throwing) for a wrong PIN so the login screen can just show an error.</summary>
    public bool VerifyPlayerPin(Guid id, string pin)
    {
        var player = Players.FirstOrDefault(p => p.Id == id)
            ?? throw new NotFoundException("Spieler wurde nicht gefunden.");
        return PinService.VerifyPin(pin, player.PinHash, player.PinSalt);
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

    public Match CreateMatch(
        DateTime playedAt, Guid playerAId, Guid playerBId, int playerASets, int playerBSets, string notes,
        List<SetResult>? setResults = null)
    {
        var winnerId = ValidationService.ComputeWinnerId(playerAId, playerBId, playerASets, playerBSets);
        ValidationService.EnsurePlayersExist(playerAId, playerBId, Players);
        ValidationService.ValidateSetResults(setResults, playerASets, playerBSets);

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
            SetResults = setResults ?? new(),
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
        Guid id, DateTime playedAt, Guid playerAId, Guid playerBId, int playerASets, int playerBSets, string notes,
        List<SetResult>? setResults = null)
    {
        var winnerId = ValidationService.ComputeWinnerId(playerAId, playerBId, playerASets, playerBSets);
        ValidationService.EnsurePlayersExist(playerAId, playerBId, Players);
        ValidationService.ValidateSetResults(setResults, playerASets, playerBSets);

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
            match.SetResults = setResults ?? new();
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

    // ----- Doubles matches -----------------------------------------------

    public DoubleMatch CreateDoubleMatch(
        DateTime playedAt,
        Guid teamAPlayer1Id, Guid teamAPlayer2Id, Guid teamBPlayer1Id, Guid teamBPlayer2Id,
        int teamASets, int teamBSets, string notes, List<SetResult>? setResults = null)
    {
        var winningTeam = ValidationService.ComputeWinningTeam(
            teamAPlayer1Id, teamAPlayer2Id, teamBPlayer1Id, teamBPlayer2Id, teamASets, teamBSets);
        ValidationService.EnsurePlayersExist(
            new[] { teamAPlayer1Id, teamAPlayer2Id, teamBPlayer1Id, teamBPlayer2Id }, Players);
        ValidationService.ValidateSetResults(setResults, teamASets, teamBSets);

        var now = Clock.Now();
        var match = new DoubleMatch
        {
            PlayedAt = playedAt,
            TeamAPlayer1Id = teamAPlayer1Id,
            TeamAPlayer2Id = teamAPlayer2Id,
            TeamBPlayer1Id = teamBPlayer1Id,
            TeamBPlayer2Id = teamBPlayer2Id,
            TeamASets = teamASets,
            TeamBSets = teamBSets,
            WinningTeam = winningTeam,
            Notes = (notes ?? string.Empty).Trim(),
            SetResults = setResults ?? new(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        _doubleMatchRepository.Update(matches =>
        {
            matches.Add(match);
            return matches;
        });

        LogAudit("DoubleMatchCreated",
            $"{teamAPlayer1Id}+{teamAPlayer2Id} vs {teamBPlayer1Id}+{teamBPlayer2Id} ({teamASets}:{teamBSets})");
        Reload();
        return match;
    }

    public void DeleteDoubleMatch(Guid id)
    {
        _doubleMatchRepository.Update(matches =>
        {
            var removed = matches.RemoveAll(m => m.Id == id);
            if (removed == 0)
            {
                throw new NotFoundException("Doppel-Spiel wurde nicht gefunden.");
            }
            return matches;
        });

        LogAudit("DoubleMatchDeleted", id.ToString());
        Reload();
    }

    /// <summary>Overwrites all players, matches, and doubles matches (used only by the
    /// Debug-only seed data tool). Never called from normal production UI flows.</summary>
    public void ReplaceAllData(List<Player> players, List<Match> matches, List<DoubleMatch> doubleMatches)
    {
        _playerRepository.Update(_ => players);
        _matchRepository.Update(_ => matches);
        _doubleMatchRepository.Update(_ => doubleMatches);
        LogAudit("SeedDataGenerated",
            $"{players.Count} Spieler, {matches.Count} Spiele, {doubleMatches.Count} Doppel-Spiele");
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
