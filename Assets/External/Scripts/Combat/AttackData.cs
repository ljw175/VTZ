using UnityEngine;

public class AttackData : MonoBehaviour
{
    public int Damage { get; set; }
    public bool IsPlayerAttack { get; set; } = true; // 피아 식별용 플래그 추가
}