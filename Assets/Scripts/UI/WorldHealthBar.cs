using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Полоска HP над сущностью (world space). Скрыта при полном здоровье, показывается после урона.
/// </summary>
[DisallowMultipleComponent]
public sealed class WorldHealthBar : MonoBehaviour
{
    [SerializeField] Health health;
    [SerializeField] Vector3 worldOffset = new Vector3(0f, 1.85f, 0f);
    [SerializeField] Vector2 worldSize = new Vector2(160f, 18f);
    [SerializeField] float pixelsPerUnit = 100f;
    [SerializeField] float billboardScale = 0.01f;
    [SerializeField] Color backgroundColor = new Color(0.1f, 0.1f, 0.12f, 0.92f);
    [SerializeField] Color fillHigh = new Color(0.25f, 0.85f, 0.35f, 1f);
    [SerializeField] Color fillLow = new Color(0.9f, 0.2f, 0.2f, 1f);

    Transform _billboardRoot;
    Canvas _canvas;
    Image _fillImage;

    void Awake()
    {
        if (health == null)
            health = GetComponent<Health>() ?? GetComponentInParent<Health>();

        BuildIfNeeded();
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

    void LateUpdate()
    {
        if (_billboardRoot == null)
            return;

        var cam = Camera.main;
        if (cam == null)
            return;

        _billboardRoot.rotation = Quaternion.LookRotation(_billboardRoot.position - cam.transform.position);
    }

    void OnHealthChanged(float current, float max)
    {
        if (_fillImage == null || max <= 0.0001f)
            return;

        var n = Mathf.Clamp01(current / max);
        _fillImage.fillAmount = n;
        _fillImage.color = Color.Lerp(fillLow, fillHigh, n);

        if (_canvas != null)
            _canvas.enabled = current < max - 0.01f;
    }

    void BuildIfNeeded()
    {
        if (_canvas != null)
            return;

        var root = new GameObject("HealthBarBillboard");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = worldOffset;
        _billboardRoot = root.transform;
        _billboardRoot.localScale = Vector3.one * billboardScale;

        var canvasGo = new GameObject("Canvas");
        canvasGo.transform.SetParent(_billboardRoot, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 50;
        var rect = canvasGo.GetComponent<RectTransform>();
        rect.sizeDelta = worldSize;
        rect.localPosition = Vector3.zero;
        rect.localRotation = Quaternion.identity;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = pixelsPerUnit;

        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bg = bgGo.AddComponent<Image>();
        bg.sprite = UiSprites.White;
        bg.color = backgroundColor;
        var bgRect = bg.GetComponent<RectTransform>();
        StretchFull(bgRect);

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(canvasGo.transform, false);
        _fillImage = fillGo.AddComponent<Image>();
        _fillImage.sprite = UiSprites.White;
        _fillImage.type = Image.Type.Filled;
        _fillImage.fillMethod = Image.FillMethod.Horizontal;
        _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        _fillImage.fillAmount = 1f;
        _fillImage.color = fillHigh;
        var fillRect = _fillImage.rectTransform;
        StretchFull(fillRect);

        if (_canvas != null)
            _canvas.enabled = false;
    }

    static void StretchFull(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(3f, 3f);
        r.offsetMax = new Vector2(-3f, -3f);
    }

}
