[System.Flags]
public enum ItemCategory
{
    None       = 0,
    Food       = 1 << 0,
    Weapon     = 1 << 1,
    Clothing   = 1 << 2,
    Material   = 1 << 3,
    Tool       = 1 << 4,
    Medicine   = 1 << 5,
    Treasure   = 1 << 6,
    ShipSupply = 1 << 7,
    Quest      = 1 << 8,
}

[System.Flags]
public enum ItemTag
{
    None      = 0,
    Logging   = 1 << 0,
    Gathering = 1 << 1,
    Fishing   = 1 << 2,
    Driftage   = 1 << 3,
    Treasure  = 1 << 4,
    Crafted   = 1 << 5,
    Purchased = 1 << 6,
    Looted    = 1 << 7,
    Salvaged  = 1 << 8,
}

public enum FreshnessState
{
    Fresh,
    Okay,
    Aging,
    Rotten,
}

public enum InventoryContainerType
{
    ShipCargo,
    PlayerBackpack,
    PortStorage,
    Loot,
}
