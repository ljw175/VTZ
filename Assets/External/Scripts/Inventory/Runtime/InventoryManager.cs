using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Grid Definitions")]
    [SerializeField] private InventoryGridDefinition shipCargoGridDef;
    [SerializeField] private InventoryGridDefinition playerBackpackGridDef;

    [Header("Item Registry")]
    [SerializeField] private ItemDefinition[] allItemDefinitions;

    private InventoryContainer shipCargoContainer;
    private InventoryContainer playerBackpackContainer;
    private Dictionary<string, InventoryContainer> portStorageContainers = new Dictionary<string, InventoryContainer>();

    private ShipRuntimeState shipRuntimeState;

    public event Action OnInventoryChanged;

    private string SaveFilePath => Path.Combine(Application.persistentDataPath, "inventory.json");

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        ItemDatabase.Initialize(allItemDefinitions);
        CreateContainers();
    }

    private void Start()
    {
        // ShipController에서 RuntimeState 획득
        var shipController = FindObjectOfType<ShipController>();
        if (shipController != null)
            shipRuntimeState = shipController.RuntimeState;

        if (GameTimer.Instance != null)
            GameTimer.Instance.OnDayChanged += HandleDayChanged;

        Load();
    }

    private void OnDestroy()
    {
        if (GameTimer.Instance != null)
            GameTimer.Instance.OnDayChanged -= HandleDayChanged;
    }

    private void CreateContainers()
    {
        if (shipCargoGridDef != null)
        {
            shipCargoContainer = new InventoryContainer(
                shipCargoGridDef,
                () => shipRuntimeState != null ? shipRuntimeState.GetStat(ShipStatType.CargoCapacity) : -1f
            );
            shipCargoContainer.OnContainerChanged += () => OnInventoryChanged?.Invoke();
        }

        if (playerBackpackGridDef != null)
        {
            playerBackpackContainer = new InventoryContainer(
                playerBackpackGridDef,
                () => 20f
            );
            playerBackpackContainer.OnContainerChanged += () => OnInventoryChanged?.Invoke();
        }
    }

    // --- Public API ---

    public IInventoryContainer GetShipCargo() => shipCargoContainer;
    public IInventoryContainer GetPlayerBackpack() => playerBackpackContainer;

    public IInventoryContainer GetOrCreatePortStorage(string portId, InventoryGridDefinition gridDef)
    {
        if (portStorageContainers.TryGetValue(portId, out var existing))
            return existing;

        var container = new InventoryContainer(gridDef, () => -1f);
        container.OnContainerChanged += () => OnInventoryChanged?.Invoke();
        portStorageContainers[portId] = container;
        return container;
    }

    public bool TransferItem(ItemInstance item, IInventoryContainer source, IInventoryContainer target)
    {
        if (item == null || source == null || target == null) return false;
        if (!target.HasWeightCapacity(item.Definition.weight)) return false;

        if (!source.RemoveItem(item)) return false;

        if (target.TryAddItem(item))
            return true;

        // 이동 실패 시 원래 컨테이너에 복원
        source.TryAddItem(item);
        return false;
    }

    public List<IInventoryContainer> GetAllContainers()
    {
        return new IInventoryContainer[] { shipCargoContainer, playerBackpackContainer }
            .Where(c => c != null)
            .Concat(portStorageContainers.Values)
            .ToList();
    }

    // --- 신선도 ---

    private void HandleDayChanged(int day)
    {
        foreach (var item in GetAllContainers().SelectMany(c => c.GridState.PlacedItems))
            item.TickFreshness();
    }

    // --- 저장/불러오기 ---

    public void Save()
    {
        var saveData = new InventorySaveData
        {
            containers = GetAllContainers().Select(c => new ContainerSaveData
            {
                gridId = c.ContainerId,
                width = c.GridState.Width,
                height = c.GridState.Height,
                items = c.GridState.PlacedItems.Select(item => new ItemSaveData
                {
                    definitionId = item.DefinitionId,
                    instanceId = item.InstanceId,
                    currentFreshness = item.CurrentFreshness,
                    gridX = item.GridX,
                    gridY = item.GridY,
                    rotationIndex = item.RotationIndex,
                }).ToList()
            }).ToList()
        };

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(SaveFilePath, json);
    }

    public void Load()
    {
        if (!File.Exists(SaveFilePath)) return;

        string json = File.ReadAllText(SaveFilePath);
        var saveData = JsonUtility.FromJson<InventorySaveData>(json);
        if (saveData == null) return;

        for (int i = 0; i < saveData.containers.Count; i++)
        {
            var containerData = saveData.containers[i];
            IInventoryContainer container = FindContainerByGridId(containerData.gridId);
            if (container == null) continue;

            for (int j = 0; j < containerData.items.Count; j++)
            {
                var itemData = containerData.items[j];
                var def = ItemDatabase.GetDefinition(itemData.definitionId);
                if (def == null) continue;

                var item = new ItemInstance(def);
                item.InstanceId = itemData.instanceId;
                item.CurrentFreshness = itemData.currentFreshness;

                container.GridState.TryPlace(item, itemData.gridX, itemData.gridY, itemData.rotationIndex);
            }
        }
    }

    private IInventoryContainer FindContainerByGridId(string gridId)
    {
        if (shipCargoContainer != null && shipCargoContainer.ContainerId == gridId)
            return shipCargoContainer;
        if (playerBackpackContainer != null && playerBackpackContainer.ContainerId == gridId)
            return playerBackpackContainer;
        if (portStorageContainers.TryGetValue(gridId, out var port))
            return port;
        return null;
    }
}
