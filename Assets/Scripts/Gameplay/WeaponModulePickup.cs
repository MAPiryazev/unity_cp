using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class WeaponModulePickup : MonoBehaviour
{
    [SerializeField] WeaponModifierDefinition moduleDefinition;
    [SerializeField] float rotationSpeed = 90f;
    [SerializeField] bool destroyOnPickup = true;

    public WeaponModifierDefinition ModuleDefinition => moduleDefinition;

    void Reset()
    {
        var trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.isTrigger = true;
    }

    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (moduleDefinition == null || other == null)
            return;

        var weapon = other.GetComponentInParent<RaycastShooting>();
        if (weapon == null)
            return;

        weapon.AddModifier(moduleDefinition);

        if (destroyOnPickup)
            Destroy(gameObject);
    }
}
