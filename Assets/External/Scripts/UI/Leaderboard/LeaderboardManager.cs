using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance;

    private string filePath;

    public LeaderboardData leaderboard = new LeaderboardData();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        filePath = Path.Combine(Application.persistentDataPath, "leaderboard.json");
        Load();
    }

    public void AddScore(string playerName, float clearTime)
    {
        LeaderboardEntry entry = new LeaderboardEntry(playerName, clearTime);

        leaderboard.entries.Add(entry);

        SortLeaderboard();

        UpdateRanks();

        // LimitEntries(10);

        Save();
    }

    void SortLeaderboard()
    {
        leaderboard.entries.Sort((a, b) => a.clearTime.CompareTo(b.clearTime));
    }

    void UpdateRanks()
    {
        for (int i = 0; i < leaderboard.entries.Count; i++)
        {
            leaderboard.entries[i].rank = i + 1;
        }
    }

    void LimitEntries(int max)
    {
        if (leaderboard.entries.Count > max)
        {
            leaderboard.entries.RemoveRange(max, leaderboard.entries.Count - max);
        }
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(leaderboard, true);

        File.WriteAllText(filePath, json);
    }

    public void Load()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);

            leaderboard = JsonUtility.FromJson<LeaderboardData>(json);

            // ★ 로드된 데이터 디버그
            Debug.Log("Loaded Leaderboard:");
            if (leaderboard.entries.Count == 0)
            {
                Debug.Log("Empty leaderboard");
            }
            else
            {
                for (int i = 0; i < leaderboard.entries.Count; i++)
                {
                    var entry = leaderboard.entries[i];
                    Debug.Log($"{i + 1}. Name: {entry.playerName}, Time: {entry.clearTime}, Rank: {entry.rank}");
                }
            }
        }
        else
        {
            leaderboard = new LeaderboardData();
            Debug.Log("Leaderboard file not found, initialized empty.");
        }
    }

    public List<LeaderboardEntry> GetLeaderboard()
    {
        return leaderboard.entries;
    }
}