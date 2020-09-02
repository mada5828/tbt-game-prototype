using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Hellguard : GameCharacter
{
	[Space]
	[Tooltip("The hellguard will get into berserk mode when its health reaches this value.")]
	public int movement = 3;
	public float speed = 5f;
	[Space]
	public AnimationCurve jumpCurve;
	public AnimationCurve moveCurve;
	public AnimationCurve meleeCurve;
	public TextMeshPro exclamationText;
	public Animator animator;
	[Space]
	public Fireball fireballPrefab;
	public float fireballSpeed = 10f;
	public GameObject shootEffect;
	public AudioClip shootChargeSFX;
	[Space]
	public int berserkHealthThreshold = 1;
	public GameObject berserkShootEffect;
	public AudioClip berserkShootChargeSFX;
	public AudioClip berserkShootSFX;

	private List<Tile> _patrolRoute = new List<Tile>();
	private int _nextPatrolIndex;
	private int _currentPatrolIndex;
	private int _direction;
	private bool _isOverridingRotation;

	private int _berserkDirectionIndex;
	private int _berserkDirectionStep;

	private static readonly List<Vector2Int> BerserkDirections = new List<Vector2Int>()
	{
		Vector2Int.up,
		Vector2Int.right,
		Vector2Int.down,
		Vector2Int.left,
	};

	protected override void Die()
	{
		base.Die();

		gameManager.FinishLevel();
	}

	public void InitializePatrol(int patrolRadius)
	{
		var coord = new Vector2Int(gameManager.centerTile.gridX + patrolRadius, gameManager.centerTile.gridY + patrolRadius);

		_patrolRoute.Clear();

		AddPatrolRouteTiles(Vector2Int.left);
		AddPatrolRouteTiles(Vector2Int.down);
		AddPatrolRouteTiles(Vector2Int.right);
		AddPatrolRouteTiles(Vector2Int.up);

		_currentPatrolIndex = Random.Range(0, _patrolRoute.Count);
		_direction = 1 - 2 * Random.Range(0, 2);
		_nextPatrolIndex = CalculateNextPatrolIndex();

		currentTile = _patrolRoute[_currentPatrolIndex];
		currentTile.Occupy(this);

		transform.position = currentTile.basePosition;

		HideExclamation();
		StartCoroutine(HandleTransformRotation());


		void AddPatrolRouteTiles(Vector2Int direction)
		{
			for (int i = 0; i < patrolRadius * 2; i++)
			{
				coord += direction;
				_patrolRoute.Add(gameManager.GetTile(coord.x, coord.y));
			}
		}
	}

	public Tile GetNextPatrolTile(int step)
	{
		var index = (_currentPatrolIndex + _direction * step + _patrolRoute.Count) % _patrolRoute.Count;
		return _patrolRoute[index];
	}

	public override void Damage(int attackStrength)
	{
		base.Damage(attackStrength);

		HideExclamation();

		if (currentHealth > 0)
		{
			animator.SetTrigger("Stun");
			gameManager.soundController.PlaySoundEffect(stunSFX);

			gameManager.uiController.playerShootButton.SetActive(true);
			gameManager.uiController.ShowMessage("You somehow gained the Hellguard's shooting power, cool!");
		}
	}

	public override void StartTurn()
	{
		HideExclamation();

		if (currentHealth > berserkHealthThreshold)
		{
			StartCoroutine(Move());
		}
		else
		{
			StartCoroutine(Berserk());
		}
	}

	public IEnumerator Move()
	{
		int correctionMoves = 0;
		if (_patrolRoute.Contains(currentTile))
		{
			var currentIndex = _patrolRoute.IndexOf(currentTile);
			if (currentIndex != _currentPatrolIndex)
			{
				_currentPatrolIndex = currentIndex;
				_nextPatrolIndex = CalculateNextPatrolIndex();
			}
		}
		else
		{
			correctionMoves = 1;

			var correctPatrolTile = _patrolRoute[_currentPatrolIndex];

			_nextPatrolIndex = _currentPatrolIndex;
			_currentPatrolIndex = CalculatePatrolIndex(_currentPatrolIndex, -_direction);

			shootEffect.SetActive(false);
			yield return null;
			shootEffect.SetActive(true);

			gameManager.soundController.PlaySoundEffect(shootChargeSFX);

			yield return new WaitForSeconds(1f);

			gameManager.soundController.PlaySoundEffect(shootSFX);

			var fireball = Instantiate(fireballPrefab, currentTile.basePosition, Quaternion.identity);
			gameManager.ShakeCamera(0.3f, 1.2f, 0.2f);

			yield return fireball.Shoot(this, currentTile, correctPatrolTile.gridX - currentTile.gridX, correctPatrolTile.gridY - currentTile.gridY, fireballSpeed);
			yield return new WaitForSeconds(1f);
		}

		for (int i = correctionMoves; i < movement; i++)
		{
			// Check target, then do a battle roar or turn around if attacking seems to lead to stopping next to adam
			if (_patrolRoute[_nextPatrolIndex].isOccupied && _patrolRoute[_nextPatrolIndex].occupant != this)
			{
				if (_patrolRoute[_nextPatrolIndex].occupant is Adam)
				{
					yield return ShowTemporaryExclamation("!!", 1f);
				}
				else
				{
					if (CheckLooksSafeToMove(_currentPatrolIndex, _nextPatrolIndex, 0))
					{
						yield return ShowTemporaryExclamation("!!", 1f);
					}
					else
					{
						yield return TurnAround();
					}
				}
			}
			else
			{
				if (!CheckLooksSafeToMove(_currentPatrolIndex, _nextPatrolIndex, movement - i - 1))
				{
					yield return TurnAround();
				}
			}

			// Cache needed variables
			var originTile = i == correctionMoves && correctionMoves == 1 ? currentTile : _patrolRoute[_currentPatrolIndex];
			var targetTile = _patrolRoute[_nextPatrolIndex];

			var originPosition = originTile.basePosition;
			var targetPosition = targetTile.basePosition;

			var curve = targetTile.isOccupied && targetTile.occupant != this ? meleeCurve : moveCurve;

			// Move
			gameManager.soundController.PlaySoundEffect(curve == meleeCurve ? meleeSFX : moveSFX);
			for (float t = 0f, d = 1f / speed; t < d; t += Time.deltaTime)
			{
				transform.position = Vector3.Lerp(originPosition, targetPosition, curve.Evaluate(t / d));
				yield return null;
			}

			transform.position = targetPosition;

			// Advance patrol index
			_currentPatrolIndex = _nextPatrolIndex;
			_nextPatrolIndex = CalculateNextPatrolIndex();

			// Handle melee aftermath
			if (targetTile.isOccupied && targetTile.occupant != this)
			{
				if (targetTile.occupant is Boulder)
				{
					StartCoroutine(ShowTemporaryExclamation("@_@", 3f));
					animator.SetTrigger("Stun");
					gameManager.soundController.PlaySoundEffect(stunSFX);
				}

				var occupant = targetTile.occupant;
				occupant.Throw(originTile);
				occupant.Damage(strength);

				gameManager.ShakeCamera(0.3f, 1.2f, 0.1f);
				gameManager.SkipFrame(10);

				break;
			}

			yield return new WaitForSeconds(0.1f);
		}

		currentTile.Vacate(this);
		currentTile = _patrolRoute[_currentPatrolIndex];
		currentTile.Occupy(this);

		gameManager.FinishTurn(this);


		IEnumerator TurnAround()
		{
			yield return ShowTemporaryExclamation("!", 0.5f);

			_direction *= -1;
			_nextPatrolIndex = CalculateNextPatrolIndex();
		}

		bool CheckLooksSafeToMove(int startPatrolIndex, int nextPatrolIndex, int extraMovesLeft)
		{
			var startTile = _patrolRoute[startPatrolIndex];
			var stopTile = _patrolRoute[nextPatrolIndex];
			var adamTile = stopTile.neighbors.Find(x => x.isOccupied && x.occupant == gameManager.adam);

			if (adamTile == null)
			{
				return true;
			}
			else
			{
				if (!stopTile.isOccupied)
				{
					// Check for corner trap
					if (extraMovesLeft == 0)
					{
						return false;
					}
					else if (extraMovesLeft == 1 || extraMovesLeft > 2)
					{
						return true;
					}
					else
					{
						if (_patrolRoute.Contains(adamTile))
						{
							return true;
						}
						else
						{
							return CheckLooksSafeToMove(CalculatePatrolIndex(nextPatrolIndex, _direction), CalculatePatrolIndex(nextPatrolIndex, _direction * 2), 0);
						}
					}
				}
				else
				{
					var startToStopDirectionX = stopTile.gridX - startTile.gridX;
					var stopToAdamDirectionX = adamTile.gridX - stopTile.gridX;
					var startToStopDirectionY = stopTile.gridY - startTile.gridY;
					var stopToAdamDirectionY = adamTile.gridY - stopTile.gridY;

					// Cannot see adam if adam is directly behind the melee target
					return startToStopDirectionX == stopToAdamDirectionX && startToStopDirectionY == stopToAdamDirectionY;
				}
			}
		}
	}

	private IEnumerator Berserk()
	{
		if (currentTile != gameManager.centerTile)
		{
			_isOverridingRotation = true;

			var previousTile = currentTile;

			var centerTile = gameManager.centerTile;
			var centerDirection = centerTile.basePosition - currentTile.basePosition;
			var targetRotation = Quaternion.LookRotation(centerDirection, Vector3.up);

			for (float t = 0f; t < 1f; t += Time.deltaTime)
			{
				transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
				yield return null;
			}

			yield return new WaitForSeconds(0.5f);

			currentTile.Vacate(this);

			for (float t = 0f, d = centerDirection.magnitude / speed * 0.5f; t < d; t += Time.deltaTime)
			{
				transform.position = Vector3.Lerp(currentTile.basePosition, centerTile.basePosition, t / d) + Vector3.up * jumpCurve.Evaluate(t / d);
				yield return null;
			}

			transform.position = centerTile.basePosition;
			gameManager.soundController.PlaySoundEffect(gameManager.dropSFX);
			gameManager.ShakeCamera(0.3f, 1.2f, 0.2f);

			currentTile = centerTile;

			for (int x = -1; x <= 1; x++)
			{
				for (int y = -1; y <= 1; y++)
				{
					if (x != 0 || y != 0)
					{
						var tile = gameManager.GetTile(centerTile.gridX + x, centerTile.gridY + y);
						if (tile.isOccupied)
						{
							tile.occupant.Damage(strength);
						}
					}
				}
			}

			currentTile.Occupy(this);

			_berserkDirectionStep = 1;
			_isOverridingRotation = false;

			SetDirectionTowards(previousTile);

			_berserkDirectionIndex = (_berserkDirectionIndex - _berserkDirectionStep + BerserkDirections.Count) % BerserkDirections.Count;
		}

		_berserkDirectionIndex = (_berserkDirectionIndex + _berserkDirectionStep + BerserkDirections.Count) % BerserkDirections.Count;

		var adamTile = gameManager.adam.currentTile;
		if (adamTile.gridX == currentTile.gridX || adamTile.gridY == currentTile.gridY)
		{
			yield return ShowTemporaryExclamation("!!", 0.5f);
			SetDirectionTowards(adamTile);
		}

		yield return new WaitForSeconds(0.5f);

		var activeFireballs = 3;
		var direction = BerserkDirections[_berserkDirectionIndex];

		StartCoroutine(ShootFireball(currentTile, true));
		foreach (var neighbor in currentTile.neighbors.FindAll(n => Mathf.Abs(n.gridX - currentTile.gridX) != Mathf.Abs(direction.x)))
		{
			StartCoroutine(ShootFireball(neighbor, false));
		}

		yield return new WaitWhile(() => activeFireballs > 0);
		yield return new WaitForSeconds(1f);

		gameManager.FinishTurn(this);


		IEnumerator ShootFireball(Tile origin, bool isMidHead)
		{
			berserkShootEffect.SetActive(false);
			yield return null;
			berserkShootEffect.SetActive(true);

			if (isMidHead)
			{
				gameManager.soundController.PlaySoundEffect(berserkShootChargeSFX);

				yield return new WaitForSeconds(1f);

				gameManager.soundController.PlaySoundEffect(berserkShootSFX);
				gameManager.ShakeCamera(1f, 1.3f, 0.35f);
			}
			else
			{
				yield return new WaitForSeconds(1f);
			}

			var fireball = Instantiate(fireballPrefab, origin.basePosition, Quaternion.identity);

			yield return fireball.Shoot(this, origin, direction.x, direction.y, fireballSpeed);

			activeFireballs -= 1;
		}

		void SetDirectionTowards(Tile targetTile)
		{
			var deltaX = targetTile.gridX - currentTile.gridX;
			var deltaY = targetTile.gridY - currentTile.gridY;

			int absDeltaX = Mathf.Abs(deltaX);
			int absDeltaY = Mathf.Abs(deltaY);

			if (absDeltaX == Mathf.Abs(deltaY))
			{
				_berserkDirectionIndex = BerserkDirections.IndexOf(Random.Range(0, 2) == 0 ? new Vector2Int(deltaX / absDeltaX, 0) : new Vector2Int(0, deltaY / absDeltaY));
			}
			else
			{
				_berserkDirectionIndex = BerserkDirections.IndexOf(absDeltaX > absDeltaY ? new Vector2Int(deltaX / absDeltaX, 0) : new Vector2Int(0, deltaY / absDeltaY));
			}
		}
	}

	private IEnumerator HandleTransformRotation()
	{
		while (Application.isPlaying)
		{
			if (!_isOverridingRotation)
			{
				if (currentHealth > berserkHealthThreshold)
				{
					transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_patrolRoute[_nextPatrolIndex].basePosition - transform.position, Vector3.up), 20f * Time.deltaTime);
				}
				else
				{
					var direction = BerserkDirections[_berserkDirectionIndex];
					transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(gameManager.GetTile(currentTile.gridX + direction.x, currentTile.gridY + direction.y).basePosition - transform.position, Vector3.up), 20f * Time.deltaTime);
				}
			}
			yield return null;
		}
	}

	private IEnumerator ShowTemporaryExclamation(string text, float time)
	{
		exclamationText.SetText(text);
		exclamationText.gameObject.SetActive(true);
		yield return new WaitForSeconds(time);
		exclamationText.gameObject.SetActive(false);
	}

	private void HideExclamation()
	{
		exclamationText.gameObject.SetActive(false);
	}

	private int CalculateNextPatrolIndex()
	{
		return (_currentPatrolIndex + _direction + _patrolRoute.Count) % _patrolRoute.Count;
	}

	private int CalculatePatrolIndex(int startIndex, int offset = 1)
	{
		return (startIndex + offset + _patrolRoute.Count) % _patrolRoute.Count;
	}
}
