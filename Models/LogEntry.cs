using MilleBornesWeb.Services;

namespace MilleBornesWeb.Models;

public record LogEntry(string Message, DateTime Timestamp, TurnOwner Owner);
