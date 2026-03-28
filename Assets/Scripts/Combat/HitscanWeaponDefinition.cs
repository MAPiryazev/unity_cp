using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Weapons/Hitscan Weapon Definition", fileName = "HitscanWeaponDefinition")]
public sealed class HitscanWeaponDefinition : ScriptableObject
{
    [SerializeField] HitscanWeaponSettings settings = CreateDefaultSettings();

    [Header("Visual Mesh")]
    [Tooltip("Prefab shown in the player's hand for this weapon. If null, a fallback cube is used.")]
    [SerializeField] GameObject visualPrefab;

    public GameObject VisualPrefab => visualPrefab;

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
            ProjectileCount = 1,
            SpreadAngle = 0f,
            ShowTracer = true,
            TracerDuration = 0.07f,
            TracerWidth = 0.04f,
            TracerColor = new Color(1f, 0.9f, 0.08f, 0.95f),
            TracerOriginHeight = 0.55f,
            FireSoundVolume = 0.45f,
            VisualLocalPosition = new Vector3(0.28f, 1.05f, 0.3f),
            VisualLocalScale = new Vector3(0.14f, 0.14f, 0.6f),
            MuzzleLocalPosition = new Vector3(0f, 0f, 0.38f),
            VisualColor = new Color(0.18f, 0.18f, 0.2f, 1f)
        };

        defaults.Clamp();
        return defaults;
    }

    void OnEnable()
    {
        ClampSettings();
    }

    void OnValidate()
    {
        ClampSettings();
    }

    void ClampSettings()
    {
        settings.Clamp();
    }
}
