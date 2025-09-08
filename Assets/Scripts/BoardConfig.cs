using UnityEngine;

/// <summary>
/// General config class for GameBoard
/// </summary>
public class BoardConfig : MonoBehaviour
{
	public static BoardConfig INSTANCE;

	[SerializeField]
	private SETTLEK _defaultSettleKind = SETTLEK.FALL;
	private SETTLEK _overrideSettleKind = SETTLEK.NIL;
	public SETTLEK SettleKind => _overrideSettleKind != SETTLEK.NIL ? _overrideSettleKind : _defaultSettleKind;

	[SerializeField]
	private CharacterWeights _characterWeights;
	public CharacterWeights Weights => _characterWeights;

	[SerializeField]
	private BoardLayout _layout;
	public BoardLayout Layout { get => _layout; set => _layout = value; }

	[SerializeField]
	private Tile _defaultTilePrefab; // this will later be joined by any other tile types we choose to add
	public Tile DefaultTilePrefab => _defaultTilePrefab;

	[Header("Y must be negative")]
	[SerializeField]
	private Vector2 _tileSpacing;
	public Vector2 TileSpacing => _tileSpacing;
	[SerializeField]
	private Vector2 _spawnOffset;
	public Vector2 SpawnOffset => _spawnOffset;

	[SerializeField]
	[Min(0.1f)]
	private float _fallSpeed;
	public float FallSpeed => _fallSpeed;

	private void Awake()
	{
		// set up singleton

		if (INSTANCE != null && INSTANCE != this)
		{
			Destroy(gameObject);
			return;
		}

		INSTANCE = this;

#if DEBUG
		// this should not show up in the release build ever

		if (_layout == null)
		{
			ConstructTestBoard();
		}
#endif
	}
	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{
	}

	// Update is called once per frame
	void Update()
	{
		
	}

#if DEBUG
	private void ConstructTestBoard()
	{
		_layout = new BoardLayout(7, 7);

		_layout[0, 0] = CELLK.STANDARD; _layout[1, 0] = CELLK.STANDARD; _layout[2, 0] = CELLK.VOID;		_layout[3, 0] = CELLK.VOID;		_layout[4, 0] = CELLK.VOID;		_layout[5, 0] = CELLK.STANDARD; _layout[6, 0] = CELLK.STANDARD;
		_layout[0, 1] = CELLK.STANDARD; _layout[1, 1] = CELLK.STANDARD; _layout[2, 1] = CELLK.STANDARD; _layout[3, 1] = CELLK.VOID;		_layout[4, 1] = CELLK.STANDARD; _layout[5, 1] = CELLK.STANDARD; _layout[6, 1] = CELLK.STANDARD;
		_layout[0, 2] = CELLK.VOID;		_layout[1, 2] = CELLK.STANDARD; _layout[2, 2] = CELLK.STANDARD; _layout[3, 2] = CELLK.STANDARD; _layout[4, 2] = CELLK.STANDARD; _layout[5, 2] = CELLK.STANDARD; _layout[6, 2] = CELLK.VOID;
		_layout[0, 3] = CELLK.VOID;		_layout[1, 3] = CELLK.VOID;		_layout[2, 3] = CELLK.STANDARD; _layout[3, 3] = CELLK.STANDARD; _layout[4, 3] = CELLK.STANDARD; _layout[5, 3] = CELLK.VOID;		_layout[6, 3] = CELLK.VOID;
		_layout[0, 4] = CELLK.VOID;		_layout[1, 4] = CELLK.STANDARD; _layout[2, 4] = CELLK.STANDARD; _layout[3, 4] = CELLK.STANDARD; _layout[4, 4] = CELLK.STANDARD; _layout[5, 4] = CELLK.STANDARD; _layout[6, 4] = CELLK.VOID;
		_layout[0, 5] = CELLK.STANDARD; _layout[1, 5] = CELLK.STANDARD; _layout[2, 5] = CELLK.STANDARD; _layout[3, 5] = CELLK.VOID;		_layout[4, 5] = CELLK.STANDARD; _layout[5, 5] = CELLK.STANDARD; _layout[6, 5] = CELLK.STANDARD;
		_layout[0, 6] = CELLK.STANDARD; _layout[1, 6] = CELLK.STANDARD; _layout[2, 6] = CELLK.VOID;		_layout[3, 6] = CELLK.VOID;		_layout[4, 6] = CELLK.VOID;		_layout[5, 6] = CELLK.STANDARD; _layout[6, 6] = CELLK.STANDARD;
	}
#endif

	public void SetOverrideSettlek(SETTLEK overrideSettleKind)
	{
		_overrideSettleKind = overrideSettleKind;
	}
}
