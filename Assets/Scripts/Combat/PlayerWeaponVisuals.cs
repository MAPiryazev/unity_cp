using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RaycastShooting))]
public sealed class PlayerWeaponVisuals : MonoBehaviour
{
    [SerializeField] string holderName = "WeaponHolder";
    [SerializeField] string muzzleName = "Muzzle";

    RaycastShooting _shooting;
    Transform _holder;
    Transform _visual;
    Transform _muzzle;
    Renderer _renderer;
    GameObject _activePrefabSource;

    void Awake()
    {
        _shooting = GetComponent<RaycastShooting>();
        EnsureHolder();
    }

    void OnEnable()
    {
        if (_shooting == null)
            _shooting = GetComponent<RaycastShooting>();

        if (_shooting == null)
            return;

        _shooting.WeaponChanged += HandleWeaponChanged;
        ApplyWeaponVisual(_shooting.ResolvedWeapon, _shooting.ActiveWeaponDefinition);
    }

    void OnDisable()
    {
        if (_shooting != null)
            _shooting.WeaponChanged -= HandleWeaponChanged;
    }

    void HandleWeaponChanged(HitscanWeaponSettings settings)
    {
        ApplyWeaponVisual(settings, _shooting != null ? _shooting.ActiveWeaponDefinition : null);
    }

    void EnsureHolder()
    {
        if (_holder != null)
            return;

        _holder = transform.Find(holderName);
        if (_holder == null)
        {
            _holder = new GameObject(holderName).transform;
            _holder.SetParent(transform, false);
        }
    }

    void ApplyWeaponVisual(HitscanWeaponSettings settings, HitscanWeaponDefinition definition)
    {
        EnsureHolder();

        var desiredPrefab = definition != null ? definition.VisualPrefab : null;
        SwapVisualIfNeeded(desiredPrefab);

        if (_visual == null)
            return;

        _visual.localPosition = settings.VisualLocalPosition;
        _visual.localRotation = Quaternion.identity;
        _visual.localScale = settings.VisualLocalScale;

        if (_muzzle != null)
        {
            _muzzle.localPosition = settings.MuzzleLocalPosition;
            _muzzle.localRotation = Quaternion.identity;
        }

        if (_renderer == null)
            _renderer = _visual.GetComponentInChildren<Renderer>();

        ApplyRendererColor(_renderer, settings.VisualColor);

        if (_shooting != null)
            _shooting.SetTracerOrigin(_muzzle);
    }

    void SwapVisualIfNeeded(GameObject prefab)
    {
        if (_activePrefabSource == prefab && _visual != null)
            return;

        if (_visual != null)
        {
            if (Application.isPlaying)
                Destroy(_visual.gameObject);
            else
                DestroyImmediate(_visual.gameObject);
            _visual = null;
            _muzzle = null;
            _renderer = null;
        }

        if (prefab != null)
        {
            var instance = Instantiate(prefab, _holder, false);
            instance.name = prefab.name;

            foreach (var col in instance.GetComponentsInChildren<Collider>())
            {
                if (Application.isPlaying)
                    Destroy(col);
                else
                    DestroyImmediate(col);
            }

            _visual = instance.transform;
            _renderer = instance.GetComponentInChildren<Renderer>();
        }
        else
        {
            _visual = _holder.Find("WeaponVisual");
            if (_visual == null)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "WeaponVisual";
                cube.transform.SetParent(_holder, false);
                var col = cube.GetComponent<Collider>();
                if (col != null)
                {
                    if (Application.isPlaying)
                        Destroy(col);
                    else
                        DestroyImmediate(col);
                }
                _visual = cube.transform;
                _renderer = cube.GetComponent<Renderer>();
            }
            else
            {
                _renderer = _visual.GetComponent<Renderer>();
            }
        }

        _activePrefabSource = prefab;

        if (_visual != null)
        {
            _muzzle = _visual.Find(muzzleName);
            if (_muzzle == null)
            {
                _muzzle = new GameObject(muzzleName).transform;
                _muzzle.SetParent(_visual, false);
            }
        }
    }

    static void ApplyRendererColor(Renderer renderer, Color color)
    {
        if (renderer == null)
            return;

        var mat = Application.isPlaying ? renderer.material : renderer.sharedMaterial;
        if (mat == null)
            return;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else
            mat.color = color;
    }
}
