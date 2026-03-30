using System;
using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }

    [SerializeField] private int currentGold;

    public int CurrentGold => currentGold;

    public event Action<int> OnGoldChanged;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        currentGold += amount;
        OnGoldChanged?.Invoke(currentGold);
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0 || currentGold < amount) return false;
        currentGold -= amount;
        OnGoldChanged?.Invoke(currentGold);
        return true;
    }
}
