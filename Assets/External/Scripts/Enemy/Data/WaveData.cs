using System;
using UnityEngine;

[Serializable]
public struct enemyData
{
    public GameObject enemyPrefab;
    public int enemyCount;
    public float spawnStartTime;
}

[CreateAssetMenu(fileName = "New Wave Data", menuName = "ScriptableObjects/WaveData", order = 1)]
public class WaveData : ScriptableObject
{
    public enemyData[] enemies;
    public bool isBossWave;
}
