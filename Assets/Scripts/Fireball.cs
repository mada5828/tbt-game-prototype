using System.Collections;
using UnityEngine;

public class Fireball : MonoBehaviour
{
	[SerializeField] private GameObject[] _fireEffectObjects = default;
	[SerializeField] private ParticleSystem _explodeEffect = default;
	[SerializeField] private AudioClip _explodeSFX = default;

	public IEnumerator Shoot(GameEntity shooter, Tile originTile, int directionX, int directionY, float fireballSpeed)
	{
		var previousHead = originTile;
		var head = originTile;
		var direction = new Vector3(directionX, 0f, directionY);

		var currentDistanceTravelled = 0.7f;
		var totalDistanceTravelled = currentDistanceTravelled;

		while (Application.isPlaying)
		{
			var delta = fireballSpeed * Time.deltaTime;

			currentDistanceTravelled += delta;
			totalDistanceTravelled += delta;

			transform.position = originTile.basePosition + direction * totalDistanceTravelled;

			if (currentDistanceTravelled > 1f)
			{
				previousHead = head;
				head = head.gameManager.GetTile(head.gridX + directionX, head.gridY + directionY);

				if (head != null)
				{
					if (head.isOccupied)
					{
						StartCoroutine(Explode());

						if (head.occupant is Boulder)
						{
							if (shooter is Adam)
							{
								head.occupant.Throw(previousHead);
							}
							else
							{
								head.occupant.Damage(shooter.GetStrength());
							}
						}
						else
						{
							head.occupant.Damage(shooter.GetStrength());
						}

						break;
					}
				}
				else
				{
					StartCoroutine(Explode());
					break;
				}

				currentDistanceTravelled -= 1f;
			}

			yield return null;
		}

		IEnumerator Explode()
		{
			shooter.gameManager.soundController.PlaySoundEffect(_explodeSFX);
			foreach (var fire in _fireEffectObjects) { fire.SetActive(false); }
			_explodeEffect.gameObject.SetActive(true);
			yield return new WaitWhile(_explodeEffect.IsAlive);
			Destroy(gameObject);
		}
	}
}
