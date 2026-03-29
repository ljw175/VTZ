public interface ILootable
{
    string PromptText { get; }
    bool CanLoot { get; }
    void Loot();
}
