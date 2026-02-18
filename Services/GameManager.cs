using MilleBornesWeb.Models;

namespace MilleBornesWeb.Services;

public enum TurnOwner { Player, AI }
public enum TurnPhase { Draw, Play }

public class GameManager
{
    private readonly IAIService _aiService;
    public GameManager(IAIService aiService) => _aiService = aiService;

    public List<Card> Deck { get; set; } = [];
    public List<Card> DiscardPile { get; set; } = [];
    public PlayerState Player { get; set; } = new() { Name = "Player 1" };
    public PlayerState AI { get; set; } = new() { Name = "AI Opponent" };
    public List<LogEntry> Logs { get; private set; } = [];

    private readonly Random _rng = new();

    public bool IsWaitingForCoupFourre { get; private set; }
    public Card? PendingHazard { get; private set; }

    public TurnOwner CurrentTurn { get; private set; } = TurnOwner.Player;
    public TurnPhase CurrentPhase { get; private set; } = TurnPhase.Draw;
    private TurnOwner? _lastRoundStarter;

    public bool GameStarted { get; private set; } = false;

    public bool GameEnded { get; private set; } = false;
    public bool MatchEnded { get; private set; } = false;

    public event Action? OnNotify;
    private void NotifyStateChanged() => OnNotify?.Invoke();

