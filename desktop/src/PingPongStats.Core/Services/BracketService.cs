using PingPongStats.Core.Helpers;
using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services;

/// <summary>
/// Single-elimination bracket construction and progression. Seeding uses the
/// standard tournament seed order (1 vs bracketSize, 2 vs bracketSize-1, ...,
/// recursively split) so the strongest entrants can only meet as late as
/// possible; when the entrant count isn't a power of two, the missing slots
/// become byes, which - by construction of the seed order - always land
/// against the top seeds first.
/// </summary>
public static class BracketService
{
    public const int MinEntrants = 2;

    /// <summary>Smallest power of two that is &gt;= n.</summary>
    public static int NextPowerOfTwo(int n)
    {
        var p = 1;
        while (p < n) p *= 2;
        return p;
    }

    /// <summary>The seed number (1-based) occupying each 0-based bracket position,
    /// for a bracket of the given power-of-two size. E.g. size 8 gives
    /// [1,8,4,5,2,7,3,6]: position 0/1 play seed 1 vs 8, etc.</summary>
    public static List<int> GenerateSeedOrder(int bracketSize)
    {
        if (bracketSize < 1 || (bracketSize & (bracketSize - 1)) != 0)
        {
            throw new ArgumentException("bracketSize must be a power of two.", nameof(bracketSize));
        }

        var seeds = new List<int> { 1 };
        while (seeds.Count < bracketSize)
        {
            var n = seeds.Count * 2;
            var next = new List<int>(n);
            foreach (var s in seeds)
            {
                next.Add(s);
                next.Add(n + 1 - s);
            }

            seeds = next;
        }

        return seeds;
    }

    /// <summary>Builds the full bracket tree (every round, including "TBD" slots
    /// for rounds beyond the first) for the given entrants. Entrants must already
    /// have Seed assigned (1 = strongest); byes are given to the highest seeds
    /// when entrants.Count isn't a power of two and auto-advance immediately (no
    /// match to play for that slot).</summary>
    public static List<TournamentMatchSlot> BuildBracket(IReadOnlyList<TournamentEntrant> entrants)
    {
        if (entrants.Count < MinEntrants)
        {
            throw new ValidationException($"Ein Turnier benötigt mindestens {MinEntrants} Teilnehmer.");
        }

        var bracketSize = NextPowerOfTwo(entrants.Count);
        var seedOrder = GenerateSeedOrder(bracketSize);
        var entrantsBySeed = entrants.ToDictionary(e => e.Seed);
        var totalRounds = (int)Math.Log2(bracketSize);

        var slots = new List<TournamentMatchSlot>();

        var round1Count = bracketSize / 2;
        var round1Slots = new List<TournamentMatchSlot>();
        for (var i = 0; i < round1Count; i++)
        {
            var seedA = seedOrder[i * 2];
            var seedB = seedOrder[i * 2 + 1];
            var entrantA = entrantsBySeed.GetValueOrDefault(seedA);
            var entrantB = entrantsBySeed.GetValueOrDefault(seedB);

            var slot = new TournamentMatchSlot
            {
                Round = 1,
                PositionInRound = i,
                EntrantAId = entrantA?.Id,
                EntrantBId = entrantB?.Id,
                EntrantAIsBye = entrantA is null,
                EntrantBIsBye = entrantB is null,
            };

            if (entrantA is not null && entrantB is null) slot.WinnerEntrantId = entrantA.Id;
            else if (entrantB is not null && entrantA is null) slot.WinnerEntrantId = entrantB.Id;

            round1Slots.Add(slot);
        }

        slots.AddRange(round1Slots);

        var previousRoundSlots = round1Slots;
        for (var round = 2; round <= totalRounds; round++)
        {
            var count = previousRoundSlots.Count / 2;
            var currentRoundSlots = new List<TournamentMatchSlot>();
            for (var i = 0; i < count; i++)
            {
                var feederA = previousRoundSlots[i * 2];
                var feederB = previousRoundSlots[i * 2 + 1];
                currentRoundSlots.Add(new TournamentMatchSlot
                {
                    Round = round,
                    PositionInRound = i,
                    EntrantAId = feederA.WinnerEntrantId,
                    EntrantBId = feederB.WinnerEntrantId,
                });
            }

            slots.AddRange(currentRoundSlots);
            previousRoundSlots = currentRoundSlots;
        }

        return slots;
    }

    /// <summary>True if this slot has both entrants known and hasn't been decided
    /// yet - i.e. it's actually playable right now.</summary>
    public static bool IsPlayable(TournamentMatchSlot slot) =>
        slot.EntrantAId is not null && slot.EntrantBId is not null && slot.WinnerEntrantId is null;

    /// <summary>Records a slot's result and feeds the winner into the next round
    /// (or completes the tournament if this was the final). Mutates the given
    /// tournament in place.</summary>
    public static void AdvanceWinner(Tournament tournament, Guid slotId, Guid winnerEntrantId, Guid? matchId)
    {
        var slot = tournament.Bracket.FirstOrDefault(s => s.Id == slotId)
            ?? throw new NotFoundException("Bracket-Partie wurde nicht gefunden.");

        slot.WinnerEntrantId = winnerEntrantId;
        slot.MatchId = matchId;

        var totalRounds = tournament.Bracket.Max(s => s.Round);
        if (slot.Round == totalRounds)
        {
            tournament.WinnerEntrantId = winnerEntrantId;
            tournament.Status = TournamentStatus.Completed;
            tournament.CompletedAt = Clock.Now();
            return;
        }

        var nextRound = slot.Round + 1;
        var nextPosition = slot.PositionInRound / 2;
        var nextSlot = tournament.Bracket.First(s => s.Round == nextRound && s.PositionInRound == nextPosition);

        if (slot.PositionInRound % 2 == 0) nextSlot.EntrantAId = winnerEntrantId;
        else nextSlot.EntrantBId = winnerEntrantId;
    }
}
