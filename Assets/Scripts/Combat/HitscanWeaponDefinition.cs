using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Weapons/Hitscan Weapon Definition", fileName = "HitscanWeaponDefinition")]
public sealed class HitscanWeaponDefinition : ScriptableObject
{
    [SerializeField] HitscanWeaponSettings settings = CreateDefaultSettings();

    public HitscanWeaponSettings BuildSettings()
    {
        var resolved = settings;
        resolved.Clamp();
        return resolved;
    }

    static HitscanWeaponSettings CreateDefaultSettings()
    {
        var defaults = new HitscanWeaponSettings
        {
            FireMode = WeaponFireMode.SemiAutomatic,
            Damage = 10f,
            MaxRange = 200f,
            ShotsPerSecond = 8f,
            ShowTracer = true,
            TracerDuration = 0.07f,
            TracerWidth = 0.04f,
            TracerColor = new Color(1f, 0.92f, 0.2f, 0.95f),
            TracerOriginHeight = 0.55f,
            FireSoundVolume = 0.45f
        };

        defaults.Clamp();
        return defaults;
    }

    void OnValidate()
    {
        settings.Clamp();
    }
}
