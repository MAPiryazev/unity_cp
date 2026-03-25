using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class WeaponModulePickup : MonoBehaviour
{
    [Header("Modifier")]
    [SerializeField] WeaponModifierDefinition moduleDefinition;
    [SerializeField] StatWeaponModifierTemplate runtimeModifier = default;
    [SerializeField] float effectDuration = 12f;

    [Header("Motion")]
    [SerializeField] float rotationSpeed = 90f;
    [SerializeField] float bobHeight = 0.12f;
    [SerializeField] float bobSpeed = 2f;
    [SerializeField] bool destroyOnPickup = true;

    [Header("Visual")]
    [SerializeField] string visualName = "Visual";
    [SerializeField] Vector3 visualScale = new Vector3(0.45f, 0.2f, 0.45f);
    [SerializeField] Vector3 visualOffset = new Vector3(0f, 0.35f, 0f);
    [SerializeField] Color visualColor = new Color(0.35f, 0.8f, 1f, 1f);

    Transform _visual;
    Renderer _visualRenderer;
    Vector3 _baseLocalVisualOffset;
    bool _pickedUp;

    public WeaponModifierDefinition ModuleDefinition => moduleDefinition;
    public float EffectDuration => Mathf.Max(0.1f, effectDuration);
    public event System.Action<WeaponModulePickup> Consumed;

    void Awake()
    {
        EnsureSpawnerExists();
        BuildVisualIfNeeded();
        ApplyVisualState();
    }

    void Reset()
    {
        var trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.isTrigger = true;
    }

    void OnValidate()
    {
        effectDuration = Mathf.Max(0.1f, effectDuration);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
        bobHeight = Mathf.Max(0f, bobHeight);
        bobSpeed = Mathf.Max(0f, bobSpeed);
        EnsureFallbackVisualColor();
        if (!Application.isPlaying)
        {
            BuildVisualIfNeeded();
            ApplyVisualState();
        }
    }

    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        if (_visual == null)
            return;

        float bobOffset = bobHeight <= 0.0001f ? 0f : Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        _visual.localPosition = _baseLocalVisualOffset + Vector3.up * bobOffset;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_pickedUp || other == null)
            return;

        var weapon = other.GetComponentInParent<RaycastShooting>();
        if (weapon == null)
            return;

        if (!TryApplyModifier(weapon))
            return;

        _pickedUp = true;
        Consumed?.Invoke(this);
        if (destroyOnPickup)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    public void Configure(
        WeaponModifierDefinition definition,
        StatWeaponModifierTemplate template,
        float duration,
        Color pickupColor,
        Vector3 pickupScale)
    {
        moduleDefinition = definition;
        runtimeModifier = template;
        effectDuration = Mathf.Max(0.1f, duration);
        visualColor = pickupColor;
        visualScale = pickupScale;
        EnsureFallbackVisualColor();
        BuildVisualIfNeeded();
        ApplyVisualState();
    }

    bool TryApplyModifier(RaycastShooting weapon)
    {
        if (weapon == null)
            return false;

        if (moduleDefinition != null)
        {
            weapon.AddModifier(moduleDefinition, EffectDuration);
            return true;
        }

        var modifierTemplate = ResolveRuntimeModifier();
        if (!modifierTemplate.HasEffect)
            return false;

        weapon.AddModifier(modifierTemplate, EffectDuration);
        return true;
    }

    StatWeaponModifierTemplate ResolveRuntimeModifier()
    {
        if (runtimeModifier.HasEffect)
            return runtimeModifier;

        return WeaponModifierPresets.GetDefaultTemplate(GetInstanceID());
    }

    void BuildVisualIfNeeded()
    {
        if (_visual == null)
        {
            _visual = transform.Find(visualName);
            if (_visual == null)
            {
                var visualObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualObject.name = visualName;
                visualObject.transform.SetParent(transform, false);

                var collider = visualObject.GetComponent<Collider>();
                if (collider != null)
                {
                    if (Application.isPlaying)
                        Destroy(collider);
                    else
                        DestroyImmediate(collider);
                }

                _visual = visualObject.transform;
            }
        }

        if (_visual != null)
        {
            var visualCollider = _visual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                if (Application.isPlaying)
                    Destroy(visualCollider);
                else
                    DestroyImmediate(visualCollider);
            }
        }

        if (_visualRenderer == null && _visual != null)
            _visualRenderer = _visual.GetComponent<Renderer>();
    }

    void ApplyVisualState()
    {
        if (_visual == null)
            return;

        _baseLocalVisualOffset = visualOffset;
        _visual.localPosition = _baseLocalVisualOffset;
        _visual.localRotation = Quaternion.identity;
        _visual.localScale = visualScale;

        if (_visualRenderer == null)
            _visualRenderer = _visual.GetComponent<Renderer>();

        ApplyRendererColor(_visualRenderer, visualColor);
    }

    void EnsureFallbackVisualColor()
    {
        if (visualColor.maxColorComponent > 0.001f)
            return;

        visualColor = WeaponModifierPresets.GetDefaultColor(GetInstanceID());
    }

    static void ApplyRendererColor(Renderer renderer, Color color)
    {
        if (renderer == null)
            return;

        var targetMaterial = Application.isPlaying ? renderer.material : renderer.sharedMaterial;
        if (targetMaterial != null)
            targetMaterial.color = color;
    }

    void EnsureSpawnerExists()
    {
        if (FindFirstObjectByType<WeaponModuleSpawner>() != null)
            return;

        var host = transform.parent != null ? transform.parent.gameObject : gameObject;
        if (host.GetComponent<WeaponModuleSpawner>() == null)
            host.AddComponent<WeaponModuleSpawner>();
    }
}
