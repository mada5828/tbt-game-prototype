using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnTable : MonoBehaviour
{
	[HideInInspector] new public Transform transform;

	public float maxAngle = 5f;
	public float speed = 1f;

	private float _timer;

	private void Awake()
	{
		transform = base.transform;
	}

	private void Update()
	{
		_timer += Time.deltaTime;
		transform.localRotation = Quaternion.Euler(0f, Mathf.Sin(_timer), 0f);
	}
}
