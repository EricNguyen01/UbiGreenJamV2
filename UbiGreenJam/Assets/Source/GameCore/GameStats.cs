using System;
[Serializable]
public class GameStats
{
    public int score;
    public int playerLives;
    public int wavesCompleted;

    public GameStats()
    {
        score = 0;
        playerLives = 3;
        wavesCompleted = 0;
    }
}
