using UnityEngine;

public abstract class LootableObject : MonoBehaviour, ILootable
{
    [Header("Interaction")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private GameObject promptUI;

    private CircleCollider2D interactionTrigger;
    private bool playerInRange;

    public abstract string PromptText { get; }
    public abstract bool CanLoot { get; }

    protected virtual void Awake()
    {
        interactionTrigger = GetComponent<CircleCollider2D>();
        if (interactionTrigger == null)
            interactionTrigger = gameObject.AddComponent<CircleCollider2D>();
        interactionTrigger.isTrigger = true;
        interactionTrigger.radius = interactionRange;

        if (promptUI != null)
            promptUI.SetActive(false);
    }

    private void Update()
    {
        if (playerInRange && CanLoot && InputManager.Instance.Interact.WasPressedThisFrame())
            Loot();
    }

    public void Loot()
    {
        if (!CanLoot) return;
        OnLoot();
    }

    protected abstract void OnLoot();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        if (promptUI != null)
            promptUI.SetActive(true);
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        if (promptUI != null)
            promptUI.SetActive(false);
    }
}
