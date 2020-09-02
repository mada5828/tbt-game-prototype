using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
	private Transform _cameraTransform;
	private Transform _transform;

	void Awake()
	{
		_cameraTransform = Camera.main.transform;
		_transform = transform;
	}

	private void LateUpdate()
	{
		_transform.forward = -_cameraTransform.forward;
	}
}
