using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameCharacter : GameEntity
{
	public AudioClip moveSFX;
	public AudioClip meleeSFX;
	public AudioClip stunSFX;
	public AudioClip shootSFX;

	private void Start()
	{
		transform.localEulerAngles = new Vector3(0f, -205f, 0f);
	}

	protected IEnumerator Move(List<Tile> path)
	{
		for (int i = 0; i < path.Count; i++)
		{
			if (i != 0)
			{
				gameManager.soundController.PlaySoundEffect(moveSFX);
			}

			while (Vector3.SqrMagnitude(transform.position - path[i].basePosition) > 0.01f)
			{
				transform.position = Vector3.Lerp(transform.position, path[i].basePosition, Time.deltaTime * 20f);
				yield return null;
			}
						
			transform.position = path[i].basePosition;
			if (i != path.Count - 1) { transform.LookAt(path[i + 1].basePosition, Vector3.up); }

			yield return new WaitForSeconds(0.1f);
		}
	}

	public override void Damage(int attackStrength)
	{
		base.Damage(attackStrength);
	}

	public virtual void StartTurn()
	{
		gameManager.FinishTurn(this);
	}
}
