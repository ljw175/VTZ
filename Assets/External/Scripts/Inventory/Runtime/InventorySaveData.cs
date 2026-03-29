using System;
using System.Collections.Generic;

[Serializable]
public class InventorySaveData
{
    public List<ContainerSaveData> containers = new List<ContainerSaveData>();
}

[Serializable]
public class ContainerSaveData
{
    public string containerId;
    public int width;
    public int height;
    public List<ItemSaveData> items = new List<ItemSaveData>();
}

[Serializable]
public class ItemSaveData
{
    public string definitionId;
    public string instanceId;
    public float currentFreshness;
    public int gridX;
    public int gridY;
    public int rotationIndex;
}
