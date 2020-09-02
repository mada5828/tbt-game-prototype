using UnityEngine;

public static class GroundPointUtilities
{
	public static Vector3 GetCameraForwardGroundPoint(Camera cam)
	{
		var cameraTransform = cam.transform;
		var downwardAngle = cameraTransform.localEulerAngles.x;
		var vectorToGround = cameraTransform.forward * (cameraTransform.position.y / Mathf.Cos(Mathf.Deg2Rad * (90f - downwardAngle)));

		return cameraTransform.position + vectorToGround;
	}

	public static Vector3 GetPointerGroundPoint(Camera cam)
	{
		var pointerRay = cam.ScreenPointToRay(Input.mousePosition);
		var downwardAngle = Vector3.Angle(pointerRay.direction, Vector3.down);
		var vectorToGround = pointerRay.direction * (cam.transform.position.y / Mathf.Cos(Mathf.Deg2Rad * (downwardAngle)));

		return pointerRay.origin + vectorToGround;
	}
}