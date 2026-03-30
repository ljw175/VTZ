using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class CargoInspectionHandler : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultText;

    public void Execute(DockFacilityConfig config, Action onComplete)
    {
        if (config == null || !config.hasCargoInspection)
        {
            onComplete?.Invoke();
            return;
        }

        var cargo = InventoryManager.Instance?.GetShipCargo();
        if (cargo == null)
        {
            onComplete?.Invoke();
            return;
        }

        // 압수 물품 탐색
        var contrabandSet = new HashSet<ItemDefinition>(
            config.contrabandItems != null ? config.contrabandItems : Array.Empty<ItemDefinition>()
        );

        var confiscated = new List<ItemInstance>();
        // PlacedItems를 복사하여 순회 (순회 중 제거 방지)
        var items = cargo.GridState.PlacedItems.ToList();

        foreach (var item in items)
        {
            if (item.Definition != null && contrabandSet.Contains(item.Definition))
            {
                confiscated.Add(item);
                cargo.RemoveItem(item);
            }
        }

        // 결과 표시
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);

            if (resultText != null)
            {
                if (confiscated.Count > 0)
                {
                    var names = confiscated.Select(i => i.Definition.itemName);
                    resultText.text = $"압수된 물품 ({confiscated.Count}건):\n{string.Join("\n", names)}";
                }
                else
                {
                    resultText.text = "검문 완료: 이상 없음.";
                }
            }
        }

        onComplete?.Invoke();
    }

    /// <summary>
    /// 결과 패널 닫기 (버튼에 연결)
    /// </summary>
    public void CloseResultPanel()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }
}
