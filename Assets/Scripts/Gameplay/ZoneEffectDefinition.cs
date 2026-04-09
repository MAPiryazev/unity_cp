using UnityEngine;

public abstract class ZoneEffectDefinition : ScriptableObject
{
    [TextArea]
    [SerializeField] string description;
    [SerializeField] GameObject prefab;

    public string Description => description;
    public GameObject Prefab => prefab;

    void OnValidate()
    {
        description = description?.Trim() ?? string.Empty;
    }

    public virtual void OnZoneEntered(GameObject target, GameObject zoneOwner)
    {
    }

    public virtual void OnZoneExited(GameObject target, GameObject zoneOwner)
    {
    }
}
