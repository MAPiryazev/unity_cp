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
    [SerializeField] float defaultTemporaryModifierDuration = 12f;

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

    readonly List<WeaponModifierDefinition> _persistentModules = new List<WeaponModifierDefinition>();
    readonly List<WeaponModifierRuntimeInstance> _temporaryModules = new List<WeaponModifierRuntimeInstance>();
    float _nextShotTime;
    int _lastShotFrame = -1;
    readonly List<LineRenderer> _tracerLines = new List<LineRenderer>();
    Coroutine _tracerRoutine;
    AudioSource _audio;
    HitscanWeaponSettings _resolvedWeapon;
    int _activeWeaponIndex;
    Material _tracerMaterial;
    Transform _tracerRoot;
    ZoneEffectReceiver _zoneEffects;

    public HitscanWeaponSettings ResolvedWeapon => _resolvedWeapon;
    public HitscanWeaponDefinition ActiveWeaponDefinition => ResolveActiveWeaponDefinition();
    public IReadOnlyList<WeaponModifierRuntimeInstance> ActiveTemporaryModifiers => _temporaryModules;
    public event Action<HitscanWeaponSettings> WeaponChanged;
    public event Action<IReadOnlyList<WeaponModifierRuntimeInstance>> TemporaryModifiersChanged;

    void Awake()
    {
        SyncPersistentModulesFromSerializedState();
        ClampWeaponIndex();
        EnsureLineOfFireLayers();
        RefreshResolvedWeapon();
        EnsureTracerRoot();
        CleanupLegacyTracerComponents();
        EnsureAudioSource();
        EnsureModifierHud();
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

    void OnDestroy()
    {
        if (_tracerMaterial == null)
            return;

        if (Application.isPlaying)
            Destroy(_tracerMaterial);
        else
            DestroyImmediate(_tracerMaterial);
    }

    void OnValidate()
    {
        SyncPersistentModulesFromSerializedState();
        ClampWeaponIndex();
        EnsureLineOfFireLayers();
        RefreshResolvedWeapon();
        ApplyLineVisuals();
    }

    void Update()
    {
        CleanupExpiredTemporaryModifiers();
        HandleWeaponSwitchInput();

        if (!ShouldFireThisUpdate())
            return;

        if (!TryBeginShot())
            return;
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
        AddModifier(modifier, defaultTemporaryModifierDuration);
    }

    public void AddModifier(WeaponModifierDefinition modifier, float duration)
    {
        if (modifier == null)
            return;

        if (TryRefreshModifier(modifier, duration))
            return;

        _temporaryModules.Add(new WeaponModifierRuntimeInstance(modifier, duration));
        RefreshResolvedWeapon();
        NotifyTemporaryModifiersChanged();
    }

    public void AddModifier(StatWeaponModifierTemplate modifierTemplate, float duration)
    {
        if (!modifierTemplate.HasEffect)
            return;

        if (TryRefreshModifier(modifierTemplate, duration))
            return;

        _temporaryModules.Add(new WeaponModifierRuntimeInstance(modifierTemplate, duration));
        RefreshResolvedWeapon();
        NotifyTemporaryModifiersChanged();
    }

    public void RemoveModifier(WeaponModifierDefinition modifier)
    {
        if (modifier == null)
            return;

        for (int i = _temporaryModules.Count - 1; i >= 0; i--)
        {
            if (!_temporaryModules[i].Matches(modifier))
                continue;

            _temporaryModules.RemoveAt(i);
            RefreshResolvedWeapon();
            NotifyTemporaryModifiersChanged();
            break;
        }
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
        if (shader == null)
            return null;
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", Color.white);
        else if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", Color.white);
        return mat;
    }

    void SyncPersistentModulesFromSerializedState()
    {
        _persistentModules.Clear();
        if (installedModules == null)
            return;

        for (int i = 0; i < installedModules.Length; i++)
        {
            if (installedModules[i] != null)
                _persistentModules.Add(installedModules[i]);
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
        RemoveExpiredTemporaryModifiers();
        var activeWeapon = ResolveActiveWeaponDefinition();
        _resolvedWeapon = activeWeapon != null ? activeWeapon.BuildSettings() : CreateFallbackSettings();
        for (int i = 0; i < _persistentModules.Count; i++)
            _persistentModules[i].Apply(ref _resolvedWeapon);
        for (int i = 0; i < _temporaryModules.Count; i++)
            _temporaryModules[i].Apply(ref _resolvedWeapon);

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
        var shotDirections = BuildShotDirections(baseDirection, projectileCount);

        for (int i = 0; i < projectileCount; i++)
            tracerEnds[i] = FireSingleProjectile(muzzle, shotDirections[i]);

        return tracerEnds;
    }

    Vector3[] BuildShotDirections(Vector3 baseDirection, int projectileCount)
    {
        var directions = new Vector3[projectileCount];
        if (projectileCount <= 1 || _resolvedWeapon.SpreadAngle <= 0.001f)
        {
            directions[0] = baseDirection;
            return directions;
        }

        for (int i = 0; i < projectileCount; i++)
            directions[i] = SampleSpreadDirection(baseDirection);

        return directions;
    }

    Vector3 SampleSpreadDirection(Vector3 baseDirection)
    {
        float spreadHalf = _resolvedWeapon.SpreadAngle * 0.5f;
        float randomAngle = UnityEngine.Random.Range(-spreadHalf, spreadHalf);
        return Quaternion.AngleAxis(randomAngle, Vector3.up) * baseDirection;
    }

    Vector3 FireSingleProjectile(Vector3 muzzle, Vector3 shotDirection)
    {
        if (Physics.Raycast(muzzle, shotDirection, out var hit, _resolvedWeapon.MaxRange, lineOfFireLayers, triggerInteraction))
        {
            float damageAmount = _resolvedWeapon.Damage;
            EnsureZoneEffects();
            if (_zoneEffects != null)
                damageAmount *= _zoneEffects.ContactDamageMultiplier;

            DamageUtility.TryApplyDamage(hit.collider, new DamageInfo(damageAmount, hit.point, shotDirection, gameObject));
            return hit.point;
        }

        return muzzle + shotDirection * _resolvedWeapon.MaxRange;
    }

    void HandleWeaponSwitchInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || weaponLoadout == null || weaponLoadout.Length == 0)
            return;

        int selectedSlot = GetPressedWeaponSlot(keyboard);
        if (selectedSlot < 0 || selectedSlot >= weaponLoadout.Length)
            return;

        TrySelectWeaponSlot(selectedSlot);
    }

    bool TryBeginShot()
    {
        if (Time.time < _nextShotTime)
            return false;

        if (preventSameFrameDoubleShot && Time.frameCount == _lastShotFrame)
            return false;

        _nextShotTime = Time.time + GetShotCooldown();
        _lastShotFrame = Time.frameCount;
        return true;
    }

    float GetShotCooldown()
    {
        float baseCooldown = 1f / _resolvedWeapon.ShotsPerSecond;
        EnsureZoneEffects();
        float attackCooldownMultiplier = _zoneEffects != null ? _zoneEffects.AttackCooldownMultiplier : 1f;
        return baseCooldown * attackCooldownMultiplier;
    }

    void EnsureZoneEffects()
    {
        if (_zoneEffects == null)
            _zoneEffects = GetComponent<ZoneEffectReceiver>();
    }

    static int GetPressedWeaponSlot(Keyboard keyboard)
    {
        if (keyboard.digit1Key.wasPressedThisFrame) return 0;
        if (keyboard.digit2Key.wasPressedThisFrame) return 1;
        if (keyboard.digit3Key.wasPressedThisFrame) return 2;
        if (keyboard.digit4Key.wasPressedThisFrame) return 3;
        if (keyboard.digit5Key.wasPressedThisFrame) return 4;
        if (keyboard.digit6Key.wasPressedThisFrame) return 5;
        if (keyboard.digit7Key.wasPressedThisFrame) return 6;
        if (keyboard.digit8Key.wasPressedThisFrame) return 7;
        if (keyboard.digit9Key.wasPressedThisFrame) return 8;
        return -1;
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

    void CleanupExpiredTemporaryModifiers(bool notifyOnly = true)
    {
        if (!RemoveExpiredTemporaryModifiers())
            return;

        RefreshResolvedWeapon();
        if (notifyOnly)
            NotifyTemporaryModifiersChanged();
    }

    bool RemoveExpiredTemporaryModifiers()
    {
        bool removedAny = false;
        for (int i = _temporaryModules.Count - 1; i >= 0; i--)
        {
            if (!_temporaryModules[i].IsExpired)
                continue;

            _temporaryModules.RemoveAt(i);
            removedAny = true;
        }

        return removedAny;
    }

    void NotifyTemporaryModifiersChanged()
    {
        TemporaryModifiersChanged?.Invoke(_temporaryModules);
    }

    bool TryRefreshModifier(WeaponModifierDefinition modifier, float duration)
    {
        for (int i = 0; i < _temporaryModules.Count; i++)
        {
            if (!_temporaryModules[i].Matches(modifier))
                continue;

            _temporaryModules[i].Refresh(duration);
            NotifyTemporaryModifiersChanged();
            return true;
        }

        return false;
    }

    bool TryRefreshModifier(StatWeaponModifierTemplate modifierTemplate, float duration)
    {
        for (int i = 0; i < _temporaryModules.Count; i++)
        {
            if (!_temporaryModules[i].Matches(modifierTemplate))
                continue;

            _temporaryModules[i].Refresh(duration);
            NotifyTemporaryModifiersChanged();
            return true;
        }

        return false;
    }

    void EnsureAudioSource()
    {
        _audio = GetComponent<AudioSource>();
        if (_audio == null)
            _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.spatialBlend = 0f;
    }

    void EnsureModifierHud()
    {
        if (GetComponent<WeaponModifierHudUI>() == null)
            gameObject.AddComponent<WeaponModifierHudUI>();
    }
}
