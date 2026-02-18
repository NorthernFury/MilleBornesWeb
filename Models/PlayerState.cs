namespace MilleBornesWeb.Models;

public class PlayerState
{
    private List<Card> _hand = [];

    public string Name { get; set; } = "Player";
    public IReadOnlyList<Card> Hand => _hand;
    public List<Card> SafetyArea { get; set; } = [];
    public List<Card> SpeedPile { get; set; } = [];
    public List<Card> BattlePile { get; set; } = [];
    public List<Card> DistanceCards { get; set; } = [];

    public int TotalDistance => DistanceCards.Sum(c => c.Value);

    public bool HasRightOfWay => SafetyArea.Any(c => c.Name == "Right of Way");
    public bool HasExtraTank => SafetyArea.Any(c => c.Name == "Extra Tank");
    public bool HasPunctureProof => SafetyArea.Any(c => c.Name == "Puncture-Proof");
    public bool HasDrivingAce => SafetyArea.Any(c => c.Name == "Driving Ace");

    public bool IsSpeedLimited => SpeedPile.Any() &&
                                  SpeedPile.Last().Type == CardType.SpeedLimit &&
                                  !HasRightOfWay;

    public bool CanMove
    {
        get
        {
            // Right of Way = Permanent "Roll" state
            if (HasRightOfWay)
            {
                // You can move as long as there isn't a Hazard on top
                if (BattlePile.Any() && BattlePile.Last().Type == CardType.Hazard) return false;
                return true;
            }

            // Otherwise, you MUST have a Roll card on top to move
            return BattlePile.Any() && BattlePile.Last().Name == "Roll";
        }
    }

    public void AddToHand(Card card) => _hand.Add(card);
    public void RemoveFromHand(Card card) => _hand.Remove(card);
    public void ClearHand() => _hand.Clear();

    public int TotalScore { get; set; } = 0; // Cumulative across rounds
    public int CoupFourreCount { get; set; } = 0;

    public void Reset()
    {
        _hand.Clear();
        SafetyArea.Clear();
        SpeedPile.Clear();
        BattlePile.Clear();
        DistanceCards.Clear();
        CoupFourreCount = 0;
    }
}
