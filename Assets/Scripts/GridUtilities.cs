using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class GridUtilities
{
	public static int CalculateManhattanDistance(Tile a, Tile b)
	{
		return Mathf.Abs(a.gridX - b.gridX) + Mathf.Abs(a.gridY - b.gridY);
	}

	public static List<Tile> GetTilesInRange(Tile origin, int range, bool includeOccupiedTiles)
	{
		var availableTiles = new List<Tile>();

		var steps = new Dictionary<int, int>();
		var frontier = new List<Tile>();

		steps.Add(origin.gridIndex, 0);
		frontier.Add(origin);

		while (frontier.Count != 0)
		{
			var candidate = frontier[0];
			if (steps[candidate.gridIndex] <= range)
			{
				foreach (var neighbor in candidate.neighbors)
				{
					if ((includeOccupiedTiles || !neighbor.isOccupied) && !frontier.Contains(neighbor))
					{
						var newNeighborStep = steps[candidate.gridIndex] + 1;
						if (steps.ContainsKey(neighbor.gridIndex))
						{
							if (newNeighborStep < steps[neighbor.gridIndex])
							{
								steps[neighbor.gridIndex] = newNeighborStep;
								frontier.Add(neighbor);
							}
						}
						else
						{
							steps.Add(neighbor.gridIndex, newNeighborStep);
							frontier.Add(neighbor);
						}
					}
				}

				if (candidate != origin)
				{
					availableTiles.Add(candidate);
				}
			}
			
			frontier.RemoveAt(0);
		}

		return availableTiles;
	}


	public static readonly List<Tile> EmptyTileList = new List<Tile>();
	public static List<Tile> CalculatePath(List<Tile> grid, Tile origin, Tile target, bool includeOccupiedTiles = false)
	{
		if (origin == null || target == null)
		{
			return EmptyTileList;
		}

		var frontier = new List<Tile>();
		var ancestors = new Dictionary<int, int>();
		var scores = new Dictionary<int, float>();

		ancestors.Add(origin.gridIndex, -1);
		scores.Add(origin.gridIndex, 0);

		frontier.Add(origin);

		while (frontier.Count != 0)
		{
			frontier.Sort((x, y) => CalculateTotalScore(x).CompareTo(CalculateTotalScore(y)));

			var candidate = frontier[0];

			if (candidate == target)
			{
				var head = target;
				var path = new List<Tile>() { head };

				while (head != origin)
				{
					head = grid[ancestors[head.gridIndex]];
					path.Add(head);
				}

				path.Reverse();

				return path;
			}

			foreach (var neighbor in candidate.neighbors)
			{
				var score = scores[candidate.gridIndex] + (!includeOccupiedTiles && neighbor.isOccupied ? 10000 : 1);

				if (scores.ContainsKey(neighbor.gridIndex))
				{
					if (score < scores[neighbor.gridIndex])
					{
						scores[neighbor.gridIndex] = score;
						ancestors[neighbor.gridIndex] = candidate.gridIndex;
						frontier.Add(neighbor);
					}
				}
				else
				{
					scores.Add(neighbor.gridIndex, score);
					ancestors[neighbor.gridIndex] = candidate.gridIndex;
					frontier.Add(neighbor);
				}
			}

			frontier.RemoveAt(0);
		}

		return EmptyTileList;


		float CalculateTotalScore(Tile tile)
		{
			return scores[tile.gridIndex] + (CalculateManhattanDistance(tile, origin) * 1.100f + CalculateManhattanDistance(tile, target));
		}
	}
}