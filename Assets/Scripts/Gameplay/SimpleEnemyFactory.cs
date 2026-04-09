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
        var enemyRoot = new GameObject(definition != null ? definition.name : "Enemy");
        enemyRoot.transform.position = position;
        enemyRoot.layer = ResolveEnemyLayer();

		var health = enemyRoot.AddComponent<Health>();
        health.SetMaxHealth(definition != null ? definition.MaxHealth : 25f);

        var controller = enemyRoot.AddComponent<SimpleEnemyController>();
        controller.Initialize(target, definition);

        var healthBar = enemyRoot.AddComponent<WorldHealthBar>();
        healthBar.SetOffset(new Vector3(0f, 2f, 0f));

        var visual = GameObject.Instantiate(definition.Prefab, enemyRoot.transform);
        visual.name = "Visual";
        visual.layer = enemyRoot.layer;

        return enemyRoot;
    }

    static int ResolveEnemyLayer()
    {
        var layer = LayerMask.NameToLayer("Enemy");
        return layer >= 0 ? layer : 0;
    }
}
