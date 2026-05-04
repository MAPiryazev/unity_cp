using System;
using UnityEngine;

[Serializable]
public struct StatWeaponModifierTemplate
{
    [SerializeField] string displayName;
    [TextArea]
    [SerializeField] string description;

    [Header("Damage")]
    [SerializeField] float damageDelta;
    [SerializeField] float damageMultiplier;

    [Header("Range / Fire Rate")]
    [SerializeField] float rangeDelta;
    [SerializeField] float shotsPerSecondDelta;
    [SerializeField] float shotsPerSecondMultiplier;

    [Header("Projectile Pattern")]
    [SerializeField] int projectileCountDelta;
    [SerializeField] float spreadAngleDelta;

    [Header("Tracer")]
    [SerializeField] float tracerWidthDelta;
    [SerializeField] float tracerDurationDelta;

    public string DisplayName => displayName;
    public string Description => description;

    public bool HasEffect =>
        !string.IsNullOrWhiteSpace(displayName) ||
        !string.IsNullOrWhiteSpace(description) ||
        Mathf.Abs(damageDelta) > 0.0001f ||
        Mathf.Abs(GetSafeMultiplier(damageMultiplier) - 1f) > 0.0001f ||
        Mathf.Abs(rangeDelta) > 0.0001f ||
        Mathf.Abs(shotsPerSecondDelta) > 0.0001f ||
        Mathf.Abs(GetSafeMultiplier(shotsPerSecondMultiplier) - 1f) > 0.0001f ||
        projectileCountDelta != 0 ||
        Mathf.Abs(spreadAngleDelta) > 0.0001f ||
        Mathf.Abs(tracerWidthDelta) > 0.0001f ||
        Mathf.Abs(tracerDurationDelta) > 0.0001f;

    public void Apply(ref HitscanWeaponSettings settings)
    {
        settings.Damage = (settings.Damage + damageDelta) * GetSafeMultiplier(damageMultiplier);
        settings.MaxRange += rangeDelta;
        settings.ShotsPerSecond = (settings.ShotsPerSecond + shotsPerSecondDelta) * GetSafeMultiplier(shotsPerSecondMultiplier);
        settings.ProjectileCount += projectileCountDelta;
        settings.SpreadAngle += spreadAngleDelta;
        settings.TracerWidth += tracerWidthDelta;
        settings.TracerDuration += tracerDurationDelta;
        settings.Clamp();
    }

    public static StatWeaponModifierTemplate Create(
        string displayName,
        string description,
        float damageDelta = 0f,
        float damageMultiplier = 1f,
        float rangeDelta = 0f,
        float shotsPerSecondDelta = 0f,
        float shotsPerSecondMultiplier = 1f,
        int projectileCountDelta = 0,
        float spreadAngleDelta = 0f,
        float tracerWidthDelta = 0f,
        float tracerDurationDelta = 0f)
    {
        return new StatWeaponModifierTemplate
        {
            displayName = displayName,
            description = description,
            damageDelta = damageDelta,
            damageMultiplier = damageMultiplier,
            rangeDelta = rangeDelta,
            shotsPerSecondDelta = shotsPerSecondDelta,
            shotsPerSecondMultiplier = shotsPerSecondMultiplier,
            projectileCountDelta = projectileCountDelta,
            spreadAngleDelta = spreadAngleDelta,
            tracerWidthDelta = tracerWidthDelta,
            tracerDurationDelta = tracerDurationDelta
        };
    }

    static float GetSafeMultiplier(float value)
    {
        return Mathf.Approximately(value, 0f) ? 1f : Mathf.Max(0f, value);
    }
}
