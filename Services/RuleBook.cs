using MilleBornesWeb.Models;

namespace MilleBornesWeb.Services;

public static class RuleBook
{
    public static MoveResult ValidateMove(Card card, PlayerState actor, PlayerState target)
    {
        // RULE: Distance Cards
        if (card.Type == CardType.Distance)
        {
            if (!actor.CanMove)
                return MoveResult.Failure("You must play a 'Roll' card first!");

            if (actor.IsSpeedLimited && card.Value > 50)
                return MoveResult.Failure("Speed Limit: Cannot play over 50km.");

            if (actor.TotalDistance + card.Value > 1000)
                return MoveResult.Failure("Cannot exceed 1000km exactly.");

            if (card.Value == 200 && actor.DistanceCards.Count(c => c.Value == 200) >= 2)
                return MoveResult.Failure("Limit: Only two 200km cards allowed per game.");

            return MoveResult.Success();
        }

        // RULE: Hazards
        if (card.Type == CardType.Hazard)
        {
            if (target.SafetyArea.Any(s => s.ProtectsAgainst(card.Name)))
                return MoveResult.Failure("Opponent is immune!");

            if (!target.CanMove)
                return MoveResult.Failure("Opponent is already stopped or hasn't started rolling.");

            return MoveResult.Success();
        }

        // RULE: Speed Limit
        if (card.Type == CardType.SpeedLimit)
        {
            if (target.HasRightOfWay) return MoveResult.Failure("Opponent has Right of Way.");
            if (target.IsSpeedLimited) return MoveResult.Failure("Already speed limited.");
            return MoveResult.Success();
        }

        // RULE: Remedies
        if (card.Type == CardType.Remedy || card.Type == CardType.EndLimit)
        {
            if (card.Type == CardType.EndLimit)
            {
                // Logic: Only play End Limit if the top card is actually a Speed Limit
                bool isLimited = actor.SpeedPile.Any() && actor.SpeedPile.Last().Name == CardNames.SpeedLimit;
                return isLimited ? MoveResult.Success() : MoveResult.Failure("Not limited.");
            }

            if (card.Name == CardNames.Roll)
            {
                if (actor.CanMove) return MoveResult.Failure("Already moving.");

                if (!actor.BattlePile.Any() || actor.BattlePile.Last().Name == CardNames.Stop)
                    return MoveResult.Success();

                if (actor.BattlePile.Last().Type == CardType.Remedy)
                    return MoveResult.Success();

                return MoveResult.Failure("Invalid Roll.");
            }

            if (!actor.BattlePile.Any()) return MoveResult.Failure("Nothing to fix.");

            var top = actor.BattlePile.Last();
            return (top.Type == CardType.Hazard && card.Fixes(top.Name))
                ? MoveResult.Success()
                : MoveResult.Failure("Doesn't fix top hazard.");
        }

        return MoveResult.Success(); // Safeties
    }
}
