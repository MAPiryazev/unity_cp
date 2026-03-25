using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Weapons/Stat Modifier", fileName = "StatWeaponModifier")]
public sealed class StatWeaponModifierDefinition : WeaponModifierDefinition
{
    [Header("Damage")]
    [SerializeField] float damageDelta;
    [SerializeField] float damageMultiplier = 1f;

    [Header("Range / Fire Rate")]
    [SerializeField] float rangeDelta;
    [SerializeField] float shotsPerSecondDelta;
    [SerializeField] float shotsPerSecondMultiplier = 1f;

    [Header("Projectile Pattern")]
    [SerializeField] int projectileCountDelta;
    [SerializeField] float spreadAngleDelta;

    [Header("Tracer")]
    [SerializeField] float tracerWidthDelta;
    [SerializeField] float tracerDurationDelta;

    public override void Apply(ref HitscanWeaponSettings settings)
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

    void OnValidate()
    {
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
        shotsPerSecondMultiplier = Mathf.Max(0f, shotsPerSecondMultiplier);
    }

    static float GetSafeMultiplier(float value)
    {
        return Mathf.Approximately(value, 0f) ? 1f : Mathf.Max(0f, value);
    }
}