    public void StartNewGame()
    {
        Logs.Clear();

        BuildDeck();
        ShuffleDeck();
        Player.Reset();
        AI.Reset();

        MatchEnded = false;

        if (_lastRoundStarter == null)
        {
            // First round of a new match: Random selection
            CurrentTurn = _rng.Next(2) == 0 ? TurnOwner.Player : TurnOwner.AI;
            AddLog($"Match Start! {(CurrentTurn == TurnOwner.Player ? "You were" : "AI was")} randomly selected to start.", TurnOwner.Player);
        }
        else
        {
            // Subsequent rounds: Alternate
            CurrentTurn = (_lastRoundStarter == TurnOwner.Player) ? TurnOwner.AI : TurnOwner.Player;
            AddLog($"New Round! It is {(CurrentTurn == TurnOwner.Player ? "your" : "AI's")} turn to start.", CurrentTurn);
        }

        // Save who started this round for the next one
        _lastRoundStarter = CurrentTurn;

        CurrentPhase = TurnPhase.Draw;
        GameEnded = false;
        GameStarted = true;

        for (int i = 0; i < 6; i++)
        {
            MoveCardFromDeckToHand(Player);
            MoveCardFromDeckToHand(AI);
        }

        NotifyStateChanged();

        if (CurrentTurn == TurnOwner.AI)
        {
            _ = _aiService.ThinkAndPlay(this);
        }
    }

    public void DrawCard(PlayerState player)
    {
        if (CurrentPhase != TurnPhase.Draw) return;

        if (Deck.Any())
        {
            MoveCardFromDeckToHand(player);
            CurrentPhase = TurnPhase.Play;
        }
        else
        {
            CurrentPhase = TurnPhase.Play;
            CheckForEndGame();
        }
        NotifyStateChanged();
    }

    private void MoveCardFromDeckToHand(PlayerState player)
    {
        if (Deck.Any())
        {
            var card = Deck[0];
            Deck.RemoveAt(0);
            player.AddToHand(card);
        }
    }

    private void BuildDeck()
    {
        Deck.Clear();
        DiscardPile.Clear();

        // Distances
        AddCards(CardType.Distance, "25km", 25, 10);
        AddCards(CardType.Distance, "50km", 50, 10);
        AddCards(CardType.Distance, "75km", 75, 10);
        AddCards(CardType.Distance, "100km", 100, 12);
        AddCards(CardType.Distance, "200km", 200, 4);

        // Hazards
        AddCards(CardType.Hazard, CardNames.Stop, 0, 5);
        AddCards(CardType.Hazard, CardNames.OutOfGas, 0, 3);
        AddCards(CardType.Hazard, CardNames.FlatTire, 0, 3);
        AddCards(CardType.Hazard, CardNames.Accident, 0, 3);

        // Speed Cards
        AddCards(CardType.SpeedLimit, CardNames.SpeedLimit, 0, 4);
        AddCards(CardType.EndLimit, CardNames.EndLimit, 0, 6);

        // Remedies
        AddCards(CardType.Remedy, CardNames.Roll, 0, 14);
        AddCards(CardType.Remedy, CardNames.Gasoline, 0, 6);
        AddCards(CardType.Remedy, CardNames.SpareTire, 0, 6);
        AddCards(CardType.Remedy, CardNames.Repairs, 0, 6);

        // Safeties
        AddCards(CardType.Safety, CardNames.Safeties.DrivingAce, 0, 1);
        AddCards(CardType.Safety, CardNames.Safeties.ExtraTank, 0, 1);
        AddCards(CardType.Safety, CardNames.Safeties.PunctureProof, 0, 1);
        AddCards(CardType.Safety, CardNames.Safeties.RightOfWay, 0, 1);
    }

    private void AddCards(CardType type, string name, int val, int count)
    {
        for (int i = 0; i < count; i++)
            Deck.Add(new Card { Type = type, Name = name, Value = val });
    }

    public void ShuffleDeck()
    {
        Deck = Deck.OrderBy(_ => _rng.Next()).ToList();
    }

    public void ExecuteMove(Card card, PlayerState actor, PlayerState target)
    {
        actor.RemoveFromHand(card);

        // CHECK FOR COUP FOURRÉ (Safety in hand?)
        if (card.Type == CardType.Hazard || card.Type == CardType.SpeedLimit)
        {
            var safety = target.Hand.FirstOrDefault(s => s.ProtectsAgainst(card.Name));
            if (safety != null)
            {
                TriggerCoupFourreWindow(card, target);
                return; // Stop execution here to wait for reaction
            }
        }

        ResolveMove(card, actor, target);
    }

    private void TriggerCoupFourreWindow(Card hazard, PlayerState target)
    {
        PendingHazard = hazard;
        IsWaitingForCoupFourre = true;
        NotifyStateChanged();

        // If the AI is the one who can Coup Fourré, it does it instantly 
        // (or we can add a delay for realism)
        if (target == AI) // It's Player's turn, so AI is being attacked
        {
            _ = HandleAICoupFourre(target);
        }
    }

    private async Task HandleAICoupFourre(PlayerState ai)
    {
        // A slight delay so the human player actually sees the card they played 
        // before the AI "zaps" it away.
        await Task.Delay(1500);

        // AI will always play a Coup Fourré if it can. 
        // It's worth 300 points and steals the turn!
        PlayCoupFourre(ai);
    }

    public void PlayCoupFourre(PlayerState actor)
    {
        if (!IsWaitingForCoupFourre || PendingHazard == null) return;

        // Find the safety in hand
        var safety = actor.Hand.First(s => s.ProtectsAgainst(PendingHazard.Name));

        actor.RemoveFromHand(safety);
        actor.SafetyArea.Add(safety);
        actor.CoupFourreCount++;

        // Hazard is discarded instead of played
        DiscardPile.Add(PendingHazard);

        IsWaitingForCoupFourre = false;
        PendingHazard = null;


        // Rule: Coup Fourré grants a card draw and the turn switches to that player immediately
        if (Deck.Count > 0)
            MoveCardFromDeckToHand(actor);

        CurrentTurn = (actor == Player) ? TurnOwner.Player : TurnOwner.AI;

        if (Deck.Count > 0)
        {
            CurrentPhase = TurnPhase.Draw;
            DrawCard(actor);
        }
        else
        {
            CurrentPhase = TurnPhase.Play;
        }

        if (CurrentTurn == TurnOwner.AI) _ = _aiService.ThinkAndPlay(this);
        
        AddLog($"COUP FOURRÉ! {actor.Name} played a safety out of turn!", actor == Player ? TurnOwner.Player : TurnOwner.AI);
        NotifyStateChanged();
    }

    public void DeclineCoupFourre(PlayerState target)
    {
        if (PendingHazard == null) return;

        IsWaitingForCoupFourre = false;

        // Resolve the hazard normally
        var actor = (target == Player) ? AI : Player;
        ResolveMove(PendingHazard, actor, target);
        PendingHazard = null;
    }

    private void ResolveMove(Card card, PlayerState actor, PlayerState target)
    {
        switch (card.Type)
        {
            case CardType.Distance: actor.DistanceCards.Add(card); break;
            case CardType.Safety:
                actor.SafetyArea.Add(card);
                CurrentPhase = TurnPhase.Draw;
                HandleSafetyDiscards(actor, card);
                break;
            case CardType.Hazard: target.BattlePile.Add(card); break;
            case CardType.Remedy: actor.BattlePile.Add(card); break;

            case CardType.SpeedLimit: target.SpeedPile.Add(card); break;
            case CardType.EndLimit: actor.SpeedPile.Add(card); break;
        }

        if (card.Type != CardType.Safety) EndTurn();
        else if (CurrentTurn == TurnOwner.AI) _ = _aiService.ThinkAndPlay(this);

        NotifyStateChanged();
    }

    private void HandleSafetyDiscards(PlayerState actor, Card safetyCard)
    {
        // 1. Check the Battle Pile (Stop, Accident, Out of Gas, Flat Tire)
        if (actor.BattlePile.Any())
        {
            var topHazard = actor.BattlePile.Last();
            if (safetyCard.ProtectsAgainst(topHazard.Name))
            {
                // Remove the hazard and move to discard
                actor.BattlePile.RemoveAt(actor.BattlePile.Count - 1);
                DiscardPile.Add(topHazard);
            }
        }

        // 2. Check the Speed Pile (Specifically for Right of Way vs Speed Limit)
        // Note: ProtectsAgainst(CardNames.SpeedLimit) returns true for Right of Way
        if (actor.SpeedPile.Any())
        {
            var topSpeedCard = actor.SpeedPile.Last();
            if (safetyCard.ProtectsAgainst(topSpeedCard.Name))
            {
                actor.SpeedPile.RemoveAt(actor.SpeedPile.Count - 1);
                DiscardPile.Add(topSpeedCard);
            }
        }
    }

    public void DiscardCard(Card card, PlayerState actor)
    {
        if (CurrentPhase != TurnPhase.Play) return;
        actor.RemoveFromHand(card);
        DiscardPile.Add(card);
        EndTurn();
    }

    public void EndTurn()
    {
        CurrentTurn = (CurrentTurn == TurnOwner.Player) ? TurnOwner.AI : TurnOwner.Player;
        CurrentPhase = TurnPhase.Draw;

        if (Deck.Count == 0)
        {
            CurrentPhase = TurnPhase.Play;
        }
        else
        {
            CurrentPhase = TurnPhase.Draw;
        }

        CheckForEndGame();
        NotifyStateChanged();

        if (CurrentTurn == TurnOwner.AI && !GameEnded)
        {
            // Trigger AI Service asynchronously
            _ = _aiService.ThinkAndPlay(this);
        }
    }

    private void CheckForEndGame()
    {
        if ((Deck.Count == 0 && Player.Hand.Count == 0 && AI.Hand.Count == 0) ||
            Player.TotalDistance == 1000 || AI.TotalDistance == 1000)
        {
            GameEnded = true;
            NotifyStateChanged();
        }
    }

    public void CompleteMatch()
    {
        MatchEnded = true;
        NotifyStateChanged();
    }

    public int CalculateRoundScore(PlayerState player)
    {
        int score = 0;
        score += player.TotalDistance; // 1pt per KM
        score += player.SafetyArea.Count * 100; // 100 per safety
        score += player.CoupFourreCount * 300; // 300 per Coup Fourré

        if (player.SafetyArea.Count == 4) score += 300; // All 4 safeties bonus

        if (player.TotalDistance == 1000)
        {
            score += 400; // Completed trip
            if (Deck.Count == 0) score += 300; // Delayed Action (Deck empty) bonus

            if (!player.DistanceCards.Any(dc => dc.Name == "200km")) score += 300; // Safe Trip bonus (no 200km cards)

            var opponent = (player == Player) ? AI : Player;
            if (opponent.TotalDistance == 0) score += 500; // Shutout bonus
        }

        return score;
    }

    public void ResetMatchStarter()
    {
        _lastRoundStarter = null;
    }

    public void AddLog(string message, TurnOwner owner)
    {
        Logs.Add(new LogEntry(message, DateTime.Now, owner));
        // Keep only the last 50 entries to prevent memory bloat
        if (Logs.Count > 50) Logs.RemoveAt(0);
        NotifyStateChanged();
    }
}
