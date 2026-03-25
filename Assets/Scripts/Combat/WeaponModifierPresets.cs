using UnityEngine;

public static class WeaponModifierPresets
{
    static readonly StatWeaponModifierTemplate[] DefaultTemplates =
    {
        StatWeaponModifierTemplate.Create(
            "Rapid Fire",
            "Greatly increases fire rate for a short time.",
            shotsPerSecondDelta: 6f,
            tracerWidthDelta: 0.01f),
        StatWeaponModifierTemplate.Create(
            "Power Rounds",
            "Shots hit harder and fly farther.",
            damageDelta: 10f,
            rangeDelta: 30f,
            tracerDurationDelta: 0.03f),
        StatWeaponModifierTemplate.Create(
            "Scatter Burst",
            "Adds two extra pellets with a wider spread.",
            damageMultiplier: 0.8f,
            projectileCountDelta: 2,
            spreadAngleDelta: 14f,
            tracerWidthDelta: 0.015f)
    };

    static readonly Color[] DefaultColors =
    {
        new Color(0.3f, 0.85f, 1f, 1f),
        new Color(1f, 0.55f, 0.25f, 1f),
        new Color(0.85f, 0.45f, 1f, 1f)
    };

    public static int DefaultTemplateCount => DefaultTemplates.Length;

    public static StatWeaponModifierTemplate GetDefaultTemplate(int index)
    {
        if (DefaultTemplates.Length == 0)
            return default;

        int wrappedIndex = Mathf.Abs(index) % DefaultTemplates.Length;
        return DefaultTemplates[wrappedIndex];
    }

    public static Color GetDefaultColor(int index)
    {
        if (DefaultColors.Length == 0)
            return Color.white;

        int wrappedIndex = Mathf.Abs(index) % DefaultColors.Length;
        return DefaultColors[wrappedIndex];
    }
}
