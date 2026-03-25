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

    [Header("Tracer")]
    [SerializeField] float tracerWidthDelta;
    [SerializeField] float tracerDurationDelta;

    public override void Apply(ref HitscanWeaponSettings settings)
    {
        settings.Damage = (settings.Damage + damageDelta) * damageMultiplier;
        settings.MaxRange += rangeDelta;
        settings.ShotsPerSecond = (settings.ShotsPerSecond + shotsPerSecondDelta) * shotsPerSecondMultiplier;
        settings.TracerWidth += tracerWidthDelta;
        settings.TracerDuration += tracerDurationDelta;
        settings.Clamp();
    }

    void OnValidate()
    {
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
        shotsPerSecondMultiplier = Mathf.Max(0f, shotsPerSecondMultiplier);
    }
}
