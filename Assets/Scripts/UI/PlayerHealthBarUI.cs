using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Полоска здоровья игрока на экране. Подписывается на <see cref="Health"/> на том же объекте.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerHealthBarUI : MonoBehaviour
{
    [SerializeField] Health health;
    [Tooltip("Скрывать HUD при полном HP; после первого урона полоска остаётся видимой.")]
    [SerializeField] bool hideWhenFullHealth = true;
    [Tooltip("Создать Canvas + полоску в рантайме, если не настроено вручную.")]
    [SerializeField] bool createUiIfMissing = true;
    [SerializeField] Vector2 anchorMin = new Vector2(0.02f, 0.92f);
    [SerializeField] Vector2 anchorMax = new Vector2(0.28f, 0.98f);
    [SerializeField] Color backgroundColor = new Color(0.08f, 0.08f, 0.1f, 0.85f);
    [SerializeField] Color fillHigh = new Color(0.2f, 0.75f, 0.95f, 1f);
    [SerializeField] Color fillLow = new Color(0.95f, 0.25f, 0.2f, 1f);

    Canvas _canvas;
    Image _fillImage;

    void Awake()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (createUiIfMissing && _fillImage == null)
            BuildScreenBar();
    }

    void OnEnable()
    {
        if (health != null)
        {
            health.HealthChanged += OnHealthChanged;
            OnHealthChanged(health.CurrentHealth, health.MaxHealth);
        }
    }

    void OnDisable()
    {
        if (health != null)
            health.HealthChanged -= OnHealthChanged;
    }

    void OnHealthChanged(float current, float max)
    {
        if (_fillImage == null || max <= 0.0001f)
            return;

        var n = Mathf.Clamp01(current / max);
        _fillImage.fillAmount = n;
        _fillImage.color = Color.Lerp(fillLow, fillHigh, n);

        if (_canvas != null && hideWhenFullHealth)
            _canvas.enabled = current < max - 0.01f;
    }

    void BuildScreenBar()
    {
        var root = new GameObject("PlayerHealthHUD");
        root.transform.SetParent(transform, false);

        _canvas = root.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 200;
        root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        root.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("BarPanel");
        panel.transform.SetParent(root.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = anchorMin;
        panelRect.anchorMax = anchorMax;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.sprite = UiSprites.White;
        bg.color = backgroundColor;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(panel.transform, false);
        _fillImage = fillGo.AddComponent<Image>();
        _fillImage.sprite = UiSprites.White;
        _fillImage.type = Image.Type.Filled;
        _fillImage.fillMethod = Image.FillMethod.Horizontal;
        _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        _fillImage.fillAmount = 1f;
        _fillImage.color = fillHigh;
        var fillRect = _fillImage.rectTransform;
        fillRect.anchorMin = new Vector2(0.02f, 0.12f);
        fillRect.anchorMax = new Vector2(0.98f, 0.88f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
    }

}
