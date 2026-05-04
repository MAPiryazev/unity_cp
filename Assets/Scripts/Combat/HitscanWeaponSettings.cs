using System;
using UnityEngine;

[Serializable]
public struct HitscanWeaponSettings
{
    [Header("Combat")]
    [SerializeField] WeaponFireMode fireMode;
    [SerializeField] float damage;
    [SerializeField] float maxRange;
    [SerializeField] float shotsPerSecond;
    [SerializeField] int projectileCount;
    [SerializeField] float spreadAngle;

    [Header("Feedback")]
    [SerializeField] bool showTracer;
    [SerializeField] float tracerDuration;
    [SerializeField] float tracerWidth;
    [SerializeField] Color tracerColor;
    [SerializeField] float tracerOriginHeight;
    [SerializeField] AudioClip fireSound;
    [SerializeField] float fireSoundVolume;

    [Header("Presentation")]
    [SerializeField] Vector3 visualLocalPosition;
    [SerializeField] Vector3 visualLocalScale;
    [SerializeField] Vector3 muzzleLocalPosition;
    [SerializeField] Color visualColor;

    public WeaponFireMode FireMode
    {
        readonly get => fireMode;
        set => fireMode = value;
    }

    public float Damage
    {
        readonly get => damage;
        set => damage = value;
    }

    public float MaxRange
    {
        readonly get => maxRange;
        set => maxRange = value;
    }

    public float ShotsPerSecond
    {
        readonly get => shotsPerSecond;
        set => shotsPerSecond = value;
    }

    public int ProjectileCount
    {
        readonly get => projectileCount;
        set => projectileCount = value;
    }

    public float SpreadAngle
    {
        readonly get => spreadAngle;
        set => spreadAngle = value;
    }

    public bool ShowTracer
    {
        readonly get => showTracer;
        set => showTracer = value;
    }

    public float TracerDuration
    {
        readonly get => tracerDuration;
        set => tracerDuration = value;
    }

    public float TracerWidth
    {
        readonly get => tracerWidth;
        set => tracerWidth = value;
    }

    public Color TracerColor
    {
        readonly get => tracerColor;
        set => tracerColor = value;
    }

    public float TracerOriginHeight
    {
        readonly get => tracerOriginHeight;
        set => tracerOriginHeight = value;
    }

    public AudioClip FireSound
    {
        readonly get => fireSound;
        set => fireSound = value;
    }

    public float FireSoundVolume
    {
        readonly get => fireSoundVolume;
        set => fireSoundVolume = value;
    }

    public Vector3 VisualLocalPosition
    {
        readonly get => visualLocalPosition;
        set => visualLocalPosition = value;
    }

    public Vector3 VisualLocalScale
    {
        readonly get => visualLocalScale;
        set => visualLocalScale = value;
    }

    public Vector3 MuzzleLocalPosition
    {
        readonly get => muzzleLocalPosition;
        set => muzzleLocalPosition = value;
    }

    public Color VisualColor
    {
        readonly get => visualColor;
        set => visualColor = value;
    }

    public void Clamp()
    {
        damage = Mathf.Max(0f, damage);
        maxRange = Mathf.Max(0.1f, maxRange);
        shotsPerSecond = Mathf.Max(0.01f, shotsPerSecond);
        projectileCount = Mathf.Max(1, projectileCount);
        spreadAngle = Mathf.Max(0f, spreadAngle);
        tracerDuration = Mathf.Max(0.01f, tracerDuration);
        tracerWidth = Mathf.Max(0.001f, tracerWidth);
        tracerOriginHeight = Mathf.Max(0f, tracerOriginHeight);
        fireSoundVolume = Mathf.Clamp01(fireSoundVolume);
        visualLocalScale.x = Mathf.Max(0.01f, visualLocalScale.x);
        visualLocalScale.y = Mathf.Max(0.01f, visualLocalScale.y);
        visualLocalScale.z = Mathf.Max(0.01f, visualLocalScale.z);
    }
}
