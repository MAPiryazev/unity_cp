using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/// <summary>
/// Хитскан top-down: направление — от дула к точке на полу под курсором (горизонтальный луч на <see cref="maxRange"/>).
/// Первое попадание по <see cref="lineOfFireLayers"/> (стены Default + враги Enemy) останавливает луч.
/// </summary>
[DisallowMultipleComponent]
public sealed class RaycastShooting : MonoBehaviour
{
    [Header("Weapon Source")]
    [SerializeField] HitscanWeaponDefinition weaponDefinition;
    [SerializeField] WeaponModifierDefinition[] installedModules = System.Array.Empty<WeaponModifierDefinition>();

    [Header("Fallback Settings")]
    [SerializeField] Camera aimCamera;
    [SerializeField] float damage = 10f;
    [SerializeField] float maxRange = 200f;
    [Tooltip("Выстрелов в секунду (режим Automatic) или максимальная скорость кликов (Semi).")]
    [SerializeField] float shotsPerSecond = 8f;
    [SerializeField] WeaponFireMode fireMode = WeaponFireMode.SemiAutomatic;
    [Tooltip("Слои, с которыми сталкивается луч: Default (стены/пол при необходимости) + Enemy. Без слоя Player — иначе луч упирается в себя.")]
    [FormerlySerializedAs("enemyLayers")]
    [SerializeField] LayerMask lineOfFireLayers;
    [Tooltip("Триггерные коллайдеры: Collide — учитывать (частые hitbox'ы врагов). Ignore — только не-триггеры.")]
    [SerializeField] QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;
    [Tooltip("Не дать двум выстрелам в один кадр (дубли ввода / несколько источников).")]
    [SerializeField] bool preventSameFrameDoubleShot = true;

    [Header("Feedback")]
    [SerializeField] bool showTracer = true;
    [SerializeField] float tracerDuration = 0.07f;
    [SerializeField] float tracerWidth = 0.04f;
    [SerializeField] Color tracerColor = new Color(1f, 0.92f, 0.2f, 0.95f);
    [SerializeField] Transform tracerOrigin;
    [SerializeField] float tracerOriginHeight = 0.55f;
    [SerializeField] AudioClip fireSound;
    [SerializeField] [Range(0f, 1f)] float fireSoundVolume = 0.45f;

    readonly List<WeaponModifierDefinition> _runtimeModules = new List<WeaponModifierDefinition>();
    float _nextShotTime;
    int _lastShotFrame = -1;
    LineRenderer _line;
    Coroutine _tracerRoutine;
    AudioSource _audio;
    HitscanWeaponSettings _resolvedWeapon;

    void Awake()
    {
        SyncModulesFromSerializedState();
        EnsureLineOfFireLayers();
        RefreshResolvedWeapon();
        _audio = GetComponent<AudioSource>();
        if (_audio == null)
            _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.spatialBlend = 0f;
    }

    void OnValidate()
    {
        SyncModulesFromSerializedState();
        EnsureLineOfFireLayers();
        RefreshResolvedWeapon();
        ApplyLineVisuals();
    }

    void Update()
    {
        if (!ShouldFireThisUpdate())
            return;

        var gap = 1f / _resolvedWeapon.ShotsPerSecond;
        if (Time.time < _nextShotTime)
            return;

        if (preventSameFrameDoubleShot && Time.frameCount == _lastShotFrame)
            return;

        _nextShotTime = Time.time + gap;
        _lastShotFrame = Time.frameCount;
        FireOnce();
    }

    bool ShouldFireThisUpdate()
    {
        return _resolvedWeapon.FireMode == WeaponFireMode.Automatic ? IsFireHeld() : WasFirePressedThisFrame();
    }

