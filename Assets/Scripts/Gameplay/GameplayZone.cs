using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class GameplayZone : MonoBehaviour
{
    [SerializeField] ZoneEffectDefinition effect;
    [SerializeField] bool affectTriggers;

    void Reset()
    {
        EnsureTriggerCollider();
    }

    void OnValidate()
    {
        EnsureTriggerCollider();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!CanAffect(other))
            return;

        effect?.OnZoneEntered(other.gameObject, gameObject);
    }

    void OnTriggerExit(Collider other)
    {
        if (!CanAffect(other))
            return;

        effect?.OnZoneExited(other.gameObject, gameObject);
    }

    bool CanAffect(Collider other)
    {
        if (other == null)
            return false;

        return affectTriggers || !other.isTrigger;
    }

    void EnsureTriggerCollider()
    {
        var trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.isTrigger = true;
    }
}
