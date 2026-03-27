using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DEBUG-версия полоски HP: всегда по центру экрана, всегда видима, логи на каждый шаг.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerHealthBarUI : MonoBehaviour
{
    [SerializeField] Health health;

    Canvas _canvas;
    Image _bg;
    Image _fill;
    GameObject _hudRoot;
    Health _subscribedHealth;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Awake()
    {
        Debug.Log("[HP-BAR] Awake on " + gameObject.name, this);
        ResolveHealth();
        BuildBar();
    }

    void OnEnable()
    {
        Debug.Log("[HP-BAR] OnEnable", this);
        ResolveHealth();
        Subscribe();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void OnDestroy()
    {
        Unsubscribe();
        if (_hudRoot != null)
            Destroy(_hudRoot);
    }

    void Update()
    {
        // Принудительно держим полоску видимой каждый кадр — если ничего не видно, проблема не в Enable/Disable.
        if (_canvas != null && !_canvas.enabled)
        {
            _canvas.enabled = true;
            Debug.LogWarning("[HP-BAR] Canvas был выключен — принудительно включаю обратно.", this);
        }

        if (_hudRoot != null && !_hudRoot.activeSelf)
        {
            _hudRoot.SetActive(true);
            Debug.LogWarning("[HP-BAR] HUD root был деактивирован — принудительно активирую.", this);
        }
    }

    // ── Health ────────────────────────────────────────────────────────────

    void ResolveHealth()
    {
        if (health != null)
            return;

        health =
            GetComponent<Health>() ??
            GetComponentInParent<Health>() ??
            GetComponentInChildren<Health>(true);

        if (health == null)
        {
            var mover = FindFirstObjectByType<PlayerMovement>();
            if (mover != null)
                health = mover.GetComponent<Health>() ?? mover.GetComponentInChildren<Health>(true);
        }

        if (health == null)
            Debug.LogError("[HP-BAR] Health НЕ НАЙДЕН! Добавь Health на игрока.", this);
        else
            Debug.Log("[HP-BAR] Health найден: " + health.gameObject.name, this);
    }

    void Subscribe()
    {
        if (health == null)
            return;

        if (_subscribedHealth == health)
            return;

        Unsubscribe();
        _subscribedHealth = health;
        health.HealthChanged += OnHealthChanged;
        OnHealthChanged(health.CurrentHealth, health.MaxHealth);
        Debug.Log($"[HP-BAR] Подписан на Health. HP сейчас: {health.CurrentHealth}/{health.MaxHealth}", this);
    }

    void Unsubscribe()
    {
        if (_subscribedHealth == null)
            return;

        _subscribedHealth.HealthChanged -= OnHealthChanged;
        _subscribedHealth = null;
    }

    // ── UI ────────────────────────────────────────────────────────────────

    void BuildBar()
    {
        if (_hudRoot != null)
            return;

        // Canvas — отдельный объект в корне сцены (SSO нельзя вешать под обычный Transform).
        _hudRoot = new GameObject("PlayerHealthHUD");
        _canvas = _hudRoot.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9999; // поверх всего

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

        // Панель — ЦЕНТР ЭКРАНА, большая, невозможно не заметить.
        var panel = new GameObject("Panel");
        panel.transform.SetParent(_hudRoot.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.25f, 0.44f);
        panelRect.anchorMax = new Vector2(0.75f, 0.56f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Фон — ярко-красный, чтобы точно видно было.
        var bgGo = new GameObject("BG");
        bgGo.transform.SetParent(panel.transform, false);
        _bg = bgGo.AddComponent<Image>();
        _bg.sprite = UiSprites.White;
        _bg.color = new Color(0.8f, 0.1f, 0.1f, 1f);
        var bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Заполнение — ярко-зелёное поверх красного.
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(panel.transform, false);
        _fill = fillGo.AddComponent<Image>();
        _fill.sprite = UiSprites.White;
        _fill.color = new Color(0.1f, 0.9f, 0.2f, 1f);
        _fill.type = Image.Type.Filled;
        _fill.fillMethod = Image.FillMethod.Horizontal;
        _fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        _fill.fillAmount = 1f;
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Debug.Log("[HP-BAR] BuildBar ГОТОВО. Canvas sortingOrder=" + _canvas.sortingOrder + ", enabled=" + _canvas.enabled, this);
    }

    void OnHealthChanged(float current, float max)
    {
        if (_fill == null || max <= 0f)
            return;

        _fill.fillAmount = Mathf.Clamp01(current / max);
        Debug.Log($"[HP-BAR] OnHealthChanged: {current}/{max} → fillAmount={_fill.fillAmount:F2}", this);
    }

#if UNITY_EDITOR
    [ContextMenu("Debug: Take 10 damage")]
    void DebugTake10() => health?.TakeDamage(10f);
#endif
}