    static bool WasFirePressedThisFrame()
    {
        var mouse = Mouse.current;
        if (mouse != null)
            return mouse.leftButton.wasPressedThisFrame;

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    static bool IsFireHeld()
    {
        var mouse = Mouse.current;
        if (mouse != null)
            return mouse.leftButton.isPressed;

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButton(0);
#else
        return false;
#endif
    }

    void FireOnce()
    {
        var cam = aimCamera != null ? aimCamera : Camera.main;
        if (cam == null)
            return;

        if (!TopDownAimUtility.TryGetPointerScreenPosition(out var screenPos))
            return;

        if (!TopDownAimUtility.TryGetGroundPointUnderScreenPosition(cam, screenPos, out var groundAim))
            return;

        var muzzle = GetMuzzleWorld();
        if (!TopDownAimUtility.TryGetFlatDirection(muzzle, groundAim, out var direction))
            return;

        Vector3 tracerEnd;
        if (Physics.Raycast(muzzle, direction, out var hit, _resolvedWeapon.MaxRange, lineOfFireLayers, triggerInteraction))
        {
            DamageUtility.TryApplyDamage(hit.collider, new DamageInfo(_resolvedWeapon.Damage, hit.point, direction, gameObject));
            tracerEnd = hit.point;
        }
        else
        {
            tracerEnd = muzzle + direction * _resolvedWeapon.MaxRange;
        }

        PlayShootFeedback(tracerEnd);
    }

    public void AddModifier(WeaponModifierDefinition modifier)
    {
        if (modifier == null)
            return;

        _runtimeModules.Add(modifier);
        RefreshResolvedWeapon();
    }

    public void RemoveModifier(WeaponModifierDefinition modifier)
    {
        if (modifier == null)
            return;

        if (_runtimeModules.Remove(modifier))
            RefreshResolvedWeapon();
    }

    Vector3 GetMuzzleWorld()
    {
        if (tracerOrigin != null)
            return tracerOrigin.position;
        return transform.position + Vector3.up * _resolvedWeapon.TracerOriginHeight;
    }

    void PlayShootFeedback(Vector3 worldEnd)
    {
        if (_resolvedWeapon.FireSound != null && _audio != null)
            _audio.PlayOneShot(_resolvedWeapon.FireSound, _resolvedWeapon.FireSoundVolume);

        if (!_resolvedWeapon.ShowTracer)
            return;

        var start = GetMuzzleWorld();

        if (_tracerRoutine != null)
            StopCoroutine(_tracerRoutine);
        _tracerRoutine = StartCoroutine(TracerRoutine(start, worldEnd));
    }

    IEnumerator TracerRoutine(Vector3 start, Vector3 end)
    {
        EnsureLineRenderer();
        ApplyLineVisuals();
        _line.enabled = true;
        _line.SetPosition(0, start);
        _line.SetPosition(1, end);
        yield return new WaitForSeconds(_resolvedWeapon.TracerDuration);
        _line.enabled = false;
        _tracerRoutine = null;
    }

    void EnsureLineRenderer()
    {
        if (_line != null)
            return;

        _line = gameObject.AddComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.numCapVertices = 4;
        _line.numCornerVertices = 2;
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows = false;
        _line.useWorldSpace = true;
        _line.material = CreateTracerMaterial();
        _line.enabled = false;
    }

    void ApplyLineVisuals()
    {
        if (_line == null)
            return;

        _line.widthMultiplier = _resolvedWeapon.TracerWidth;
        _line.startColor = _resolvedWeapon.TracerColor;
        _line.endColor = _resolvedWeapon.TracerColor;
    }

    static Material CreateTracerMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", Color.white);
        else if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", Color.white);
        return mat;
    }

    void SyncModulesFromSerializedState()
    {
        _runtimeModules.Clear();
        if (installedModules == null)
            return;

        for (int i = 0; i < installedModules.Length; i++)
        {
            if (installedModules[i] != null)
                _runtimeModules.Add(installedModules[i]);
        }
    }

    void EnsureLineOfFireLayers()
    {
        if (lineOfFireLayers.value == 0)
        {
            lineOfFireLayers = LayerMask.GetMask("Default", "Enemy");
            return;
        }

        var enemyOnly = LayerMask.GetMask("Enemy");
        if (lineOfFireLayers.value == enemyOnly)
            lineOfFireLayers |= LayerMask.GetMask("Default");
    }

    void RefreshResolvedWeapon()
    {
        _resolvedWeapon = weaponDefinition != null ? weaponDefinition.BuildSettings() : CreateFallbackSettings();
        for (int i = 0; i < _runtimeModules.Count; i++)
            _runtimeModules[i].Apply(ref _resolvedWeapon);

        _resolvedWeapon.Clamp();
    }

    HitscanWeaponSettings CreateFallbackSettings()
    {
        var settings = new HitscanWeaponSettings
        {
            FireMode = fireMode,
            Damage = damage,
            MaxRange = maxRange,
            ShotsPerSecond = shotsPerSecond,
            ShowTracer = showTracer,
            TracerDuration = tracerDuration,
            TracerWidth = tracerWidth,
            TracerColor = tracerColor,
            TracerOriginHeight = tracerOriginHeight,
            FireSound = fireSound,
            FireSoundVolume = fireSoundVolume
        };

        settings.Clamp();
        return settings;
    }
}
