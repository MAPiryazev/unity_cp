using UnityEngine;

public static class SimpleEnemyFactory
{
    // Fallback values matching the original hardcoded setup.
    const float DefaultColliderRadius = 0.45f;
    const float DefaultColliderHeight = 2f;
    static readonly Vector3 DefaultVisualScale = new Vector3(0.9f, 1f, 0.9f);
    static readonly Color DefaultTintColor = new Color(0.85f, 0.2f, 0.2f, 1f);

    public static GameObject CreateEnemy(Vector3 position, Transform target, EnemyDefinition definition)
    {
        float radius = definition != null ? definition.ColliderRadius : DefaultColliderRadius;
        float height = definition != null ? definition.ColliderHeight : DefaultColliderHeight;
        Vector3 visualScale = definition != null ? definition.VisualLocalScale : DefaultVisualScale;

        var enemyRoot = new GameObject(definition != null ? definition.name : "Enemy");
        enemyRoot.transform.position = position;
        enemyRoot.layer = ResolveEnemyLayer();

        var collider = enemyRoot.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, height * 0.5f, 0f);
        collider.radius = radius;
        collider.height = height;

        var health = enemyRoot.AddComponent<Health>();
        health.SetMaxHealth(definition != null ? definition.MaxHealth : 25f);

        var controller = enemyRoot.AddComponent<SimpleEnemyController>();
        controller.Initialize(target, definition);

        var healthBar = enemyRoot.AddComponent<WorldHealthBar>();
        healthBar.SetOffset(new Vector3(0f, height + 0.3f, 0f));

        var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual";
        visual.layer = enemyRoot.layer;
        visual.transform.SetParent(enemyRoot.transform, false);
        // Place visual so it sits on the ground: center at half the collider height.
        visual.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
        visual.transform.localScale = visualScale;

        var visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
            Object.Destroy(visualCollider);

        var renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (definition != null && definition.VisualMaterial != null)
            {
                renderer.material = Object.Instantiate(definition.VisualMaterial);
            }
            else
            {
                Color tint = definition != null ? definition.TintColor : DefaultTintColor;
                renderer.material.color = tint;
            }
        }

        return enemyRoot;
    }

    static int ResolveEnemyLayer()
    {
        var layer = LayerMask.NameToLayer("Enemy");
        return layer >= 0 ? layer : 0;
    }
}
