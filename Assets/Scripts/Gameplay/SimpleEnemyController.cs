using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Collider))]
public sealed class SimpleEnemyController : MonoBehaviour
{
    [SerializeField] EnemyDefinition enemyDefinition;
    [SerializeField] float moveSpeed = 2.75f;
    [SerializeField] float contactDamage = 10f;
    [SerializeField] float attackRange = 1.15f;
    [SerializeField] float attackCooldown = 0.75f;
    [SerializeField] string targetName = "Player";
    [SerializeField] bool allowSceneLookupFallback = true;
    [SerializeField] float targetLookupInterval = 0.5f;
    [SerializeField] float separationRadius = 1.05f;
    [SerializeField] float separationWeight = 1.35f;
    [SerializeField] LayerMask separationLayers;

    Transform _target;
    Collider _selfCollider;
    Health _health;
    ZoneEffectReceiver _zoneEffects;
    float _nextAttackTime;
    float _nextTargetLookupTime;

    public void Initialize(Transform target, EnemyDefinition definition)
    {
        _target = target;
        enemyDefinition = definition;
        ApplyDefinition();
    }

    void Awake()
    {
        _selfCollider = GetComponent<Collider>();
        _health = GetComponent<Health>();
        _zoneEffects = GetComponent<ZoneEffectReceiver>();
        ApplyDefinition();
        if (separationLayers.value == 0)
            separationLayers = LayerMask.GetMask("Enemy");
    }

    void Update()
    {
        if (!TryEnsureTarget())
            return;

        var toTarget = _target.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude <= attackRange * attackRange)
        {
            TryDealContactDamage();
            return;
        }

        var moveDirection = (toTarget.normalized + ComputeSeparationOffset()).normalized;
        transform.position += moveDirection * (GetEffectiveMoveSpeed() * Time.deltaTime);

        if (moveDirection.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);
    }

    void ApplyDefinition()
    {
        if (enemyDefinition == null)
            return;

        moveSpeed = enemyDefinition.MoveSpeed;
        contactDamage = enemyDefinition.ContactDamage;

        if (_health != null)
            _health.SetMaxHealth(enemyDefinition.MaxHealth);
    }

    void TryDealContactDamage()
    {
        if (Time.time < _nextAttackTime || _target == null)
            return;

        if (DamageUtility.TryApplyDamage(_target, new DamageInfo(GetEffectiveContactDamage(), _target.position, transform.forward, gameObject)))
            _nextAttackTime = Time.time + GetEffectiveAttackCooldown();
    }

    Transform ResolveTarget()
    {
        if (!string.IsNullOrWhiteSpace(targetName))
        {
            var namedTarget = GameObject.Find(targetName);
            if (namedTarget != null)
                return namedTarget.transform;
        }

        var player = FindFirstObjectByType<PlayerMovement>();
        return player != null ? player.transform : null;
    }

    bool TryEnsureTarget()
    {
        if (_target != null)
            return true;

        if (!allowSceneLookupFallback)
            return false;

        if (Time.time < _nextTargetLookupTime)
            return false;

        _nextTargetLookupTime = Time.time + Mathf.Max(0.1f, targetLookupInterval);
        _target = ResolveTarget();
        return _target != null;
    }

    Vector3 ComputeSeparationOffset()
    {
        if (separationRadius <= 0f || separationWeight <= 0f)
            return Vector3.zero;

        var center = transform.position + Vector3.up;
        var neighbors = Physics.OverlapSphere(center, separationRadius, separationLayers, QueryTriggerInteraction.Ignore);
        if (neighbors == null || neighbors.Length == 0)
            return Vector3.zero;

        Vector3 push = Vector3.zero;
        for (int i = 0; i < neighbors.Length; i++)
        {
            var other = neighbors[i];
            if (other == null || other == _selfCollider)
                continue;

            var otherTransform = other.transform;
            if (otherTransform == transform || otherTransform.IsChildOf(transform))
                continue;

            Vector3 away = transform.position - otherTransform.position;
            away.y = 0f;
            float distance = away.magnitude;
            if (distance <= 0.0001f || distance > separationRadius)
                continue;

            push += away.normalized * ((separationRadius - distance) / separationRadius);
        }

        return push * separationWeight;
    }

    float GetEffectiveMoveSpeed()
    {
        if (_zoneEffects == null)
            _zoneEffects = GetComponent<ZoneEffectReceiver>();

        return moveSpeed * (_zoneEffects != null ? _zoneEffects.MovementMultiplier : 1f);
    }

    float GetEffectiveContactDamage()
    {
        if (_zoneEffects == null)
            _zoneEffects = GetComponent<ZoneEffectReceiver>();

        return contactDamage * (_zoneEffects != null ? _zoneEffects.ContactDamageMultiplier : 1f);
    }

    float GetEffectiveAttackCooldown()
    {
        if (_zoneEffects == null)
            _zoneEffects = GetComponent<ZoneEffectReceiver>();

        return attackCooldown * (_zoneEffects != null ? _zoneEffects.AttackCooldownMultiplier : 1f);
    }
}
