using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RaycastShooting))]
public sealed class WeaponModifierHudUI : MonoBehaviour
{
    [SerializeField] RaycastShooting shooting;
    [SerializeField] Vector2 anchorMin = new Vector2(0.72f, 0.7f);
    [SerializeField] Vector2 anchorMax = new Vector2(0.98f, 0.94f);
    [SerializeField] Color panelColor = new Color(0.05f, 0.07f, 0.1f, 0.82f);
    [SerializeField] Color rowColor = new Color(0.16f, 0.2f, 0.28f, 0.9f);
    [SerializeField] Color textColor = new Color(0.92f, 0.96f, 1f, 1f);
    [SerializeField] int fontSize = 20;

    readonly Dictionary<WeaponModifierRuntimeInstance, Text> _rows = new Dictionary<WeaponModifierRuntimeInstance, Text>();
    Transform _runtimeRoot;
    Canvas _canvas;
    RectTransform _contentRoot;
    Font _font;

    void Awake()
    {
        if (shooting == null)
            shooting = GetComponent<RaycastShooting>();

        BuildUiIfNeeded();
        RebuildRows();
    }

    void OnEnable()
    {
        if (shooting != null)
            shooting.TemporaryModifiersChanged += HandleModifiersChanged;

        RebuildRows();
        RefreshRowTexts();
    }

    void OnDisable()
    {
        if (shooting != null)
            shooting.TemporaryModifiersChanged -= HandleModifiersChanged;
    }

    void OnDestroy()
    {
        if (_runtimeRoot != null)
            Destroy(_runtimeRoot.gameObject);
    }

    void Update()
    {
        RefreshRowTexts();
    }

    void HandleModifiersChanged(IReadOnlyList<WeaponModifierRuntimeInstance> _)
    {
        RebuildRows();
        RefreshRowTexts();
    }

    void BuildUiIfNeeded()
    {
        if (_canvas != null)
            return;

        _font = ResolveFont();

        var rootObject = new GameObject("WeaponModifierHUD");
        _runtimeRoot = rootObject.transform;
        _canvas = rootObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 220;
        rootObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        rootObject.AddComponent<GraphicRaycaster>();

        var panelObject = new GameObject("Panel");
        panelObject.transform.SetParent(_runtimeRoot, false);
        var panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = anchorMin;
        panelRect.anchorMax = anchorMax;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = panelObject.AddComponent<Image>();
        panelImage.sprite = UiSprites.White;
        panelImage.color = panelColor;

        var layout = panelObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        _contentRoot = panelRect;
    }

    void RebuildRows()
    {
        BuildUiIfNeeded();

        for (int i = _contentRoot.childCount - 1; i >= 0; i--)
        {
            var child = _contentRoot.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        _rows.Clear();

        if (shooting == null || shooting.ActiveTemporaryModifiers.Count == 0)
        {
            if (_canvas != null)
                _canvas.enabled = false;
            return;
        }

        for (int i = 0; i < shooting.ActiveTemporaryModifiers.Count; i++)
        {
            var modifier = shooting.ActiveTemporaryModifiers[i];
            var rowObject = new GameObject($"ModifierRow_{i + 1}");
            rowObject.transform.SetParent(_contentRoot, false);

            var rowLayout = rowObject.AddComponent<LayoutElement>();
            rowLayout.minHeight = 32f;

            var rowImage = rowObject.AddComponent<Image>();
            rowImage.sprite = UiSprites.White;
            rowImage.color = rowColor;

            var textObject = new GameObject("Label");
            textObject.transform.SetParent(rowObject.transform, false);
            var textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 6f);
            textRect.offsetMax = new Vector2(-10f, -6f);

            var text = textObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.color = textColor;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            _rows[modifier] = text;
        }

        _canvas.enabled = true;
    }

    void RefreshRowTexts()
    {
        if (shooting == null || _canvas == null)
            return;

        bool hasAnyModifier = false;
        foreach (var pair in _rows)
        {
            if (pair.Key == null || pair.Value == null)
                continue;

            hasAnyModifier = true;
            pair.Value.text = $"{pair.Key.DisplayName}  {pair.Key.RemainingDuration:0.0}s";
        }

        _canvas.enabled = hasAnyModifier;
    }

    static Font ResolveFont()
    {
        try
        {
            var legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (legacyFont != null)
                return legacyFont;
        }
        catch (System.ArgumentException)
        {
        }

        try
        {
            var arialFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (arialFont != null)
                return arialFont;
        }
        catch (System.ArgumentException)
        {
        }

        return Font.CreateDynamicFontFromOSFont("Arial", 16);
    }
}
