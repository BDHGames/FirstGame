using odin.serialize.OdinSerializer;
using UnityEngine;

[CreateAssetMenu(fileName = "BoardLayout", menuName = "Scriptable Objects/Board Layout")]
public class BoardLayout : SerializedScriptableObject
{
	[Min(1)]
	public int _length = 1;

	[Min(1)]
	public int _height = 1;

	[OdinSerialize]
	private CELLK[,] _grid;

	public CELLK[,] Grid => _grid;

	public CELLK this[int col, int row]
	{
		get => _grid[col, row];
		set => _grid[col, row] = value; // should only be used if a boss has a board disruption
	}

	public CELLK this[Vector2Int coord]
	{
		get => _grid[coord.x, coord.y];
		set => _grid[coord.x, coord.y] = value; // should only be used if a boss has a board disruption
	}

#if UNITY_EDITOR
	// Does not compile in release builds

	public void SetGrid(CELLK[,] gridNew)
	{
		_grid = gridNew;
		_length = _grid.GetLength(0);
		_height = _grid.GetLength(1);
	}
#endif

	public Vector2Int Dims() => new Vector2Int(_length, _height);
	public Vector2Int BottomRight() => Dims() - Vector2Int.one;

	// should Layout have TopRow, BottomRow, LeftCol, and RightCol properties?
}
