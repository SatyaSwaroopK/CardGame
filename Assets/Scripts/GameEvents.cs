using System;

public static class GameEvents
{
    public static event Action<int>
        TurnStarted;

    public static event Action<int>
        PlayerEndedTurn;

    public static event Action<int, int>
        CardRevealed;

    public static event Action<int, int>
        ScoreUpdated;

    public static event Action<int>
        TurnEnded;

    public static event Action<int>
        GameEnded;


    public static void RaiseTurnStarted(
        int round)
    {
        TurnStarted?.Invoke(round);
    }


    public static void RaisePlayerEndedTurn(
        int playerId)
    {
        PlayerEndedTurn?.Invoke(
            playerId
        );
    }


    public static void RaiseCardRevealed(
        int playerId,
        int cardId)
    {
        CardRevealed?.Invoke(
            playerId,
            cardId
        );
    }


    public static void RaiseScoreUpdated(
        int playerId,
        int score)
    {
        ScoreUpdated?.Invoke(
            playerId,
            score
        );
    }


    public static void RaiseTurnEnded(
        int round)
    {
        TurnEnded?.Invoke(
            round
        );
    }


    public static void RaiseGameEnded(
        int winnerId)
    {
        GameEnded?.Invoke(
            winnerId
        );
    }
}