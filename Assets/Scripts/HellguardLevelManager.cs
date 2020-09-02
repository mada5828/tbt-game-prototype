using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "NewHellguardLevel", menuName = "TBTP/Hellguard Level")]
public class HellguardLevelManager : LevelLogicManager
{
	public Boulder boulderPrefab;
	public Hellguard hellguardPrefab;
	public int hellguardPatrolRadius;
	public int[] boulderRadiuses;
	public int minimumBoulders;
	public bool dropHintBoulder;

	public override void InitiateLevel(int level, GameManager gameManager, bool isRestart)
	{
		int boulderCount = 0;

		gameManager.StartCoroutine(InitiateLevel());

		IEnumerator InitiateLevel()
		{
			foreach (var boulderRadius in boulderRadiuses)
			{
				for (int x = -boulderRadius; x <= boulderRadius; x++)
				{
					for (int y = -boulderRadius; y <= boulderRadius; y++)
					{
						if (Mathf.Abs(x) + Mathf.Abs(y) == boulderRadius)
						{
							var tile = gameManager.GetTile(gameManager.centerTile.gridX + x, gameManager.centerTile.gridY + y);

							if (tile != null)
							{
								SpawnBoulder(tile);
							}
						}
					}
				}
			}

			var hellguard = gameManager.Spawn(hellguardPrefab, Vector3.zero, false);
			hellguard.Initialize(gameManager, null);
			hellguard.InitializePatrol(hellguardPatrolRadius);

			var isMovingHellguard = true;
			gameManager.StartCoroutine(MoveHellguard());

			yield return new WaitUntil(() => gameManager.hasDroppedAdam);

			gameManager.ShakeCamera(1f, 1.5f, 0.32f);
			//gameManager.SkipFrame(3);

			Boulder boulder = default;
			if (dropHintBoulder)
			{
				boulder = SpawnBoulder(hellguard.GetNextPatrolTile(hellguard.movement));
				yield return gameManager.Drop(boulder);
			}

			yield return new WaitWhile(() => isMovingHellguard);
			while (dropHintBoulder && boulder.currentHealth > 0)
			{
				yield return hellguard.Move();
			}

			gameManager.SetStartingCharacter(dropHintBoulder ? gameManager.adam : hellguard as GameCharacter);
			gameManager.FinishInitiatingLevel();


			IEnumerator MoveHellguard()
			{
				while (!gameManager.hasDroppedAdam)
				{
					yield return hellguard.Move();
					yield return new WaitForSeconds(1f);
				}

				isMovingHellguard = false;
			}
		}


		Boulder SpawnBoulder(Tile tile)
		{
			var boulder = gameManager.Spawn(boulderPrefab, Vector3.zero, false);
			boulder.Initialize(gameManager, tile);
			boulder.onWillDie += OnBoulderWillDie;
			boulderCount += 1;
			return boulder;
		}

		void OnBoulderWillDie(GameEntity entity)
		{
			boulderCount -= 1;

			if (boulderCount < minimumBoulders)
			{
				var emptyTiles = gameManager.GetEmptyTiles();
				emptyTiles.RemoveAll(t => t.gridIndex == entity.currentTile.gridIndex || t.gridX == 0 || t.gridY == 0 || t.gridX == gridSize - 1 || t.gridY == gridSize - 1);

				gameManager.StartCoroutine(DropNewBoulder(SpawnBoulder(emptyTiles[Random.Range(0, emptyTiles.Count)])));
			}
		}

		IEnumerator DropNewBoulder(Boulder boulder)
		{
			gameManager.PauseLoop();
			yield return gameManager.Drop(boulder);
			gameManager.ResumeLoop();
		}
	}
}