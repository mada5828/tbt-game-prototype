using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerAction
{
	Cancel,
	Move,
	Punch,
	Shoot
}

public class Adam : GameCharacter
{
	[Space]
	public int moveReach;
	[Space]
	public GameObject punchEffect;
	public GameObject shootEffect;
	[Space]
	public Fireball fireballPrefab;
	public float fireballSpeed;
	[Space]
	public AudioClip punchChargeSFX;
	public AudioClip shootChargeSFX;

	private UiController _uiController;
	private bool _isInTurn;
	private PlayerAction _playerAction;
	private GameEntity _selectedEntity;

	public override void Initialize(GameManager gameManager, Tile tile)
	{
		base.Initialize(gameManager, tile);

		_uiController = gameManager.uiController;
		_uiController.onChoosePlayerAction += OnChoosePlayerAction;

		gameManager.onSelectEntity += OnSelectEntity;
	}

	private void OnSelectEntity(GameEntity selection)
	{
		if (_isInTurn)
		{
			_selectedEntity = selection;
		}
	}

	private void OnChoosePlayerAction(PlayerAction playerAction)
	{
		if (_isInTurn)
		{
			_playerAction = playerAction;
		}
	}

	public override void Damage(int attackStrength)
	{
		base.Damage(attackStrength);
	}

	protected override void Die()
	{
		gameManager.LoseGame();
	}

	public override void StartTurn()
	{
		StopAllCoroutines();
		StartCoroutine(HandlePlayerControl());
	}

