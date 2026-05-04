using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Zones/Smoke Zone Effect", fileName = "SmokeZoneEffect")]
public sealed class SmokeZoneEffectDefinition : ZoneEffectDefinition
{
    [SerializeField] float movementMultiplier = 0.85f;
    [SerializeField] float aimTurnMultiplier = 0.7f;
    [SerializeField] float contactDamageMultiplier = 0.75f;
    [SerializeField] float attackCooldownMultiplier = 1.25f;

    void OnValidate()
    {
        movementMultiplier = Mathf.Clamp(movementMultiplier, 0.05f, 1f);
        aimTurnMultiplier = Mathf.Clamp(aimTurnMultiplier, 0.05f, 1f);
        contactDamageMultiplier = Mathf.Clamp(contactDamageMultiplier, 0.05f, 2f);
        attackCooldownMultiplier = Mathf.Clamp(attackCooldownMultiplier, 0.05f, 3f);
    }

    public override void OnZoneEntered(GameObject target, GameObject zoneOwner)
    {
        if (!TryGetReceiver(target, out var receiver))
            return;

        receiver.SetMovementMultiplier(zoneOwner, movementMultiplier);
        receiver.SetAimTurnMultiplier(zoneOwner, aimTurnMultiplier);
        receiver.SetContactDamageMultiplier(zoneOwner, contactDamageMultiplier);
        receiver.SetAttackCooldownMultiplier(zoneOwner, attackCooldownMultiplier);
    }

    public override void OnZoneExited(GameObject target, GameObject zoneOwner)
    {
        if (!TryGetReceiver(target, out var receiver))
            return;

        receiver.ClearMovementMultiplier(zoneOwner);
        receiver.ClearAimTurnMultiplier(zoneOwner);
        receiver.ClearContactDamageMultiplier(zoneOwner);
        receiver.ClearAttackCooldownMultiplier(zoneOwner);
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
