using System;
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
    [SerializeField] HitscanWeaponDefinition[] weaponLoadout = Array.Empty<HitscanWeaponDefinition>();
    [SerializeField] int startingWeaponIndex;
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
    readonly List<LineRenderer> _tracerLines = new List<LineRenderer>();
    Coroutine _tracerRoutine;
    AudioSource _audio;
    HitscanWeaponSettings _resolvedWeapon;
    int _activeWeaponIndex;
    Material _tracerMaterial;
    Transform _tracerRoot;

    public HitscanWeaponSettings ResolvedWeapon => _resolvedWeapon;
    public HitscanWeaponDefinition ActiveWeaponDefinition => ResolveActiveWeaponDefinition();
    public event Action<HitscanWeaponSettings> WeaponChanged;

    void Awake()
    {
        SyncModulesFromSerializedState();
        ClampWeaponIndex();
        EnsureLineOfFireLayers();
        RefreshResolvedWeapon();
        EnsureTracerRoot();
        CleanupLegacyTracerComponents();
        _audio = GetComponent<AudioSource>();
        if (_audio == null)
            _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.spatialBlend = 0f;
    }

    void OnDisable()
    {
        if (_tracerRoutine != null)
        {
            StopCoroutine(_tracerRoutine);
            _tracerRoutine = null;
        }

        DisableAllTracers();
    }

    void OnValidate()
    {
        SyncModulesFromSerializedState();
        ClampWeaponIndex();
        EnsureLineOfFireLayers();
        RefreshResolvedWeapon();
        ApplyLineVisuals();
    }

    void Update()
    {
        HandleWeaponSwitchInput();

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

        Vector3[] tracerEnds = FireProjectiles(muzzle, direction);
        PlayShootFeedback(tracerEnds);
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

    public void SetTracerOrigin(Transform origin)
    {
        tracerOrigin = origin;
    }

    public bool TrySelectWeaponSlot(int zeroBasedSlotIndex)
    {
        if (weaponLoadout == null || zeroBasedSlotIndex < 0 || zeroBasedSlotIndex >= weaponLoadout.Length)
            return false;

        if (weaponLoadout[zeroBasedSlotIndex] == null)
            return false;

        _activeWeaponIndex = zeroBasedSlotIndex;
        RefreshResolvedWeapon();
        return true;
    }

    Vector3 GetMuzzleWorld()
    {
        if (tracerOrigin != null)
            return tracerOrigin.position;
        return transform.position + Vector3.up * _resolvedWeapon.TracerOriginHeight;
    }

    void PlayShootFeedback(IReadOnlyList<Vector3> worldEnds)
    {
        if (_resolvedWeapon.FireSound != null && _audio != null)
            _audio.PlayOneShot(_resolvedWeapon.FireSound, _resolvedWeapon.FireSoundVolume);

        if (!_resolvedWeapon.ShowTracer || worldEnds == null || worldEnds.Count == 0)
            return;

        var start = GetMuzzleWorld();

        if (_tracerRoutine != null)
            StopCoroutine(_tracerRoutine);
        DisableAllTracers();
        _tracerRoutine = StartCoroutine(TracerRoutine(start, worldEnds));
    }

    IEnumerator TracerRoutine(Vector3 start, IReadOnlyList<Vector3> ends)
    {
        EnsureTracerLineCount(ends.Count);
        ApplyLineVisuals();

        for (int i = 0; i < ends.Count; i++)
        {
            var line = _tracerLines[i];
            line.enabled = true;
            line.SetPosition(0, start);
            line.SetPosition(1, ends[i]);
        }

        yield return new WaitForSeconds(_resolvedWeapon.TracerDuration);
        DisableAllTracers();
        _tracerRoutine = null;
    }

    void EnsureTracerLineCount(int requiredCount)
    {
        PurgeMissingTracerLines();
        if (requiredCount <= 0 || this == null)
            return;

        EnsureTracerRoot();
        while (_tracerLines.Count < requiredCount)
        {
            var tracerObject = new GameObject($"TracerLine_{_tracerLines.Count}");
            tracerObject.layer = gameObject.layer;
            tracerObject.transform.SetParent(_tracerRoot, false);

            var line = tracerObject.AddComponent<LineRenderer>();
            if (line == null)
                return;

            line.positionCount = 2;
            line.numCapVertices = 4;
            line.numCornerVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.useWorldSpace = true;
            _tracerMaterial ??= CreateTracerMaterial();
            if (_tracerMaterial != null)
                line.material = _tracerMaterial;
            line.enabled = false;
            _tracerLines.Add(line);
        }
    }

    void EnsureTracerRoot()
    {
        if (_tracerRoot != null)
            return;

        var existingRoot = transform.Find("TracerLines");
        if (existingRoot != null)
        {
            _tracerRoot = existingRoot;
            return;
        }

        _tracerRoot = new GameObject("TracerLines").transform;
        _tracerRoot.SetParent(transform, false);
        _tracerRoot.localPosition = Vector3.zero;
        _tracerRoot.localRotation = Quaternion.identity;
        _tracerRoot.localScale = Vector3.one;
    }

    void CleanupLegacyTracerComponents()
    {
        var legacyLines = GetComponents<LineRenderer>();
        for (int i = 0; i < legacyLines.Length; i++)
        {
            if (Application.isPlaying)
                Destroy(legacyLines[i]);
            else
                DestroyImmediate(legacyLines[i]);
        }
    }

    void ApplyLineVisuals()
    {
        for (int i = 0; i < _tracerLines.Count; i++)
        {
            var line = _tracerLines[i];
            if (line == null)
                continue;

            line.widthMultiplier = _resolvedWeapon.TracerWidth;
            line.startColor = _resolvedWeapon.TracerColor;
            line.endColor = _resolvedWeapon.TracerColor;
        }
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
        _resolvedWeapon = ResolveActiveWeaponDefinition() != null ? ResolveActiveWeaponDefinition().BuildSettings() : CreateFallbackSettings();
        for (int i = 0; i < _runtimeModules.Count; i++)
            _runtimeModules[i].Apply(ref _resolvedWeapon);

        _resolvedWeapon.Clamp();
        WeaponChanged?.Invoke(_resolvedWeapon);
    }

    HitscanWeaponSettings CreateFallbackSettings()
    {
        var settings = new HitscanWeaponSettings
        {
            FireMode = fireMode,
            Damage = damage,
            MaxRange = maxRange,
            ShotsPerSecond = shotsPerSecond,
            ProjectileCount = 1,
            SpreadAngle = 0f,
            ShowTracer = showTracer,
            TracerDuration = tracerDuration,
            TracerWidth = tracerWidth,
            TracerColor = tracerColor,
            TracerOriginHeight = tracerOriginHeight,
            FireSound = fireSound,
            FireSoundVolume = fireSoundVolume,
            VisualLocalPosition = new Vector3(0.28f, 1.05f, 0.3f),
            VisualLocalScale = new Vector3(0.14f, 0.14f, 0.6f),
            MuzzleLocalPosition = new Vector3(0f, 0f, 0.38f),
            VisualColor = new Color(0.18f, 0.18f, 0.2f, 1f)
        };

        settings.Clamp();
        return settings;
    }

    void DisableAllTracers()
    {
        PurgeMissingTracerLines();
        for (int i = 0; i < _tracerLines.Count; i++)
        {
            if (_tracerLines[i] != null)
                _tracerLines[i].enabled = false;
        }
    }

    void PurgeMissingTracerLines()
    {
        _tracerLines.RemoveAll(line => line == null);
    }

    Vector3[] FireProjectiles(Vector3 muzzle, Vector3 baseDirection)
    {
        int projectileCount = Mathf.Max(1, _resolvedWeapon.ProjectileCount);
        var tracerEnds = new Vector3[projectileCount];

        for (int i = 0; i < projectileCount; i++)
        {
            Vector3 shotDirection = GetProjectileDirection(baseDirection, i, projectileCount);
            if (Physics.Raycast(muzzle, shotDirection, out var hit, _resolvedWeapon.MaxRange, lineOfFireLayers, triggerInteraction))
            {
                DamageUtility.TryApplyDamage(hit.collider, new DamageInfo(_resolvedWeapon.Damage, hit.point, shotDirection, gameObject));
                tracerEnds[i] = hit.point;
            }
            else
            {
                tracerEnds[i] = muzzle + shotDirection * _resolvedWeapon.MaxRange;
            }
        }

        return tracerEnds;
    }

    Vector3 GetProjectileDirection(Vector3 baseDirection, int projectileIndex, int projectileCount)
    {
        if (projectileCount <= 1 || _resolvedWeapon.SpreadAngle <= 0.001f)
            return baseDirection;

        if (projectileIndex == 0)
            return baseDirection;

        float spreadHalf = _resolvedWeapon.SpreadAngle * 0.5f;
        float t = projectileCount <= 2 ? 0.5f : (projectileIndex - 1) / (float)(projectileCount - 2);
        float angle = Mathf.Lerp(-spreadHalf, spreadHalf, t);
        return Quaternion.AngleAxis(angle, Vector3.up) * baseDirection;
    }

    void HandleWeaponSwitchInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || weaponLoadout == null || weaponLoadout.Length == 0)
            return;

        if (keyboard.digit1Key.wasPressedThisFrame)
            TrySelectWeaponSlot(0);
        else if (keyboard.digit2Key.wasPressedThisFrame)
            TrySelectWeaponSlot(1);
    }

    HitscanWeaponDefinition ResolveActiveWeaponDefinition()
    {
        if (weaponLoadout != null && weaponLoadout.Length > 0)
        {
            int clampedIndex = Mathf.Clamp(_activeWeaponIndex, 0, weaponLoadout.Length - 1);
            if (weaponLoadout[clampedIndex] != null)
                return weaponLoadout[clampedIndex];
        }

        return weaponDefinition;
    }

    void ClampWeaponIndex()
    {
        _activeWeaponIndex = Mathf.Max(0, startingWeaponIndex);
        if (weaponLoadout != null && weaponLoadout.Length > 0)
            _activeWeaponIndex = Mathf.Clamp(_activeWeaponIndex, 0, weaponLoadout.Length - 1);
    }
}
