using System.Collections.Generic;
using UnityEngine;

public abstract class LevelLogicManager : ScriptableObject
{
	public int gridSize = 7;
	public int adamExtraMoves;
	public List<PlayerAction> allowedPlayerActionsAtStart;

	public abstract void InitiateLevel(int level, GameManager gameManager, bool isRestart);

	private void OnValidate()
	{
		gridSize = Mathf.Max(1, gridSize - gridSize % 2 + 1);
	}
}