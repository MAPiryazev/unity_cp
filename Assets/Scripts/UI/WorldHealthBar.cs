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

    /// <summary>Overrides the bar position above the enemy. Safe to call after AddComponent.</summary>
    public void SetOffset(Vector3 offset)
    {
        worldOffset = offset;
        if (_billboardRoot != null)
            _billboardRoot.localPosition = offset;
    }

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
        HealthBarFactory.ApplyFill(_fillImage, current, max, fillLow, fillHigh);

        if (_canvas != null)
            _canvas.enabled = current < max - 0.01f;
    }

    void BuildIfNeeded()
    {
        if (_canvas != null)
            return;

        var references = HealthBarFactory.CreateWorldBar(
            transform,
            "HealthBarBillboard",
            worldOffset,
            worldSize,
            billboardScale,
            pixelsPerUnit,
            backgroundColor,
            fillHigh);

        _billboardRoot = references.Root;
        _canvas = references.Canvas;
        _fillImage = references.FillImage;
    }
}
