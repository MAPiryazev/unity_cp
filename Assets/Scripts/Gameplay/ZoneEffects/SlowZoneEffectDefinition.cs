using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Zones/Slow Zone Effect", fileName = "SlowZoneEffect")]
public sealed class SlowZoneEffectDefinition : ZoneEffectDefinition
{
    [SerializeField] float movementMultiplier = 0.6f;
    [SerializeField] float aimTurnMultiplier = 0.85f;

    void OnValidate()
    {
        movementMultiplier = Mathf.Clamp(movementMultiplier, 0.05f, 1f);
        aimTurnMultiplier = Mathf.Clamp(aimTurnMultiplier, 0.05f, 1f);
    }

    public override void OnZoneEntered(GameObject target, GameObject zoneOwner)
    {
        if (!TryGetReceiver(target, out var receiver))
            return;

        receiver.SetMovementMultiplier(zoneOwner, movementMultiplier);
        receiver.SetAimTurnMultiplier(zoneOwner, aimTurnMultiplier);
    }

    public override void OnZoneExited(GameObject target, GameObject zoneOwner)
    {
        if (!TryGetReceiver(target, out var receiver))
            return;

        receiver.ClearMovementMultiplier(zoneOwner);
        receiver.ClearAimTurnMultiplier(zoneOwner);
    }

    static bool TryGetReceiver(GameObject target, out ZoneEffectReceiver receiver)
    {
        receiver = null;
        if (target == null)
            return false;

        receiver = target.GetComponentInParent<ZoneEffectReceiver>();
        if (receiver != null)
            return true;

        var movement = target.GetComponentInParent<PlayerMovement>();
        if (movement != null)
        {
            receiver = movement.GetComponent<ZoneEffectReceiver>();
            if (receiver == null)
                receiver = movement.gameObject.AddComponent<ZoneEffectReceiver>();
            return true;
        }

        var enemy = target.GetComponentInParent<SimpleEnemyController>();
        if (enemy != null)
        {
            receiver = enemy.GetComponent<ZoneEffectReceiver>();
            if (receiver == null)
                receiver = enemy.gameObject.AddComponent<ZoneEffectReceiver>();
            return true;
        }

        return false;
    }
}
