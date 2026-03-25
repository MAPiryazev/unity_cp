using UnityEngine;

public abstract class WeaponModifierDefinition : ScriptableObject
{
    [TextArea]
    [SerializeField] string description;

    public string Description => description;

    public abstract void Apply(ref HitscanWeaponSettings settings);
}
