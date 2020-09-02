using UnityEngine;
using UnityEngine.Events;

public class OnEnabledAction : MonoBehaviour
{
	public UnityEvent actions;

	private void OnEnable()
	{
		if (actions != null)
		{
			actions.Invoke();
		}
	}
}
