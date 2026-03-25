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

    [Header("Feedback")]
    [SerializeField] bool showTracer;
    [SerializeField] float tracerDuration;
    [SerializeField] float tracerWidth;
    [SerializeField] Color tracerColor;
    [SerializeField] float tracerOriginHeight;
    [SerializeField] AudioClip fireSound;
    [SerializeField] float fireSoundVolume;

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

    public void Clamp()
    {
        damage = Mathf.Max(0f, damage);
        maxRange = Mathf.Max(0.1f, maxRange);
        shotsPerSecond = Mathf.Max(0.01f, shotsPerSecond);
        tracerDuration = Mathf.Max(0.01f, tracerDuration);
        tracerWidth = Mathf.Max(0.001f, tracerWidth);
        tracerOriginHeight = Mathf.Max(0f, tracerOriginHeight);
        fireSoundVolume = Mathf.Clamp01(fireSoundVolume);
    }
}
