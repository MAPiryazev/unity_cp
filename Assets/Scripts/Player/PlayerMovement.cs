using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 3.5f;
    [SerializeField] float gravity = -30f;
    [SerializeField] Camera aimCamera;
    [SerializeField] Animator animator;
    [Tooltip("Degrees per second — max turn speed toward cursor.")]
    [SerializeField] float aimTurnSpeedDegrees = 540f;

    CharacterController _characterController;
    ZoneEffectReceiver _zoneEffects;
    Vector3 _verticalVelocity;

	private IEnumerator Start()
	{
        for (float t = 0; t < 0.25f; t += Time.deltaTime)
		{
            transform.position += Vector3.forward * Time.deltaTime;
            yield return null;
        }
	}

	void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _zoneEffects = GetComponent<ZoneEffectReceiver>();

        // `GameplayZone` listens for `OnTriggerEnter(Collider)`. `CharacterController` is not a `Collider`,
        // so we add a lightweight trigger collider to the player.
        EnsureTriggerColliderForZones();
    }

    void EnsureTriggerColliderForZones()
    {
        if (_characterController == null)
            return;

        // If you already have a collider on the player, don't duplicate it.
        var existingCollider = GetComponent<Collider>();
        if (existingCollider != null)
            return;

        var capsule = gameObject.AddComponent<CapsuleCollider>();
        capsule.isTrigger = true;
        capsule.radius = _characterController.radius;
        capsule.height = _characterController.height;
        capsule.center = _characterController.center;
        capsule.direction = 1; // Y axis
    }

    void Update()
    {
        ReadMoveInput(out float x, out float z);
        var direction = new Vector3(x, 0f, z);
        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        animator.SetFloat("speed", direction.sqrMagnitude);

        var move = direction * (GetEffectiveMoveSpeed() * Time.deltaTime);
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

        if (!TopDownAimUtility.TryGetPointerScreenPosition(out Vector2 screenPos))
            return;

        if (!TopDownAimUtility.TryGetGroundPointUnderScreenPosition(cam, screenPos, out var groundPoint))
            return;

        if (!TopDownAimUtility.TryGetFlatDirection(transform.position, groundPoint, out var aimDirection))
            return;

        var target = Quaternion.LookRotation(aimDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, target, GetEffectiveAimTurnSpeed() * Time.deltaTime);
    }

    float GetEffectiveMoveSpeed()
    {
        if (_zoneEffects == null)
            _zoneEffects = GetComponent<ZoneEffectReceiver>();

        return moveSpeed * (_zoneEffects != null ? _zoneEffects.MovementMultiplier : 1f);
    }

    float GetEffectiveAimTurnSpeed()
    {
        if (_zoneEffects == null)
            _zoneEffects = GetComponent<ZoneEffectReceiver>();

        return aimTurnSpeedDegrees * (_zoneEffects != null ? _zoneEffects.AimTurnMultiplier : 1f);
    }

    void ReadMoveInput(out float x, out float z)
    {
        x = 0f;
        z = 0f;

        var kb = Keyboard.current;
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
