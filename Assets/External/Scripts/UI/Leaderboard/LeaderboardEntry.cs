using System;

[Serializable]
public class LeaderboardEntry
{
    public int rank;
    public string playerName;
    public float clearTime;

    public LeaderboardEntry(string name, float time)
    {
        playerName = name;
        clearTime = time;
    }
}