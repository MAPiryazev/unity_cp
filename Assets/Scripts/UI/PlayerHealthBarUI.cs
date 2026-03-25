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
    Transform _runtimeRoot;

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

    void OnDestroy()
    {
        if (_runtimeRoot != null)
            Destroy(_runtimeRoot.gameObject);
    }

    void OnHealthChanged(float current, float max)
    {
        HealthBarFactory.ApplyFill(_fillImage, current, max, fillLow, fillHigh);

        if (_canvas != null && hideWhenFullHealth)
            _canvas.enabled = current < max - 0.01f;
    }

    void BuildScreenBar()
    {
        var references = HealthBarFactory.CreateScreenBar(
            transform,
            "PlayerHealthHUD",
            anchorMin,
            anchorMax,
            backgroundColor,
            fillHigh,
            new Vector2(0.02f, 0.12f),
            new Vector2(-0.02f, -0.12f));

        _runtimeRoot = references.Root;
        _canvas = references.Canvas;
        _fillImage = references.FillImage;
    }

}
