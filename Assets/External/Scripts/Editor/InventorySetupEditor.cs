using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

public static class InventorySetupEditor
{
    private const string BasePath = "Assets/External/ScriptableObjects/Inventory";
    private const string PrefabPath = "Assets/External/Prefabs";

    [MenuItem("Tools/Inventory/Create Grid Definitions")]
    public static void CreateGridDefinitions()
    {
        if (!Directory.Exists(BasePath))
            Directory.CreateDirectory(BasePath);

        CreateGridDef("ShipCargo", "ShipCargo", "선박 화물칸",
            InventoryContainerType.ShipCargo, 8, 6);

        CreateGridDef("PlayerBackpack", "PlayerBackpack", "플레이어 배낭",
            InventoryContainerType.PlayerBackpack, 5, 4);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[InventorySetup] Grid Definition 에셋 생성 완료: " + BasePath);
    }

    [MenuItem("Tools/Inventory/Create Grid Popup Prefab")]
    public static void CreateGridPopupPrefab()
    {
        string path = $"{PrefabPath}/GridPopupPrefab.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log($"[InventorySetup] 이미 존재: {path}");
            return;
        }

        // Root
        GameObject root = new GameObject("GridPopupWindow");
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(600, 500);
        root.AddComponent<CanvasRenderer>();
        var rootImage = root.AddComponent<Image>();
        rootImage.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);
        rootImage.raycastTarget = true;

        var draggable = root.AddComponent<DraggableWindow>();
        var popup = root.AddComponent<InventoryGridPopupUI>();

        // TitleBar
        GameObject titleBar = CreateChild("TitleBar", root.transform);
        RectTransform titleBarRt = titleBar.GetComponent<RectTransform>();
        titleBarRt.anchorMin = new Vector2(0, 1);
        titleBarRt.anchorMax = new Vector2(1, 1);
        titleBarRt.pivot = new Vector2(0.5f, 1);
        titleBarRt.anchoredPosition = Vector2.zero;
        titleBarRt.sizeDelta = new Vector2(0, 32);
        var titleBarImage = titleBar.AddComponent<Image>();
        titleBarImage.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        titleBarImage.raycastTarget = true;

        // TitleText
        GameObject titleTextObj = new GameObject("TitleText");
        titleTextObj.transform.SetParent(titleBar.transform, false);
        RectTransform titleTextRt = titleTextObj.AddComponent<RectTransform>();
        titleTextRt.anchorMin = new Vector2(0, 0);
        titleTextRt.anchorMax = new Vector2(1, 1);
        titleTextRt.offsetMin = new Vector2(8, 0);
        titleTextRt.offsetMax = new Vector2(-40, 0);
        var titleTmp = titleTextObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "Container";
        titleTmp.fontSize = 16;
        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
        titleTmp.color = Color.white;
        titleTmp.raycastTarget = false;

        // Close Button
        GameObject closeBtn = new GameObject("CloseButton");
        closeBtn.transform.SetParent(titleBar.transform, false);
        RectTransform closeBtnRt = closeBtn.AddComponent<RectTransform>();
        closeBtnRt.anchorMin = new Vector2(1, 0);
        closeBtnRt.anchorMax = new Vector2(1, 1);
        closeBtnRt.pivot = new Vector2(1, 0.5f);
        closeBtnRt.anchoredPosition = Vector2.zero;
        closeBtnRt.sizeDelta = new Vector2(32, 0);
        var closeBtnImage = closeBtn.AddComponent<Image>();
        closeBtnImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        var button = closeBtn.AddComponent<Button>();
        button.targetGraphic = closeBtnImage;

        GameObject closeText = new GameObject("Text");
        closeText.transform.SetParent(closeBtn.transform, false);
        RectTransform closeTextRt = closeText.AddComponent<RectTransform>();
        closeTextRt.anchorMin = Vector2.zero;
        closeTextRt.anchorMax = Vector2.one;
        closeTextRt.offsetMin = Vector2.zero;
        closeTextRt.offsetMax = Vector2.zero;
        var closeTmp = closeText.AddComponent<TextMeshProUGUI>();
        closeTmp.text = "X";
        closeTmp.fontSize = 16;
        closeTmp.alignment = TextAlignmentOptions.Center;
        closeTmp.color = Color.white;
        closeTmp.raycastTarget = false;

        // Grid Container
        GameObject gridContainer = CreateChild("GridContainer", root.transform);
        RectTransform gridRt = gridContainer.GetComponent<RectTransform>();
        gridRt.anchorMin = new Vector2(0, 0);
        gridRt.anchorMax = new Vector2(1, 1);
        gridRt.offsetMin = new Vector2(8, 40);   // padding + footer
        gridRt.offsetMax = new Vector2(-8, -40);  // padding + titlebar

        var gridUI = gridContainer.AddComponent<InventoryGridUI>();

        // Assign cell/item prefabs if they exist
        var cellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/CellPrefab.prefab");
        var itemUIPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/ItemUIPrefab.prefab");
        if (cellPrefab != null)
            SetSerializedField(gridUI, "cellPrefab", cellPrefab);
        if (itemUIPrefab != null)
            SetSerializedField(gridUI, "itemUIPrefab", itemUIPrefab);

        // Footer
        GameObject footer = CreateChild("Footer", root.transform);
        RectTransform footerRt = footer.GetComponent<RectTransform>();
        footerRt.anchorMin = new Vector2(0, 0);
        footerRt.anchorMax = new Vector2(1, 0);
        footerRt.pivot = new Vector2(0.5f, 0);
        footerRt.anchoredPosition = Vector2.zero;
        footerRt.sizeDelta = new Vector2(0, 32);

        // Weight Text
        GameObject weightObj = new GameObject("WeightText");
        weightObj.transform.SetParent(footer.transform, false);
        RectTransform weightRt = weightObj.AddComponent<RectTransform>();
        weightRt.anchorMin = new Vector2(0, 0);
        weightRt.anchorMax = new Vector2(0.6f, 1);
        weightRt.offsetMin = new Vector2(8, 0);
        weightRt.offsetMax = Vector2.zero;
        var weightTmp = weightObj.AddComponent<TextMeshProUGUI>();
        weightTmp.text = "0.0 / 0.0 kg";
        weightTmp.fontSize = 14;
        weightTmp.alignment = TextAlignmentOptions.MidlineLeft;
        weightTmp.color = Color.white;
        weightTmp.raycastTarget = false;

        // AutoSort Button
        GameObject sortBtn = new GameObject("AutoSortButton");
        sortBtn.transform.SetParent(footer.transform, false);
        RectTransform sortBtnRt = sortBtn.AddComponent<RectTransform>();
        sortBtnRt.anchorMin = new Vector2(0.6f, 0);
        sortBtnRt.anchorMax = new Vector2(1, 1);
        sortBtnRt.offsetMin = new Vector2(4, 2);
        sortBtnRt.offsetMax = new Vector2(-8, -2);
        var sortBtnImage = sortBtn.AddComponent<Image>();
        sortBtnImage.color = new Color(0.3f, 0.3f, 0.35f, 1f);
        var sortButton = sortBtn.AddComponent<Button>();
        sortButton.targetGraphic = sortBtnImage;

        GameObject sortText = new GameObject("Text");
        sortText.transform.SetParent(sortBtn.transform, false);
        RectTransform sortTextRt = sortText.AddComponent<RectTransform>();
        sortTextRt.anchorMin = Vector2.zero;
        sortTextRt.anchorMax = Vector2.one;
        sortTextRt.offsetMin = Vector2.zero;
        sortTextRt.offsetMax = Vector2.zero;
        var sortTmp = sortText.AddComponent<TextMeshProUGUI>();
        sortTmp.text = "Auto Sort";
        sortTmp.fontSize = 14;
        sortTmp.alignment = TextAlignmentOptions.Center;
        sortTmp.color = Color.white;
        sortTmp.raycastTarget = false;

        // Wire serialized references
        SetSerializedField(draggable, "titleBar", titleBarRt);
        SetSerializedField(draggable, "closeButton", button);
        SetSerializedField(popup, "window", draggable);
        SetSerializedField(popup, "gridUI", gridUI);
        SetSerializedField(popup, "autoSortButton", sortButton);
        SetSerializedField(popup, "titleText", titleTmp);
        SetSerializedField(popup, "weightText", weightTmp);

        // Save prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        Debug.Log($"[InventorySetup] GridPopup 프리팹 생성 완료: {path}");
    }

    private static void CreateGridDef(string fileName, string gridId, string displayName,
        InventoryContainerType type, int width, int height)
    {
        string path = $"{BasePath}/{fileName}.asset";
        if (AssetDatabase.LoadAssetAtPath<InventoryGridDefinition>(path) != null)
        {
            Debug.Log($"[InventorySetup] 이미 존재: {path}");
            return;
        }

        var def = ScriptableObject.CreateInstance<InventoryGridDefinition>();
        def.gridId = gridId;
        def.displayName = displayName;
        def.containerType = type;
        def.width = width;
        def.height = height;

        AssetDatabase.CreateAsset(def, path);
        Debug.Log($"[InventorySetup] 생성됨: {path} ({width}x{height})");
    }

    private static GameObject CreateChild(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private static void SetSerializedField(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