	private IEnumerator HandlePlayerControl()
	{
		_isInTurn = true;
		_playerAction = PlayerAction.Cancel;

		gameManager.RecalculateAdamMoveTargets();

		while (_isInTurn)
		{
			switch (_playerAction)
			{
				case PlayerAction.Cancel:
					yield return HandleActionChoice();
					break;

				case PlayerAction.Move:
					yield return HandleMove();
					break;

				case PlayerAction.Punch:
#if UNITY_EDITOR
					if (Input.GetKey(KeyCode.LeftAlt))
					{
						FindObjectOfType<Hellguard>().Damage(1);
						gameManager.FinishTurn(this);
						_isInTurn = false;
						break;
					}
#endif
					yield return HandlePunch();
					break;

				case PlayerAction.Shoot:
					yield return HandleShoot();
					break;

				default:
					break;
			}

			yield return null;
		}

		IEnumerator HandleActionChoice()
		{
			gameManager.ClearHighlights();
			_uiController.ShowPlayerControlPanel();						
			yield return new WaitUntil(() => _playerAction != PlayerAction.Cancel);
			_uiController.HidePlayerControlPanel();
			_uiController.ShowPlayerCancelButton();
		}

		IEnumerator HandleMove()
		{
			var currentPath = new List<Tile>();
			var lastPointedTile = currentTile;

			while (Application.isPlaying)
			{
				if (_playerAction == PlayerAction.Cancel)
				{
					_uiController.HidePlayerCancelButton();
					yield break;
				}

				var currentPointedTile = gameManager.GetPointedTile();
				if (currentPointedTile != lastPointedTile)
				{
					lastPointedTile = currentPointedTile;

					currentPath.Clear();
					currentPath.AddRange(gameManager.CalculateAdamPath());

					gameManager.HighlightAdamMoveTargetsAndPath(currentPath);
				}

				if (currentPath.Count != 0 && Input.GetMouseButtonDown(0))
				{
					break;
				}

				yield return null;
			}

			gameManager.ClearHighlights();
			_uiController.HidePlayerCancelButton();

			yield return Move(currentPath);

			currentTile.Vacate(this);
			currentTile = currentPath[currentPath.Count - 1];
			currentTile.Occupy(this);

			_isInTurn = false;
			gameManager.FinishTurn(this);
		}

		IEnumerator HandlePunch()
		{
			var currentTargets = new List<Tile>();
			var lastPointedTile = currentTile;

			currentTargets.AddRange(GridUtilities.GetTilesInRange(currentTile, 1, true).FindAll(x => x.isOccupied && x.occupant != this));
			gameManager.HighlightAdamActionTargets(currentTargets, default(Tile));
			gameManager.ActivateEntityColliders(currentTargets);

			_selectedEntity = null;

			while (Application.isPlaying)
			{
				if (_playerAction == PlayerAction.Cancel)
				{
					_uiController.HidePlayerCancelButton();
					yield break;
				}

				var currentPointedTile = gameManager.GetPointedTile();
				if (currentPointedTile != lastPointedTile)
				{
					gameManager.HighlightAdamActionTargets(currentTargets, currentPointedTile);
					lastPointedTile = currentPointedTile;
				}

				if (_selectedEntity != null)
				{
					if (currentTargets.Contains(_selectedEntity.currentTile))
					{
						break;
					}

					_selectedEntity = null;
				}

				yield return null;
			}

			gameManager.ClearHighlights();
			_uiController.HidePlayerCancelButton();

			transform.LookAt(_selectedEntity.currentTile.basePosition, Vector3.up);

			punchEffect.SetActive(false);
			yield return null;
			punchEffect.SetActive(true);

			gameManager.soundController.PlaySoundEffect(punchChargeSFX);

			yield return new WaitForSeconds(0.5f);

			gameManager.soundController.PlaySoundEffect(meleeSFX);

			if (_selectedEntity is Boulder)
			{
				var targetThrowTile = gameManager.GetTile(2 * _selectedEntity.currentTile.gridX - currentTile.gridX, 2 * _selectedEntity.currentTile.gridY - currentTile.gridY);
				if (targetThrowTile != null)
				{
					_selectedEntity.Throw(currentTile);
				}
			}
			else
			{
				_selectedEntity.Throw(currentTile);
				_selectedEntity.Damage(strength);
			}

			gameManager.ShakeCamera(0.8f, 1.5f, 0.26f);
			//gameManager.SkipFrame(10);

			_selectedEntity = null;

			yield return new WaitForSeconds(0.5f);

			_isInTurn = false;
			gameManager.FinishTurn(this);
		}

		IEnumerator HandleShoot()
		{
			var currentTargets = new List<Tile>();
			var aimedTargets = new List<Tile>();
			var lastPointedTile = currentTile;

			var deltaX = 0;
			var deltaY = 0;

			AddTargets(currentTargets, 1, 0);
			AddTargets(currentTargets, 0, -1);
			AddTargets(currentTargets, -1, 0);
			AddTargets(currentTargets, 0, 1);

			while (Application.isPlaying)
			{
				if (_playerAction == PlayerAction.Cancel)
				{
					_uiController.HidePlayerCancelButton();
					yield break;
				}

				var currentPointedTile = gameManager.GetPointedTile();
				if (currentPointedTile != lastPointedTile)
				{
					lastPointedTile = currentPointedTile;

					if (currentPointedTile != currentTile && currentTargets.Contains(currentPointedTile))
					{
						deltaX = currentPointedTile.gridX - currentTile.gridX;
						deltaY = currentPointedTile.gridY - currentTile.gridY;

						deltaX = deltaX == 0 ? 0 : deltaX / Mathf.Abs(deltaX);
						deltaY = deltaY == 0 ? 0 : deltaY / Mathf.Abs(deltaY);

						aimedTargets.Clear();
						AddTargets(aimedTargets, deltaX, deltaY);

						gameManager.HighlightAdamActionTargets(currentTargets, aimedTargets);
					}
					else
					{
						gameManager.HighlightAdamActionTargets(currentTargets, GridUtilities.EmptyTileList);
					}
				}

				if (currentTargets.Contains(currentPointedTile) && Input.GetMouseButtonDown(0))
				{
					break;
				}

				yield return null;
			}

			gameManager.ClearHighlights();
			_uiController.HidePlayerCancelButton();

			transform.LookAt(lastPointedTile.basePosition, Vector3.up);

			shootEffect.SetActive(false);
			yield return null;
			shootEffect.SetActive(true);

			gameManager.soundController.PlaySoundEffect(shootChargeSFX);

			yield return new WaitForSeconds(1f);

			gameManager.soundController.PlaySoundEffect(shootSFX);

			var fireball = Instantiate(fireballPrefab, currentTile.basePosition, Quaternion.identity);
			gameManager.ShakeCamera(1f, 1.6f, 0.28f);

			yield return fireball.Shoot(this, currentTile, deltaX, deltaY, fireballSpeed);
			yield return new WaitForSeconds(0.2f);

			_isInTurn = false;
			gameManager.FinishTurn(this);


			void AddTargets(List<Tile> targetList, int directionX, int directionY)
			{
				Tile head = gameManager.GetTile(currentTile.gridX + directionX, currentTile.gridY + directionY);
				while (head != null)
				{
					targetList.Add(head);
					head = gameManager.GetTile(head.gridX + directionX, head.gridY + directionY);
				}
			}
		}
	}
}
