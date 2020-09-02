using System;
using System.Collections;
using UnityEngine;

public abstract class GameEntity : MonoBehaviour
{
	[Header("Entity Attributes")]
	[SerializeField] protected int strength;
	[SerializeField] protected int health;
	[SerializeField] protected float lingerTimeBeforeDeath;
	[Space]
	[SerializeField] private AudioClip _damageSFX = default;
	[SerializeField] private AudioClip _dieSFX = default;

	new public Transform transform { get; private set; }
	public GameManager gameManager { get; protected set; }
	public Tile currentTile { get; protected set; }
	public int currentHealth { get; protected set; }
	public bool isBeingThrown { get; protected set; }

	public int GetStrength() => strength;

	public event Action<GameEntity> onDamaged;
	public event Action<GameEntity> onWillDie;

	protected virtual void Awake()
	{
		transform = base.transform;
	}

	public virtual void Initialize(GameManager gameManager, Tile tile)
	{
		this.gameManager = gameManager;

		SetTile(tile);

		currentHealth = health;
	}

	public void SetTile(Tile tile)
	{
		currentTile = tile;

		if (tile != null)
		{
			tile.Occupy(this);
			transform.position = tile.basePosition;
		}
	}

	public virtual void Throw(Tile throwerTile)
	{
		StartCoroutine(Throw());

		IEnumerator Throw()
		{
			isBeingThrown = true;

			var speed = 5;

			var origin = currentTile.basePosition;
			var target = 2 * origin - throwerTile.basePosition;

			var targetTile = gameManager.GetTile(2 * currentTile.gridX - throwerTile.gridX, 2 * currentTile.gridY - throwerTile.gridY);
			if (targetTile != null)
			{
				currentTile.Vacate(this);

				var vector = target - origin;
				var distance = vector.magnitude;
				var travelled = 0f;
				var hasCheckedTargetOccupant = false;

				while (travelled < distance)
				{
					transform.position = Vector3.Lerp(origin, target, travelled / distance);
					travelled += speed * Time.deltaTime;

					if (!hasCheckedTargetOccupant && travelled > 0.3f * distance)
					{
						if (currentHealth > 0)
						{
							if (targetTile.isOccupied)
							{
								targetTile.occupant.Throw(currentTile);
							}
						}
					}

					yield return null;
				}

				transform.position = target;

				yield return new WaitForSeconds(0.1f);

				if (currentHealth > 0)
				{
					currentTile = targetTile;
					currentTile.Occupy(this);
				}
			}

			isBeingThrown = false;
		}
	}

	public virtual void Damage(int attackStrength)
	{
		currentHealth -= Mathf.Max(1, attackStrength - strength);
		gameManager.soundController.PlaySoundEffect(_damageSFX);

		if (currentHealth <= 0)
		{
			if (!isBeingThrown)
			{
				currentTile.Vacate(this);
			}

			Die();
		}
	}

	protected virtual void Die()
	{
		onWillDie?.Invoke(this);
		gameManager.soundController.PlaySoundEffect(_dieSFX);

		if (lingerTimeBeforeDeath == 0)
		{
			Despawn();
		}
		else
		{
			StartCoroutine(LingerThenDespawn());
		}


		void Despawn()
		{
			gameManager.Despawn(gameObject);
		}

		IEnumerator LingerThenDespawn()
		{
			yield return new WaitForSeconds(lingerTimeBeforeDeath);
			while (transform.position.y > -2f)
			{
				transform.position += Vector3.down * Time.deltaTime;
				yield return null;
			}

			Despawn();
		}
	}

	private void OnMouseDown()
	{
		gameManager.Select(this);
	}
}
