using System;
using UnityEngine;

[Serializable]
public struct DriftageSpawnEntry
{
    public GameObject driftagePrefab;
    public DriftageDefinition definition;
    public int count;
}

[CreateAssetMenu(fileName = "New WaveLootData", menuName = "ScriptableObjects/WaveLootData")]
public class WaveLootData : ScriptableObject
{
    public DriftageSpawnEntry[] driftageSpawns;
}
