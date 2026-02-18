namespace MilleBornesWeb.Services;

public interface IAIService
{
    Task ThinkAndPlay(GameManager game);
}
