using UnityEngine;

public abstract class ZoneEffectDefinition : ScriptableObject
{
    [TextArea]
    [SerializeField] string description;

    public string Description => description;

    public virtual void OnZoneEntered(GameObject target, GameObject zoneOwner)
    {
    }

    public virtual void OnZoneExited(GameObject target, GameObject zoneOwner)
    {
    }
}
