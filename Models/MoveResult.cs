namespace MilleBornesWeb.Models;

public record MoveResult(bool IsValid, string Message = "")
{
    public static MoveResult Success() => new(true);
    public static MoveResult Failure(string message) => new(false, message);

    // This allows you to write "if (result)" instead of "if (result.IsValid)"
    public static implicit operator bool(MoveResult result) => result.IsValid;
}