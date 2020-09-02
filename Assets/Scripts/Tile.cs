using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
	private MaterialPropertyBlock _materialPropertyBlock;

	[SerializeField] private MeshRenderer _meshRenderer = default;
	[SerializeField] private SpriteRenderer _highlight = default;
	[Space]
	[SerializeField] private float _baseHeight = 0f;
	[SerializeField] private float _heightVariance = 0.02f;
	[Space]
	[SerializeField] private Color _baseColor = Color.grey;
	[SerializeField] private float _colorVariance = 0.1f;

	public List<Tile> neighbors { get; private set; }
	new public Transform transform { get; private set; }
	public GameManager gameManager { get; private set; }
	public Vector3 basePosition { get; private set; }
	public int gridIndex { get; private set; }
	public int gridX { get; private set; }
	public int gridY { get; private set; }
	public GameEntity occupant;// { get; private set; }
	public bool isOccupied { get; private set; }

	public void SetHighlight(bool isHighlighted, Color color)
	{
		_highlight.gameObject.SetActive(isHighlighted);
		_highlight.color = color;
	}

	public void Occupy(GameEntity occupant)
	{
		if (this.occupant == null && occupant != null)
		{
			this.occupant = occupant;
			isOccupied = true;
		}
	}

	public void Vacate(GameEntity exOccupant)
	{
		if (occupant == exOccupant)
		{
			occupant = null;
			isOccupied = false;
		}
		else
		{
			Debug.LogError(occupant);
		}
	}

	public void Initialize(int gridIndex, int gridX, int gridY, GameManager gameManager)
	{
		this.gameManager = gameManager;
		this.gridIndex = gridIndex;
		this.gridX = gridX;
		this.gridY = gridY;

		transform = base.transform;

		basePosition = new Vector3(gridX, _baseHeight + _heightVariance * Random.Range(-1f, 1f), gridY);
		transform.localPosition = basePosition;

		float h, s, v;
		Color.RGBToHSV(_baseColor, out h, out s, out v);

		_materialPropertyBlock = new MaterialPropertyBlock();
		_meshRenderer.GetPropertyBlock(_materialPropertyBlock);
		_materialPropertyBlock.SetColor("_Color", Color.HSVToRGB(h, s, v + _colorVariance * Random.Range(-1f, 1f)));
		_meshRenderer.SetPropertyBlock(_materialPropertyBlock);

		name = gridIndex.ToString();

		neighbors = new List<Tile>()
		{
			gameManager.GetTile(gridX - 1, gridY),
			gameManager.GetTile(gridX, gridY + 1),
			gameManager.GetTile(gridX + 1, gridY),
			gameManager.GetTile(gridX, gridY - 1)
		};

		neighbors.RemoveAll(x => x == null);
	}
}
