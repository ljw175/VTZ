using UnityEngine;

[System.Serializable]
public class DockFacilityConfig
{
    [Tooltip("화물 검문 존재 여부")]
    public bool hasCargoInspection;

    [Tooltip("이 항구에서 압수하는 물품 목록")]
    public ItemDefinition[] contrabandItems;
}
