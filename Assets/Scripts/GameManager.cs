using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
	public static readonly float GridOffsetValue = 0.5f;
	public static readonly Vector3 GridOffset = new Vector3(GridOffsetValue, 0f, GridOffsetValue);

	[SerializeField] private Camera _gameCamera = default;
	[SerializeField] private float _cameraInitialHeight = 20;
	[SerializeField] private float _cameraShakePerlinScale = 10f;
	[Space]
	[SerializeField] private SoundController _soundController = default;
	[SerializeField] private UiController _uiController = default;
	[SerializeField] private Color _adamActionTargetsHighlightColor = default;
	[SerializeField] private Color _adamMoveTargetsHighlightColor = default;
	[SerializeField] private Color _adamPathHighlightColor = default;
	[Space]
	[SerializeField] private float _fallHeight = 20f;
	[SerializeField] private float _initialFallSpeed = 30f;
	[SerializeField] private float _gravity = 10f;
	[Space]
	[SerializeField] private Adam _adamPrefab = default;
	[SerializeField] private Tile _tilePrefab = default;
	[SerializeField] private Transform _mapTilesContainer = default;
	[SerializeField] private Transform _gameEntitiesContainer = default;
	[SerializeField] private LevelLogicManager[] _levelLogicManagers = default;
	[Space]
	[SerializeField] private AudioClip _menuBGM = default;
	[SerializeField] private AudioClip _levelBGM = default;
	[SerializeField] private AudioClip _loseBGM = default;
	[SerializeField] private AudioClip _endBGM = default;
	[Space]
	[SerializeField] private AudioClip _dropSFX = default;

	private int _mapWidth;
	private int _mapLength;
	private int _level;
	private Vector3 _baseCameraPosition;
	private List<Tile> _tiles = new List<Tile>();
	private List<GameCharacter> _gameCharacters = new List<GameCharacter>();
	private List<GameObject> _instantiatedObjects = new List<GameObject>();
	private List<GameEntity> _gameEntities = new List<GameEntity>();
	private GameCharacter _activeCharacter;
	private GameCharacter _previousCharacter;

	public List<Tile> tiles => _tiles;
	public UiController uiController => _uiController;
	public SoundController soundController => _soundController;
	public AudioClip dropSFX => _dropSFX;
	public Adam adam { get; private set; }
	public Tile centerTile { get; private set; }

	public bool hasDroppedAdam { get; private set; }
	public bool hasFinishedInitiatingLevel { get; private set; }
	public bool isRunningGameLoop { get; private set; }
	public bool isPaused { get; private set; }

	#region Pause, Return, Exit

	public void TryTogglePause()
	{
		if (isRunningGameLoop)
		{
			isPaused = !isPaused;

			_uiController.gameMenu.SetActive(!isPaused);
			_uiController.pauseMenu.SetActive(isPaused);
		}
	}

	public void ReturnToMainMenu()
	{
		EndLevel();

		_uiController.Fade(onScreenFullyCovered: () =>
		{
			CleanUpLevel(false);

			_uiController.endMenu.SetActive(false);
			_uiController.loseMenu.SetActive(false);
			_uiController.pauseMenu.SetActive(false);
			_uiController.gameMenu.SetActive(false);
			_uiController.mainMenu.SetActive(true);

			_soundController.SwitchBGM(_menuBGM, true);
		});
	}

	public void ExitGame()
	{
#if UNITY_EDITOR
		EditorApplication.ExitPlaymode();
#endif
		if (!Application.isEditor)
		{
			Application.Quit();
		}
	}

	#endregion

	#region Game/Level Starts

	public void StartGame()
	{
		_level = 0;

		adam = Spawn(_adamPrefab, Vector3.zero, false);
		adam.Initialize(this, null);

		StartLevel();
	}

	public void RestartLevel()
	{
		_uiController.Fade(0.3f, 0.3f, 0.1f, OnFadeFullyCovered);

		void OnFadeFullyCovered()
		{
			_uiController.loseMenu.SetActive(false);
			_uiController.gameMenu.SetActive(true);

			StartGameLoop(true);
		}
	}

	private void StartLevel()
	{
		_uiController.Fade(onScreenFullyCovered: OnFadeFullyCovered);

		void OnFadeFullyCovered()
		{
			CleanUpLevel(true);

			_uiController.mainMenu.SetActive(false);
			_uiController.gameMenu.SetActive(true);
			_uiController.levelClearedScreen.SetActive(false);

			StartGameLoop(false);
		}
	}

	public void FinishInitiatingLevel()
	{
		hasFinishedInitiatingLevel = true;
	}

	private void StartGameLoop(bool isRestart)
	{
		StartCoroutine(StartGameLoop());

		IEnumerator StartGameLoop()
		{
			yield return InitiateLevel();
			StartCoroutine(GameLoop());
		}

		IEnumerator InitiateLevel()
		{
			if (isRestart || _level == 0)
			{
				_soundController.SwitchBGM(_levelBGM, true);
			}

			var levelLogicManager = _levelLogicManagers[_level];

			_uiController.playerMoveButton.SetActive(levelLogicManager.allowedPlayerActionsAtStart.Contains(PlayerAction.Move));
			_uiController.playerPunchButton.SetActive(levelLogicManager.allowedPlayerActionsAtStart.Contains(PlayerAction.Punch));
			_uiController.playerShootButton.SetActive(levelLogicManager.allowedPlayerActionsAtStart.Contains(PlayerAction.Shoot));

			hasFinishedInitiatingLevel = false;
			hasDroppedAdam = false;

			GenerateTiles();
			ResetCameraToFaceMapCenter();

			adam.transform.position = centerTile.basePosition + Vector3.up * _fallHeight;

			levelLogicManager.InitiateLevel(_level, this, isRestart);

			yield return new WaitWhile(() => _uiController.isFading);
			if (!isRestart) { yield return _uiController.ShowLevelTitle(_level); }
			yield return SpawnAndDropAdam();
			yield return new WaitUntil(() => hasFinishedInitiatingLevel);

			if (levelLogicManager.adamExtraMoves > 0)
			{
				_uiController.ShowMessage("Your movement reach is increased on this level. Check it out!");
			}
			else if (levelLogicManager.adamExtraMoves < 0)
			{
				_uiController.ShowMessage("Sorry, your movement reach is decreased on this level. Cheers!");
			}
		}

		IEnumerator SpawnAndDropAdam()
		{
			adam.SetTile(centerTile);

			yield return Drop(adam);

			hasDroppedAdam = true;
		}
	}

	#endregion

	#region Game/Level Ends	

	public void LoseGame()
	{
		EndLevel();

		_uiController.gameMenu.SetActive(false);
		_uiController.ShowLoseOverlay();

		_soundController.SwitchBGM(_loseBGM, false);
	}

	private void EndLevel()
	{
		StopAllCoroutines();
		isRunningGameLoop = false;
	}

	public void FinishLevel()
	{
		EndLevel();

		StartCoroutine(WaitThenShowLevelCleared());

		IEnumerator WaitThenShowLevelCleared()
		{
			yield return new WaitForSeconds(1f);
			_uiController.ShowLevelCleared(() =>
			{
				AdvanceLevel();
			});
		}
	}

	private void AdvanceLevel()
	{
		_level += 1;
		if (_level >= 2) { EndGame(); }
		else { StartLevel(); }
	}

	private void EndGame()
	{
		_uiController.Fade(onScreenFullyCovered: OnFadeFullyCovered);

		void OnFadeFullyCovered()
		{
			_uiController.gameMenu.SetActive(false);
			_uiController.levelClearedScreen.SetActive(false);
			_uiController.endMenu.SetActive(true);
			_uiController.ShowThankYou();

			_soundController.SwitchBGM(_endBGM, false);
		}
	}

	#endregion

	#region Game Loop

	public void SetStartingCharacter(GameCharacter character)
	{
		if (isRunningGameLoop == false)
		{
			_activeCharacter = character;
		}
	}

	private IEnumerator GameLoop()
	{
		isPaused = false;
		isRunningGameLoop = true;

		while (Application.isPlaying)
		{
			_activeCharacter.StartTurn();

			yield return new WaitUntil(() => _activeCharacter == null);
			yield return new WaitForSeconds(0.5f);

			if (isPaused)
			{
				yield return new WaitWhile(() => isPaused);
			}

			_activeCharacter = _gameCharacters[(_gameCharacters.IndexOf(_previousCharacter) + 1) % _gameCharacters.Count];
		}

		isRunningGameLoop = false;
	}

	public void PauseLoop()
	{
		isPaused = true;
	}

	public void ResumeLoop()
	{
		isPaused = false;
	}

	public void FinishTurn(GameCharacter character)
	{
		if (character == _activeCharacter)
		{
			_previousCharacter = character;
			_activeCharacter = null;
		}
	}

	#endregion

	#region Player Control

	public event Action<GameEntity> onSelectEntity;

	public void Select(GameEntity gameCharacter)
	{
		onSelectEntity?.Invoke(gameCharacter);
	}

	public void ClearHighlights()
	{
		foreach (var tile in _tiles)
		{
			tile.SetHighlight(false, Color.black);
		}
	}

	private List<Tile> _adamMoveTargets = new List<Tile>();

	public void RecalculateAdamMoveTargets()
	{
		_adamMoveTargets = GridUtilities.GetTilesInRange(adam.currentTile, adam.moveReach + _levelLogicManagers[_level].adamExtraMoves, false);
	}

	public List<Tile> CalculateAdamPath()
	{
		var pointedPath = GetPointedTile();
		if (_adamMoveTargets.Contains(pointedPath))
		{
			return GridUtilities.CalculatePath(_tiles, adam.currentTile, GetPointedTile());
		}
		else
		{
			return GridUtilities.EmptyTileList;
		}
	}

	public void HighlightAdamMoveTargetsAndPath(List<Tile> path)
	{
		foreach (var tile in _tiles)
		{
			tile.SetHighlight(_adamMoveTargets.Contains(tile), path.Contains(tile) ? _adamPathHighlightColor : _adamMoveTargetsHighlightColor);
		}
	}

	public void HighlightAdamActionTargets(List<Tile> targets, Tile pointedTarget)
	{
		foreach (var tile in _tiles)
		{
			tile.SetHighlight(targets.Contains(tile), tile == pointedTarget ? _adamActionTargetsHighlightColor : _adamMoveTargetsHighlightColor);
		}
	}

	public void HighlightAdamActionTargets(List<Tile> targets, List<Tile> aimedTargets)
	{
		foreach (var tile in _tiles)
		{
			tile.SetHighlight(targets.Contains(tile), aimedTargets.Contains(tile) ? _adamActionTargetsHighlightColor : _adamMoveTargetsHighlightColor);
		}
	}

	public void ActivateEntityColliders(List<Tile> tilesWithActiveEntityColliders)
	{
		foreach (var tile in _tiles)
		{
			if (tile.isOccupied)
			{
				var colliders = tile.occupant.GetComponentsInChildren<Collider>();
				if (colliders != null)
				{
					foreach (var collider in colliders)
					{
						collider.enabled = tilesWithActiveEntityColliders.Contains(tile);
					}
				}
			}
		}
	}

	#endregion

	#region Misc

	public IEnumerator Drop(GameEntity entity)
	{
		var fallSpeed = _initialFallSpeed;

		entity.transform.position = entity.currentTile.basePosition + Vector3.up * _fallHeight;

		while (Application.isPlaying)
		{
			var delta = _initialFallSpeed * Time.deltaTime;
			if (entity.transform.position.y - delta < entity.currentTile.basePosition.y) { break; }
			else
			{
				entity.transform.position += Vector3.down * delta;
			}

			fallSpeed += _gravity * Time.deltaTime;
			yield return null;
		}

		entity.transform.position = entity.currentTile.basePosition;
		_soundController.PlaySoundEffect(_dropSFX);
	}

	public List<Tile> GetEmptyTiles()
	{
		return _tiles.FindAll(x => !x.isOccupied);
	}

	#endregion

	#region Tiles Management & Utilities

	private void GenerateTiles()
	{
		CleanUpLevel(true);

		_mapLength = _mapWidth = _levelLogicManagers[_level].gridSize;

		for (int i = 0; i < _mapLength * _mapWidth; i++)
		{
			_tiles.Add(Spawn(_tilePrefab, Vector3.zero, _mapTilesContainer));
		}

		for (int i = 0; i < _tiles.Count; i++)
		{
			_tiles[i].Initialize(i, i % _mapLength, i / _mapLength, this);
		}

		centerTile = _tiles[_tiles.Count / 2];
	}

	public Tile GetTile(int x, int y)
	{
		if (x < 0 || y < 0 || x >= _mapLength || y >= _mapWidth)
		{
			return null;
		}
		else
		{
			return _tiles[x + y * _mapLength];
		}
	}

	public Tile GetPointedTile()
	{
		var pointerGroundPos = GroundPointUtilities.GetPointerGroundPoint(_gameCamera);
		var offsetGroundPos = pointerGroundPos + GridOffset;

		if (offsetGroundPos.x < 0 || offsetGroundPos.z < 0 || offsetGroundPos.x >= _mapLength || offsetGroundPos.z >= _mapWidth)
		{
			return null;
		}
		else
		{
			var pointedIndex = (int)offsetGroundPos.z * _mapWidth + (int)offsetGroundPos.x;
			return _tiles[pointedIndex];
		}
	}

	#endregion

	#region Instance Management

	public T Spawn<T>(T prefab, Vector3 position, bool isTile) where T : MonoBehaviour
	{
		var instance = Instantiate(prefab, position, Quaternion.identity, isTile ? _mapTilesContainer : _gameEntitiesContainer);
		_instantiatedObjects.Add(instance.gameObject);

		if (prefab is GameCharacter)
		{
			_gameCharacters.Add(instance as GameCharacter);
		}

		if (prefab is GameEntity)
		{
			_gameEntities.Add(instance as GameEntity);
		}

		return instance;
	}

	public void Despawn(GameObject gameObject)
	{
		_gameEntities.RemoveAll(x => x.gameObject == gameObject);
		_gameCharacters.RemoveAll(x => x.gameObject == gameObject);
		_instantiatedObjects.Remove(gameObject);

		Destroy(gameObject);
	}

	private void CleanUpLevel(bool saveAdam)
	{
		for (int i = 0; i < _instantiatedObjects.Count; i++)
		{
			if (saveAdam && _instantiatedObjects[i] == adam.gameObject)
			{
				continue;
			}

			Destroy(_instantiatedObjects[i]);
		}

		_tiles.Clear();
		_gameEntities.Clear();
		_gameCharacters.Clear();
		_instantiatedObjects.Clear();

		if (saveAdam)
		{
			_gameEntities.Add(adam);
			_gameCharacters.Add(adam);
			_instantiatedObjects.Add(adam.gameObject);
		}
	}

	#endregion

	#region Camera & Screen Effects

	private void ResetCameraToFaceMapCenter()
	{
		var mapCenterPoint = new Vector3(_mapWidth, 0f, _mapLength) * GridOffsetValue - GridOffset;
		var cameraTransform = _gameCamera.transform;

		cameraTransform.position += Vector3.up * (_cameraInitialHeight - cameraTransform.position.y);
		cameraTransform.position += mapCenterPoint - GroundPointUtilities.GetCameraForwardGroundPoint(_gameCamera);

		_baseCameraPosition = cameraTransform.position;
	}

	public void ShakeCamera(float strength = 1f, float frequencyMultiplier = 1f, float duration = 0.2f)
	{
		StartCoroutine(Shake());

		IEnumerator Shake()
		{
			var cameraTransform = _gameCamera.transform;

			var x = Random.Range(-1000f, 1000f);
			var y = Random.Range(-1000f, 1000f);
			var o = 0f;

			for (float t = duration; t >= 0; t -= Time.deltaTime)
			{
				var influence = t / duration;
				cameraTransform.position = _baseCameraPosition + (cameraTransform.up * Mathf.Lerp(-1f, 1f, Mathf.PerlinNoise(x, o)) + cameraTransform.right * Mathf.Lerp(-1f, 1f, Mathf.PerlinNoise(o, y))) * strength * influence;

				yield return null;

				o += _cameraShakePerlinScale * frequencyMultiplier * Time.deltaTime;
			}

			cameraTransform.position = _baseCameraPosition;

			yield break;
		}
	}

	public void SkipFrame(int frames = 1)
	{
		StartCoroutine(Skip());

		IEnumerator Skip()
		{
			Time.timeScale = 0;

			for (int i = 0; i < frames; i++)
			{
				yield return null;
			}

			Time.timeScale = 1;
		}
	}

	#endregion

	#region DEBUG

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			TryTogglePause();
		}
#if UNITY_EDITOR
		else if (Input.GetKeyDown(KeyCode.L))
		{
			LoseGame();
		}
		else if (Input.GetKeyDown(KeyCode.F))
		{
			FinishLevel();
		}
		else if (Input.GetKeyDown(KeyCode.Space))
		{
			ShakeCamera();
		}

		Debug.DrawRay(_gameCamera.transform.position, _gameCamera.transform.forward * 100);
		Debug.DrawLine(_gameCamera.transform.position, new Vector3(_mapLength, 0f, _mapWidth) * GridOffsetValue - GridOffset, Color.red);
#endif
	}

	private void OnDrawGizmos()
	{
		for (int x = 0; x <= _mapLength; x++)
		{
			Gizmos.DrawRay(Vector3.right * (x - GridOffsetValue) + Vector3.back * GridOffsetValue, Vector3.forward * _mapWidth);
		}

		for (int y = 0; y <= _mapWidth; y++)
		{
			Gizmos.DrawRay(Vector3.forward * (y - GridOffsetValue) + Vector3.left * GridOffsetValue, Vector3.right * _mapLength);
		}
	}

	#endregion
}
