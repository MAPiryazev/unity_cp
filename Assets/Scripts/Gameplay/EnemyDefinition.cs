using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Enemies/Enemy Definition", fileName = "EnemyDefinition")]
public sealed class EnemyDefinition : ScriptableObject
{
    [Header("Stats")]
    [SerializeField] float maxHealth = 25f;
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float contactDamage = 10f;

    [Header("Hitbox")]
    [Tooltip("CapsuleCollider radius. Default matches the standard enemy (0.45).")]
    [SerializeField] float colliderRadius = 0.45f;
    [Tooltip("CapsuleCollider height.")]
    [SerializeField] float colliderHeight = 2f;

    [Header("Visual")]
    [Tooltip("LocalScale of the visual capsule mesh. Default (0.9, 1, 0.9) matches standard enemy.")]
    [SerializeField] Vector3 visualLocalScale = new Vector3(0.9f, 1f, 0.9f);
    [Tooltip("Tint color applied to the primitive material when no Material override is set.")]
    [SerializeField] Color tintColor = new Color(0.85f, 0.2f, 0.2f, 1f);
    [Tooltip("Optional material override. When assigned, tintColor is ignored.")]
    [SerializeField] Material visualMaterial;

    public float MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public float ContactDamage => contactDamage;
    public float ColliderRadius => colliderRadius;
    public float ColliderHeight => colliderHeight;
    public Vector3 VisualLocalScale => visualLocalScale;
    public Color TintColor => tintColor;
    public Material VisualMaterial => visualMaterial;

    void OnEnable() => ClampValues();
    void OnValidate() => ClampValues();

    void ClampValues()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        contactDamage = Mathf.Max(0f, contactDamage);
        colliderRadius = Mathf.Max(0.1f, colliderRadius);
        colliderHeight = Mathf.Max(0.2f, colliderHeight);
        visualLocalScale = new Vector3(
            Mathf.Max(0.1f, visualLocalScale.x),
            Mathf.Max(0.1f, visualLocalScale.y),
            Mathf.Max(0.1f, visualLocalScale.z));
    }
}
