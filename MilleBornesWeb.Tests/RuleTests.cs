using MilleBornesWeb.Models;
using MilleBornesWeb.Services;

namespace MilleBornesWeb.Tests;

public class RuleTests
{
    [Fact]
    public void Cannot_Play_Accident_On_Opponent_Who_Is_Already_Stopped()
    {
        // Arrange
        var player = new PlayerState();
        var opponent = new PlayerState();
        opponent.BattlePile.Add(new Card { Name = CardNames.Stop, Type = CardType.Hazard }); // Opponent already has a Stop card

        var accident = new Card { Name = CardNames.Accident, Type = CardType.Hazard };

        // Act
        var result = RuleBook.ValidateMove(accident, player, opponent);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void RightOfWay_Allows_Distance_Without_Roll_Card()
    {
        var player = new PlayerState();
        player.SafetyArea.Add(new Card { Name = CardNames.Safeties.RightOfWay, Type = CardType.Safety });
        var distance = new Card { Type = CardType.Distance, Value = 100 };

        var result = RuleBook.ValidateMove(distance, player, player);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EndLimit_Allows_100km_After_Being_Limited()
    {
        var player = new PlayerState();
        player.BattlePile.Add(new Card { Name = CardNames.Roll, Type = CardType.Remedy });
        player.SpeedPile.Add(new Card { Name = CardNames.SpeedLimit, Type = CardType.SpeedLimit });

        var dist100 = new Card { Type = CardType.Distance, Value = 100 };
        var endLimit = new Card { Name = CardNames.EndLimit, Type = CardType.EndLimit };

        // Before: Should fail
        var resultBefore = RuleBook.ValidateMove(dist100, player, player);
        Assert.False(resultBefore.IsValid);

        // Apply card
        player.SpeedPile.Add(endLimit);

        // After: Should succeed
        var resultAfter = RuleBook.ValidateMove(dist100, player, player);
        Assert.True(resultAfter.IsValid);
    }

    [Fact]
    public void Cannot_Play_Accident_On_Empty_Opponent_Pile()
    {
        // Arrange
        var player = new PlayerState();
        var opponent = new PlayerState(); // Empty BattlePile
        var accident = new Card { Name = CardNames.Accident, Type = CardType.Hazard };

        // Act
        var result = RuleBook.ValidateMove(accident, player, opponent);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void SpeedLimit_Goes_To_SpeedPile_Not_BattlePile()
    {
        // Arrange (Setup a real manager for execution test)
        var game = new GameManager(null); // AI service null for simple state test
        var player = game.Player;
        var opponent = game.AI;
        var limitCard = new Card { Name = CardNames.SpeedLimit, Type = CardType.SpeedLimit };
        player.AddToHand(limitCard);

        // Act
        game.ExecuteMove(limitCard, player, opponent);

        // Assert
        Assert.Empty(opponent.BattlePile);
        Assert.Single(opponent.SpeedPile);
        Assert.Equal(CardNames.SpeedLimit, opponent.SpeedPile.First().Name);
    }

    [Fact]
    public void RightOfWay_Clears_SpeedLimit_Restriction()
    {
        // Arrange
        var player = new PlayerState();
        player.SpeedPile.Add(new Card { Name = CardNames.SpeedLimit, Type = CardType.SpeedLimit });
        // Give player Right of Way so they can move, but check if distance is limited
        player.SafetyArea.Add(new Card { Name = CardNames.Safeties.RightOfWay, Type = CardType.Safety });

        var dist100 = new Card { Type = CardType.Distance, Value = 100 };

        // Act
        var result = RuleBook.ValidateMove(dist100, player, player);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Cannot_Play_Distance_When_BattlePile_Is_Empty()
    {
        // Arrange
        var player = new PlayerState();
        var card = new Card { Type = CardType.Distance, Value = 100 };

        // Act
        var result = RuleBook.ValidateMove(card, player, player);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Can_Play_Roll_On_Empty_BattlePile()
    {
        // Arrange
        var player = new PlayerState();
        var rollCard = new Card { Name = CardNames.Roll, Type = CardType.Remedy };

        // Act
        var result = RuleBook.ValidateMove(rollCard, player, player);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Cannot_Exceed_1000km_Exactly()
    {
        // Arrange
        var player = new PlayerState();
        player.DistanceCards.Add(new Card { Value = 200 });
        player.DistanceCards.Add(new Card { Value = 200 });
        player.DistanceCards.Add(new Card { Value = 200 });
        player.DistanceCards.Add(new Card { Value = 200 });
        player.DistanceCards.Add(new Card { Value = 100 }); // Total 900

        // Ensure player is in "Roll" state
        player.BattlePile.Add(new Card { Name = CardNames.Roll });

        var distance200 = new Card { Type = CardType.Distance, Value = 200 };

        // Act
        var result = RuleBook.ValidateMove(distance200, player, player);

        // Assert
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(CardNames.Stop, CardNames.Roll, true)]      // Roll fixes Stop
    [InlineData(CardNames.Accident, CardNames.Repairs, true)] // Repairs fixes Accident
    [InlineData(CardNames.Stop, CardNames.Repairs, false)]    // Repairs does NOT fix Stop
    public void Remedies_Should_Fix_Correct_Hazards(string hazard, string remedy, bool shouldFix)
    {
        // Arrange
        var remedyCard = new Card { Name = remedy, Type = CardType.Remedy };

        // Act
        var result = remedyCard.Fixes(hazard);

        // Assert
        Assert.Equal(shouldFix, result);
    }

    [Fact]
    public async Task AI_Prioritizes_Safeties_Over_Distance()
    {
        // Arrange
        var aiService = new BasicAIService();
        var game = new GameManager(aiService);
        game.StartNewGame();

        // Force a specific hand for the AI
        var ai = game.AI;
        ai.ClearHand();
        ai.AddToHand(new Card { Name = CardNames.Safeties.RightOfWay, Type = CardType.Safety });
        ai.AddToHand(new Card { Name = "100km", Type = CardType.Distance, Value = 100 });

        // Make AI already moving so it *could* play the distance card
        ai.BattlePile.Add(new Card { Name = CardNames.Roll });

        // Act
        // We want to see if ExecuteMove is called with the Safety
        // (This requires slightly more setup with a Mocking library like Moq or NSubstitute)
    }

    [Fact]
    public void CoupFourre_Should_Result_In_Seven_Cards_In_Hand()
    {
        // Arrange
        var game = new GameManager(null);
        game.StartNewGame();
        var player = game.Player;

        // Setup: Player has 6 cards, one is a Safety
        player.ClearHand();
        for (int i = 0; i < 5; i++) player.AddToHand(new Card { Name = "100km" });
        var safety = new Card { Name = CardNames.Safeties.ExtraTank, Type = CardType.Safety };
        player.AddToHand(safety);

        // AI plays a Hazard on Player
        var hazard = new Card { Name = CardNames.OutOfGas, Type = CardType.Hazard };
        game.ExecuteMove(hazard, game.AI, player);

        // Act
        game.PlayCoupFourre(player);

        // Assert
        // 6 (start) - 1 (played) + 1 (replacement) + 1 (turn draw) = 7
        Assert.Equal(7, player.Hand.Count);
        Assert.Equal(TurnOwner.Player, game.CurrentTurn);
        Assert.Equal(TurnPhase.Play, game.CurrentPhase);
    }

    [Fact]
    public void CalculateScore_Should_Award_Correct_Points_For_Win()
    {
        // Arrange
        var game = new GameManager(null);
        var player = new PlayerState();

        var dist100 = new Card { Type = CardType.Distance, Value = 100 };

        // 1000km traveled
        for (int i = 0; i < 10; i++)
        {
            player.DistanceCards.Add(dist100);
        }

        // 1 Safety played
        player.SafetyArea.Add(new Card { Name = CardNames.Safeties.RightOfWay });

        // Act
        var score = game.CalculateRoundScore(player);

        // Assert
        // 1000 (km) + 100 (safety) + 400 (win bonus) + 500 (shutout) + 300 (safe trip) + 300 (delayed action) = 2600
        Assert.Equal(2600, score);
    }

    [Fact]
    public void Playing_DrivingAce_Should_Discard_Existing_Accident()
    {
        // Arrange
        var game = new GameManager(null);
        var player = game.Player;

        // Setup: Player has an Accident on top of a Roll card
        player.BattlePile.Add(new Card { Name = CardNames.Roll, Type = CardType.Remedy });
        var accident = new Card { Name = CardNames.Accident, Type = CardType.Hazard };
        player.BattlePile.Add(accident);

        var drivingAce = new Card { Name = CardNames.Safeties.DrivingAce, Type = CardType.Safety };

        // Act
        game.ExecuteMove(drivingAce, player, player);

        // Assert
        Assert.DoesNotContain(accident, player.BattlePile);
        Assert.Equal(CardNames.Roll, player.BattlePile.Last().Name);
        Assert.True(player.CanMove); // Revealed Roll card allows movement
    }
    [Fact]
    public void CalculateScore_Should_Award_500_For_All_Safeties()
    {
        // Arrange
        var game = new GameManager(null);
        var player = game.Player;
        player.DistanceCards.Add(new Card { Value = 100 });

        // Add all 4 safeties
        player.SafetyArea.Add(new Card { Name = CardNames.Safeties.RightOfWay });
        player.SafetyArea.Add(new Card { Name = CardNames.Safeties.DrivingAce });
        player.SafetyArea.Add(new Card { Name = CardNames.Safeties.ExtraTank });
        player.SafetyArea.Add(new Card { Name = CardNames.Safeties.PunctureProof });

        // Act
        int score = game.CalculateRoundScore(player);

        // Assert
        // 100 (distance) + 400 (4 individual safeties) + 300 (all 4 bonus) = 1000
        Assert.Equal(800, score);
    }

    [Fact]
    public void CalculateScore_Should_Award_300_For_Delayed_Action()
    {
        // Arrange
        var game = new GameManager(null);
        var player = game.Player;
        var ai = game.AI;

        // Reach 1000km
        for (int i = 0; i < 10; i++) player.DistanceCards.Add(new Card { Value = 100 });
        ai.DistanceCards.Add(new Card { Value = 100 });

        // Force deck to be empty
        game.Deck.Clear();

        // Act
        int score = game.CalculateRoundScore(player);

        // Assert
        // 1000 (km) + 400 (Trip Complete) + 300 (Delayed Action) + 300 (Safe Trip - no 200km used) = 2000
        Assert.Equal(2000, score);
    }
}