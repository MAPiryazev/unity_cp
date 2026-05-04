using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ConfigurableJoint))]
public class PhysicalBodyPart : MonoBehaviour {

    [SerializeField] private Transform _target;
    [SerializeField, HideInInspector] private ConfigurableJoint _joint;
    private Quaternion _startRotation;

	private void OnValidate()
	{
        _joint = GetComponent<ConfigurableJoint>();
    }

	void Start() {
        _startRotation = transform.localRotation;
    }

    void FixedUpdate() {
        _joint.targetRotation = Quaternion.Inverse(_target.localRotation) * _startRotation;
    }

}
