using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/// <summary>
/// Хитскан top-down: направление — от дула к точке на полу под курсором (горизонтальный луч на <see cref="maxRange"/>).
/// Первое попадание по <see cref="lineOfFireLayers"/> (стены Default + враги Enemy) останавливает луч; урон только при <see cref="Health"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class RaycastShooting : MonoBehaviour
{
    public enum FireMode
    {
        /// <summary>Один выстрел на нажатие (ЛКМ).</summary>
        SemiAutomatic,
        /// <summary>Удержание ЛКМ — очередь с ограничением по <see cref="shotsPerSecond"/>.</summary>
        Automatic
    }

    [SerializeField] Camera aimCamera;
    [SerializeField] float damage = 10f;
    [SerializeField] float maxRange = 200f;
    [Tooltip("Выстрелов в секунду (режим Automatic) или максимальная скорость кликов (Semi).")]
    [SerializeField] float shotsPerSecond = 8f;
    [SerializeField] FireMode fireMode = FireMode.SemiAutomatic;
    [Tooltip("Слои, с которыми сталкивается луч: Default (стены/пол при необходимости) + Enemy. Без слоя Player — иначе луч упирается в себя.")]
    [FormerlySerializedAs("enemyLayers")]
    [SerializeField] LayerMask lineOfFireLayers;
    [Tooltip("Триггерные коллайдеры: Collide — учитывать (частые hitbox'ы врагов). Ignore — только не-триггеры.")]
    [SerializeField] QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;
    [Tooltip("Не дать двум выстрелам в один кадр (дубли ввода / несколько источников).")]
    [SerializeField] bool preventSameFrameDoubleShot = true;

    [Header("Feedback")]
    [SerializeField] bool showTracer = true;
    [SerializeField] float tracerDuration = 0.07f;
    [SerializeField] float tracerWidth = 0.04f;
    [SerializeField] Color tracerColor = new Color(1f, 0.92f, 0.2f, 0.95f);
    [SerializeField] Transform tracerOrigin;
    [SerializeField] float tracerOriginHeight = 0.55f;
    [SerializeField] AudioClip fireSound;
    [SerializeField] [Range(0f, 1f)] float fireSoundVolume = 0.45f;

    static readonly Plane GroundPlane = new Plane(Vector3.up, Vector3.zero);

    float _nextShotTime;
    int _lastShotFrame = -1;
    LineRenderer _line;
    Coroutine _tracerRoutine;
    AudioSource _audio;

    void Awake()
    {
        if (lineOfFireLayers.value == 0)
            lineOfFireLayers = LayerMask.GetMask("Default", "Enemy");
        else
        {
            // Старые пресеты только с Enemy — добавляем стены (ArenaBootstrap кладёт их на Default).
            var enemyOnly = LayerMask.GetMask("Enemy");
            if (lineOfFireLayers.value == enemyOnly)
                lineOfFireLayers |= LayerMask.GetMask("Default");
        }

        shotsPerSecond = Mathf.Max(0.01f, shotsPerSecond);

        _audio = GetComponent<AudioSource>();
        if (_audio == null)
            _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.spatialBlend = 0f;
    }

    void Update()
    {
        if (!ShouldFireThisUpdate())
            return;

        var gap = 1f / shotsPerSecond;
        if (Time.time < _nextShotTime)
            return;

        if (preventSameFrameDoubleShot && Time.frameCount == _lastShotFrame)
            return;

        _nextShotTime = Time.time + gap;
        _lastShotFrame = Time.frameCount;
        FireOnce();
    }

    bool ShouldFireThisUpdate()
    {
        return fireMode == FireMode.Automatic ? IsFireHeld() : WasFirePressedThisFrame();
    }

    static bool WasFirePressedThisFrame()
    {
        var mouse = Mouse.current;
        if (mouse != null)
            return mouse.leftButton.wasPressedThisFrame;

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    static bool IsFireHeld()
    {
        var mouse = Mouse.current;
        if (mouse != null)
            return mouse.leftButton.isPressed;

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButton(0);
#else
        return false;
#endif
    }

    void FireOnce()
    {
        var cam = aimCamera != null ? aimCamera : Camera.main;
        if (cam == null)
            return;

        if (!TryGetPointerScreenPosition(out var screenPos))
            return;

        if (!TryGetGroundPointUnderCursor(cam, screenPos, out var groundAim))
            return;

        var muzzle = GetMuzzleWorld();
        var dir = new Vector3(groundAim.x - muzzle.x, 0f, groundAim.z - muzzle.z);
        if (dir.sqrMagnitude < 1e-6f)
            return;

        dir.Normalize();

        Vector3 tracerEnd;
        if (Physics.Raycast(muzzle, dir, out var hit, maxRange, lineOfFireLayers, triggerInteraction))
        {
            ApplyDamageIfAny(hit.collider, damage);
            tracerEnd = hit.point;
        }
        else
        {
            tracerEnd = muzzle + dir * maxRange;
        }

        PlayShootFeedback(tracerEnd);
    }

    bool TryGetGroundPointUnderCursor(Camera cam, Vector2 screenPixel, out Vector3 groundPoint)
    {
        groundPoint = default;
        var ray = BuildAimRay(cam, screenPixel);
        if (!GroundPlane.Raycast(ray, out float enter))
            return false;

        groundPoint = ray.GetPoint(enter);
        return true;
    }

    Vector3 GetMuzzleWorld()
    {
        if (tracerOrigin != null)
            return tracerOrigin.position;
        return transform.position + Vector3.up * tracerOriginHeight;
    }

    void PlayShootFeedback(Vector3 worldEnd)
    {
        if (fireSound != null && _audio != null)
            _audio.PlayOneShot(fireSound, fireSoundVolume);

        if (!showTracer)
            return;

        var start = GetMuzzleWorld();

        if (_tracerRoutine != null)
            StopCoroutine(_tracerRoutine);
        _tracerRoutine = StartCoroutine(TracerRoutine(start, worldEnd));
    }

    IEnumerator TracerRoutine(Vector3 start, Vector3 end)
    {
        EnsureLineRenderer();
        _line.enabled = true;
        _line.SetPosition(0, start);
        _line.SetPosition(1, end);
        yield return new WaitForSeconds(tracerDuration);
        _line.enabled = false;
        _tracerRoutine = null;
    }

    void EnsureLineRenderer()
    {
        if (_line != null)
            return;

        _line = gameObject.AddComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.widthMultiplier = tracerWidth;
        _line.numCapVertices = 4;
        _line.numCornerVertices = 2;
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows = false;
        _line.useWorldSpace = true;
        _line.startColor = tracerColor;
        _line.endColor = tracerColor;
        _line.material = CreateTracerMaterial();
        _line.enabled = false;
    }

    static Material CreateTracerMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", Color.white);
        else if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", Color.white);
        return mat;
    }

    static void ApplyDamageIfAny(Collider collider, float damageAmount)
    {
        if (collider == null)
            return;

        var health = collider.GetComponent<Health>() ?? collider.GetComponentInParent<Health>();
        if (health != null)
            health.TakeDamage(damageAmount);
    }

    static Ray BuildAimRay(Camera cam, Vector2 screenPixel)
    {
        var rect = cam.pixelRect;
        var nx = (screenPixel.x - rect.x) / rect.width;
        var ny = (screenPixel.y - rect.y) / rect.height;
        if (nx >= 0f && nx <= 1f && ny >= 0f && ny <= 1f)
            return cam.ViewportPointToRay(new Vector3(nx, ny, 0f));

        return cam.ScreenPointToRay(new Vector3(screenPixel.x, screenPixel.y, 0f));
    }

    static bool TryGetPointerScreenPosition(out Vector2 screenPos)
    {
        var mouse = Mouse.current;
        if (mouse != null)
        {
            screenPos = mouse.position.ReadValue();
            return true;
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        screenPos = Input.mousePosition;
        return true;
#else
        screenPos = default;
        return false;
#endif
    }
}
