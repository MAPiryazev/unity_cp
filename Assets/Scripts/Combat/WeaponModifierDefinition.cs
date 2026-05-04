using UnityEngine;

public abstract class WeaponModifierDefinition : ScriptableObject
{
    [SerializeField] string displayName;
    [TextArea]
    [SerializeField] string description;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;

    public abstract void Apply(ref HitscanWeaponSettings settings);
}
