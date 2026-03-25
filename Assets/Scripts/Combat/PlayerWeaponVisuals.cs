using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RaycastShooting))]
public sealed class PlayerWeaponVisuals : MonoBehaviour
{
    [SerializeField] string holderName = "WeaponHolder";
    [SerializeField] string visualName = "WeaponVisual";
    [SerializeField] string muzzleName = "Muzzle";

    RaycastShooting _shooting;
    Transform _holder;
    Transform _visual;
    Transform _muzzle;
    Renderer _renderer;

    void Awake()
    {
        _shooting = GetComponent<RaycastShooting>();
        BuildIfNeeded();
    }

    void OnEnable()
    {
        if (_shooting == null)
            _shooting = GetComponent<RaycastShooting>();

        if (_shooting == null)
            return;

        _shooting.WeaponChanged += ApplyWeaponVisual;
        _shooting.SetTracerOrigin(_muzzle);
        ApplyWeaponVisual(_shooting.ResolvedWeapon);
    }

    void OnDisable()
    {
        if (_shooting != null)
            _shooting.WeaponChanged -= ApplyWeaponVisual;
    }

    void BuildIfNeeded()
    {
        if (_holder == null)
        {
            _holder = transform.Find(holderName);
            if (_holder == null)
            {
                _holder = new GameObject(holderName).transform;
                _holder.SetParent(transform, false);
            }
        }

        if (_visual == null)
        {
            _visual = _holder.Find(visualName);
            if (_visual == null)
            {
                var visualObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualObject.name = visualName;
                visualObject.transform.SetParent(_holder, false);

                var collider = visualObject.GetComponent<Collider>();
                if (collider != null)
                    Destroy(collider);

                _visual = visualObject.transform;
                _renderer = visualObject.GetComponent<Renderer>();
            }
            else if (_renderer == null)
            {
                _renderer = _visual.GetComponent<Renderer>();
            }
        }

        if (_muzzle == null)
        {
            _muzzle = _visual.Find(muzzleName);
            if (_muzzle == null)
            {
                _muzzle = new GameObject(muzzleName).transform;
                _muzzle.SetParent(_visual, false);
            }
        }
    }

    void ApplyWeaponVisual(HitscanWeaponSettings settings)
    {
        BuildIfNeeded();

        _visual.localPosition = settings.VisualLocalPosition;
        _visual.localRotation = Quaternion.identity;
        _visual.localScale = settings.VisualLocalScale;
        _muzzle.localPosition = settings.MuzzleLocalPosition;
        _muzzle.localRotation = Quaternion.identity;

        if (_renderer == null)
            _renderer = _visual.GetComponent<Renderer>();

        if (_renderer != null)
            _renderer.material.color = settings.VisualColor;

        if (_shooting != null)
            _shooting.SetTracerOrigin(_muzzle);
    }
}
