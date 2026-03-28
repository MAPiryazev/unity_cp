using UnityEngine;

[DefaultExecutionOrder(-100)]
public sealed class ArenaBootstrap : MonoBehaviour
{
    [SerializeField] float halfExtent = 12.5f;
    [SerializeField] float wallHeight = 3f;
    [SerializeField] float wallThickness = 0.5f;
    [SerializeField] Color floorColor = new Color(0.62f, 0.62f, 0.65f, 1f);
    [SerializeField] Color wallColor = new Color(0.72f, 0.72f, 0.75f, 1f);

    public float HalfExtent => halfExtent;

    void Awake()
    {
        EnsureGameplayHelpers();
        if (transform.Find("Floor") != null)
        {
            ApplyThemeToBakedArena();
            return;
        }

        BuildArena();
    }

    /// <summary>When Floor/Walls are saved in the scene, BuildArena is skipped — still apply light colors.</summary>
    void ApplyThemeToBakedArena()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;

            var name = renderer.gameObject.name;
            if (name.Contains("Wall"))
                ApplyColorToRenderer(renderer, wallColor);
            else if (name == "Floor")
                ApplyColorToRenderer(renderer, floorColor);
        }
    }

    static void ApplyColorToRenderer(Renderer renderer, Color color)
    {
        if (renderer == null)
            return;

        var mat = renderer.material;
        if (mat == null)
            return;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else
            mat.color = color;
    }

    void BuildArena()
    {
        float h = wallHeight * 0.5f;
        float span = halfExtent * 2f + wallThickness * 2f;

        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.layer = LayerMask.NameToLayer("Default");
        floor.transform.SetParent(transform, false);
        floor.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        floor.transform.localScale = new Vector3(span, 0.1f, span);
        ApplySurfaceColor(floor, floorColor);

        void AddWall(string name, Vector3 localPosition, Vector3 localScale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.layer = LayerMask.NameToLayer("Default");
            wall.transform.SetParent(transform, false);
            wall.transform.localPosition = localPosition;
            wall.transform.localScale = localScale;
            ApplySurfaceColor(wall, wallColor);
        }

        AddWall(
            "WallNorth",
            new Vector3(0f, h, halfExtent + wallThickness * 0.5f),
            new Vector3(span, wallHeight, wallThickness));

        AddWall(
            "WallSouth",
            new Vector3(0f, h, -halfExtent - wallThickness * 0.5f),
            new Vector3(span, wallHeight, wallThickness));

        AddWall(
            "WallEast",
            new Vector3(halfExtent + wallThickness * 0.5f, h, 0f),
            new Vector3(wallThickness, wallHeight, span));

        AddWall(
            "WallWest",
            new Vector3(-halfExtent - wallThickness * 0.5f, h, 0f),
            new Vector3(wallThickness, wallHeight, span));
    }

    void EnsureGameplayHelpers()
    {
        if (GetComponent<WeaponModuleSpawner>() == null)
            gameObject.AddComponent<WeaponModuleSpawner>();
        if (GetComponent<SurvivalGameFlow>() == null)
            gameObject.AddComponent<SurvivalGameFlow>();
        if (GetComponent<GameplayZoneSpawner>() == null)
            gameObject.AddComponent<GameplayZoneSpawner>();
    }

    static void ApplySurfaceColor(GameObject go, Color color)
    {
        var renderer = go.GetComponent<Renderer>();
        ApplyColorToRenderer(renderer, color);
    }
}
