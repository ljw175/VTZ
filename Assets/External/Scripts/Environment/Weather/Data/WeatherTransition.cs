using UnityEngine;

[System.Serializable]
public struct WeatherTransition
{
    public WeatherStateDefinition targetState;
    [Range(0f, 1f)] public float probability;
}
