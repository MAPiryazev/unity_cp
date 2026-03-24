using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 3.5f;
    [SerializeField] float gravity = -30f;
    [SerializeField] Camera aimCamera;
    [Tooltip("Degrees per second — max turn speed toward cursor.")]
    [SerializeField] float aimTurnSpeedDegrees = 540f;

    CharacterController _characterController;
    Vector3 _verticalVelocity;
    static readonly Plane GroundPlane = new Plane(Vector3.up, Vector3.zero);

    void Awake() => _characterController = GetComponent<CharacterController>();

    void Update()
    {
        ReadMoveInput(out float x, out float z);
        var direction = new Vector3(x, 0f, z);
        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        var move = direction * (moveSpeed * Time.deltaTime);
        _characterController.Move(move);

        if (_characterController.isGrounded && _verticalVelocity.y < 0f)
            _verticalVelocity.y = -2f;

        _verticalVelocity.y += gravity * Time.deltaTime;
        _characterController.Move(_verticalVelocity * Time.deltaTime);

        RotateTowardAimPoint();
    }

    void RotateTowardAimPoint()
    {
        var cam = aimCamera != null ? aimCamera : Camera.main;
        if (cam == null)
            return;

        if (!TryGetPointerScreenPosition(out Vector2 screenPos))
            return;

        var ray = cam.ScreenPointToRay(screenPos);
        if (!GroundPlane.Raycast(ray, out float enter))
            return;

        var hit = ray.GetPoint(enter);
        var toFlat = hit - transform.position;
        toFlat.y = 0f;
        if (toFlat.sqrMagnitude < 0.0001f)
            return;

        var target = Quaternion.LookRotation(toFlat.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            target,
            aimTurnSpeedDegrees * Time.deltaTime);
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

    static Keyboard ResolveKeyboard()
    {
        var kb = Keyboard.current;
        if (kb != null)
            return kb;

        foreach (var device in InputSystem.devices)
        {
            if (device is Keyboard k)
                return k;
        }

        return null;
    }

    void ReadMoveInput(out float x, out float z)
    {
        x = 0f;
        z = 0f;

        var kb = ResolveKeyboard();
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
                x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
                x += 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
                z += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
                z -= 1f;
            if (x != 0f || z != 0f)
                return;
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        x = Input.GetAxisRaw("Horizontal");
        z = Input.GetAxisRaw("Vertical");
        if (x != 0f || z != 0f)
            return;
#endif

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            x += 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            z += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            z -= 1f;
    }
}
