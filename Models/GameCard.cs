namespace MilleBornesWeb.Models;

public enum CardType { Distance, Hazard, Remedy, Safety, SpeedLimit, EndLimit }

public class Card
{
    public string Name { get; set; } = string.Empty;
    public CardType Type { get; set; }
    public int Value { get; set; }
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Checks if this card (a Remedy or End Limit) can fix a specific hazard.
    /// </summary>
    public bool Fixes(string hazardName)
    {
        if (Type != CardType.Remedy && Type != CardType.EndLimit) return false;

        return (Name, hazardName) switch
        {
            (CardNames.Roll, CardNames.Stop) => true,
            (CardNames.EndLimit, CardNames.SpeedLimit) => true,
            (CardNames.Gasoline, CardNames.OutOfGas) => true,
            (CardNames.SpareTire, CardNames.FlatTire) => true,
            (CardNames.Repairs, CardNames.Accident) => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if this card (a Safety) provides permanent immunity to a specific hazard.
    /// </summary>
    public bool ProtectsAgainst(string hazardName)
    {
        if (Type != CardType.Safety) return false;

        return (Name, hazardName) switch
        {
            // Right of Way protects against BOTH Stop and Speed Limit
            (CardNames.Safeties.RightOfWay, CardNames.Stop) => true,
            (CardNames.Safeties.RightOfWay, CardNames.SpeedLimit) => true,

            (CardNames.Safeties.ExtraTank, CardNames.OutOfGas) => true,
            (CardNames.Safeties.PunctureProof, CardNames.FlatTire) => true,
            (CardNames.Safeties.DrivingAce, CardNames.Accident) => true,
            _ => false
        };
    }
}