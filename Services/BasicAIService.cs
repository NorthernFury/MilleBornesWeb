using MilleBornesWeb.Models;

namespace MilleBornesWeb.Services;

public class BasicAIService : IAIService
{
    private const int THINK_TIME_MS = 1500;

    public async Task ThinkAndPlay(GameManager game)
    {
        try
        {
            await Task.Delay(THINK_TIME_MS);
            game.DrawCard(game.AI);
            await Task.Delay(THINK_TIME_MS);

            // 1. Identify the best move by checking the CORRECT target
            Card? cardToPlay = DetermineBestMove(game, game.AI, game.Player);

            if (cardToPlay != null)
            {
                // IMPORTANT: Use the same target logic here as we used in validation
                var target = GetLogicalTarget(cardToPlay, game.AI, game.Player);
                game.AddLog($"AI played {cardToPlay.Name}.", TurnOwner.AI);
                game.ExecuteMove(cardToPlay, game.AI, target);
            }
            else
            {
                // 2. Discard logic: Keep high-value cards, toss the trash
                var discard = game.AI.Hand.OrderBy(c => GetCardWeight(c)).First();
                game.AddLog($"AI discarded a card.", TurnOwner.AI);
                game.DiscardCard(discard, game.AI);
            }
        }
        catch (Exception ex)
        {
            game.AddLog($"AI Error: {ex.Message}", TurnOwner.AI);
        }
    }

    private Card? DetermineBestMove(GameManager game, PlayerState ai, PlayerState player)
    {
        // Check cards in a specific priority order
        // We pass the CORRECT target to RuleBook.ValidateMove

        // Priority 1: Safeties
        var safety = ai.Hand.FirstOrDefault(c => c.Type == CardType.Safety &&
            RuleBook.ValidateMove(c, ai, GetLogicalTarget(c, ai, player)).IsValid);
        if (safety != null) return safety;

        // Priority 2: Remedies (Fixing itself)
        var remedy = ai.Hand.FirstOrDefault(c => (c.Type == CardType.Remedy || c.Type == CardType.EndLimit) &&
            RuleBook.ValidateMove(c, ai, GetLogicalTarget(c, ai, player)).IsValid);
        if (remedy != null) return remedy;

        // Priority 3: Distances (Highest first)
        var distance = ai.Hand.Where(c => c.Type == CardType.Distance &&
            RuleBook.ValidateMove(c, ai, GetLogicalTarget(c, ai, player)).IsValid)
            .OrderByDescending(c => c.Value)
            .FirstOrDefault();
        if (distance != null) return distance;

        // Priority 4: Sabotage (Hazards on player)
        var hazard = ai.Hand.FirstOrDefault(c => (c.Type == CardType.Hazard || c.Type == CardType.SpeedLimit) &&
            RuleBook.ValidateMove(c, ai, GetLogicalTarget(c, ai, player)).IsValid);

        return hazard;
    }

    /// <summary>
    /// Helper to decide if a card should be played on yourself or the opponent.
    /// </summary>
    private PlayerState GetLogicalTarget(Card card, PlayerState actor, PlayerState opponent)
    {
        return (card.Type == CardType.Hazard || card.Type == CardType.SpeedLimit)
            ? opponent
            : actor;
    }

    /// <summary>
    /// AI Weighting: Higher number = Keep, Lower number = Discard.
    /// </summary>
    private int GetCardWeight(Card c)
    {
        // Never discard safeties if possible
        if (c.Type == CardType.Safety) return 1000;

        // Roll cards are the most important thing to keep if you are stopped
        if (c.Name == CardNames.Roll) return 500;

        // Distance cards are weighted by their km value
        if (c.Type == CardType.Distance) return c.Value;

        // Hazards and Remedies are useful but replaceable
        return 50;
    }
}