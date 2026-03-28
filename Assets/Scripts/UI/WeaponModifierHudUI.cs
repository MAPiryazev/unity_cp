using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DEBUG-версия HUD модификаторов оружия: всегда по центру экрана, всегда видим.
/// Показывает "NO BUFFS" когда модификаторов нет.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RaycastShooting))]
public sealed class WeaponModifierHudUI : MonoBehaviour
{
    [SerializeField] RaycastShooting shooting;

    readonly Dictionary<WeaponModifierRuntimeInstance, Text> _rows =
        new Dictionary<WeaponModifierRuntimeInstance, Text>();

    GameObject _hudRoot;
    Canvas _canvas;
    RectTransform _contentRoot;
    Font _font;
    bool _subscribed;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Awake()
    {
        ResolveShootingReference();
        BuildUi();
    }

    void OnEnable()
    {
        ResolveShootingReference();
        Subscribe();
        RebuildRows();
    }

    void Start()
    {
        ResolveShootingReference();
        Subscribe();
        RebuildRows();
    }

    void OnDisable() => Unsubscribe();

    void OnDestroy()
    {
        Unsubscribe();
        if (_hudRoot != null)
            Destroy(_hudRoot);
    }

    void Update()
    {
        RefreshRowTexts();
    }

    // ── Shooting reference ────────────────────────────────────────────────

    void ResolveShootingReference()
    {
        if (shooting != null)
            return;

        shooting = GetComponent<RaycastShooting>() ?? GetComponentInParent<RaycastShooting>();

        if (shooting == null)
        {
            var player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
                shooting = player.GetComponent<RaycastShooting>() ?? player.GetComponentInChildren<RaycastShooting>(true);
        }

        if (shooting == null)
            Debug.LogError("[MOD-HUD] RaycastShooting не найден. Добавь его на игрока.", this);
    }

    void Subscribe()
    {
        if (shooting == null || _subscribed)
            return;

        shooting.TemporaryModifiersChanged += HandleModifiersChanged;
        _subscribed = true;
    }

    void Unsubscribe()
    {
        if (shooting == null || !_subscribed)
            return;

        shooting.TemporaryModifiersChanged -= HandleModifiersChanged;
        _subscribed = false;
    }

    void HandleModifiersChanged(IReadOnlyList<WeaponModifierRuntimeInstance> _)
    {
        RebuildRows();
    }

    // ── UI ────────────────────────────────────────────────────────────────

    void BuildUi()
    {
        if (_hudRoot != null)
            return;

        _font = ResolveFont();

        // Canvas в корне сцены (SSO нельзя вешать под обычный Transform).
        _hudRoot = new GameObject("WeaponModifierHUD");
        _canvas = _hudRoot.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9998; // чуть ниже HP bar (9999)

        var scaler = _hudRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        _hudRoot.AddComponent<GraphicRaycaster>();

        // Корневой RectTransform — на весь экран.
        var rootRect = _hudRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // Панель — правый верхний угол, компактная полоска.
        var panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(_hudRoot.transform, false);
        var panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.72f, 0.88f);
        panelRect.anchorMax = new Vector2(0.99f, 0.99f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = panelGo.AddComponent<Image>();
        panelImage.sprite = UiSprites.White;
        panelImage.color = new Color(0.06f, 0.07f, 0.12f, 0.88f);

        var layout = panelGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        _contentRoot = panelRect;

    }

    void RebuildRows()
    {
        if (_contentRoot == null)
            BuildUi();

        // Удаляем старые строки.
        for (int i = _contentRoot.childCount - 1; i >= 0; i--)
        {
            var child = _contentRoot.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        _rows.Clear();

        bool hasModifiers = shooting != null && shooting.ActiveTemporaryModifiers.Count > 0;

        // Показываем панель только при активных баффах.
        if (_canvas != null)
            _canvas.enabled = hasModifiers;

        if (!hasModifiers)
            return;

        for (int i = 0; i < shooting.ActiveTemporaryModifiers.Count; i++)
        {
            var modifier = shooting.ActiveTemporaryModifiers[i];
            var rowGo = new GameObject($"Row_{i + 1}");
            rowGo.transform.SetParent(_contentRoot, false);

            var rowLayout = rowGo.AddComponent<LayoutElement>();
            rowLayout.minHeight = 28f;

            var rowImage = rowGo.AddComponent<Image>();
            rowImage.sprite = UiSprites.White;
            rowImage.color = new Color(0.15f, 0.3f, 0.7f, 0.85f);

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(rowGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 2f);
            textRect.offsetMax = new Vector2(-8f, -2f);

            var text = textGo.AddComponent<Text>();
            text.font = _font;
            text.fontSize = 18;
            text.color = new Color(0.9f, 0.95f, 1f, 1f);
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            _rows[modifier] = text;
        }
    }

    void RefreshRowTexts()
    {
        foreach (var pair in _rows)
        {
            if (pair.Key == null || pair.Value == null)
                continue;

            pair.Value.text = $"{pair.Key.DisplayName}  {pair.Key.RemainingDuration:0.0}s";
        }
    }

    static Font ResolveFont()
    {
        try
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f != null) return f;
        }
        catch (System.ArgumentException) { }

        try
        {
            var f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (f != null) return f;
        }
        catch (System.ArgumentException) { }

        return Font.CreateDynamicFontFromOSFont("Arial", 16);
    }
}
