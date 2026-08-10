using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    public List<Sprite> Sprites = new List<Sprite>();
    public GameObject TilePrefab;
    public int GridDimension = 8;
    public float Distance = 1.0f;
    private GameObject[,] Grid;

    public GameObject GameOverMenu;
    public TextMeshProUGUI MovesText;
    public TextMeshProUGUI ScoreText;

    public int StartingMoves = 50;
    public bool IsGameOver = false;

    private int _numMoves;
    public int NumMoves
    {
        get { return _numMoves; }
        set
        {
            _numMoves = value;
            if (MovesText != null)
                MovesText.text = _numMoves.ToString();
        }
    }

    private int _score;
    public int Score
    {
        get { return _score; }
        set
        {
            _score = value;
            if (ScoreText != null)
                ScoreText.text = _score.ToString();
        }
    }

    void Awake()
    {
        Instance = this;

        if (MovesText == null)
            Debug.LogWarning("GridManager: MovesText не назначен в инспекторе!");
        if (ScoreText == null)
            Debug.LogWarning("GridManager: ScoreText не назначен в инспекторе!");
        if (GameOverMenu == null)
            Debug.LogWarning("GridManager: GameOverMenu не назначен в инспекторе!");
        if (Sprites == null || Sprites.Count == 0)
            Debug.LogError("GridManager: список Sprites пуст! Заполните его в инспекторе.");

        IsGameOver = false;
        Score = 0;
        NumMoves = StartingMoves;

        if (GameOverMenu != null)
            GameOverMenu.SetActive(false);
    }

    void Start()
    {
        Grid = new GameObject[GridDimension, GridDimension];
        InitGrid();
    }

    void InitGrid()
    {
        Vector3 positionOffset = transform.position - new Vector3(GridDimension * Distance / 2.0f, GridDimension * Distance / 2.0f, 0);

        for (int row = 0; row < GridDimension; row++)
        {
            for (int column = 0; column < GridDimension; column++)
            {
                GameObject newTile = Instantiate(TilePrefab);
                SpriteRenderer renderer = newTile.GetComponent<SpriteRenderer>();
                renderer.sprite = Sprites[Random.Range(0, Sprites.Count)];

                Tile tile = newTile.AddComponent<Tile>();
                tile.Position = new Vector2Int(column, row);

                newTile.transform.parent = transform;
                newTile.transform.position = new Vector3(column * Distance, row * Distance, 0) + positionOffset;

                Grid[column, row] = newTile;
            }
        }
    }

    SpriteRenderer GetSpriteRendererAt(int column, int row)
    {
        if (column < 0 || column >= GridDimension || row < 0 || row >= GridDimension)
            return null;

        GameObject tile = Grid[column, row];
        return tile.GetComponent<SpriteRenderer>();
    }

    List<SpriteRenderer> FindColumnMatchForTile(int col, int row, Sprite sprite)
    {
        List<SpriteRenderer> result = new List<SpriteRenderer>();
        for (int i = col + 1; i < GridDimension; i++)
        {
            SpriteRenderer next = GetSpriteRendererAt(i, row);
            if (next == null || next.sprite != sprite)
                break;
            result.Add(next);
        }
        return result;
    }

    List<SpriteRenderer> FindRowMatchForTile(int col, int row, Sprite sprite)
    {
        List<SpriteRenderer> result = new List<SpriteRenderer>();
        for (int i = row + 1; i < GridDimension; i++)
        {
            SpriteRenderer next = GetSpriteRendererAt(col, i);
            if (next == null || next.sprite != sprite)
                break;
            result.Add(next);
        }
        return result;
    }

    bool CheckMatches()
    {
        HashSet<SpriteRenderer> matchedTiles = new HashSet<SpriteRenderer>();

        for (int row = 0; row < GridDimension; row++)
        {
            for (int column = 0; column < GridDimension; column++)
            {
                SpriteRenderer current = GetSpriteRendererAt(column, row);
                if (current.sprite == null)
                    continue;

                List<SpriteRenderer> horizontalMatches = FindColumnMatchForTile(column, row, current.sprite);
                if (horizontalMatches.Count >= 2)
                {
                    matchedTiles.UnionWith(horizontalMatches);
                    matchedTiles.Add(current);
                }

                List<SpriteRenderer> verticalMatches = FindRowMatchForTile(column, row, current.sprite);
                if (verticalMatches.Count >= 2)
                {
                    matchedTiles.UnionWith(verticalMatches);
                    matchedTiles.Add(current);
                }
            }
        }

        foreach (SpriteRenderer renderer in matchedTiles)
        {
            renderer.sprite = null;
        }

        if (matchedTiles.Count > 0)
            Score += matchedTiles.Count;

        return matchedTiles.Count > 0;
    }

    void FillHoles()
    {
        if (Sprites == null || Sprites.Count == 0)
        {
            Debug.LogError("FillHoles: список Sprites пуст, заполнение отменено.");
            return;
        }

        for (int column = 0; column < GridDimension; column++)
        {
            for (int row = 0; row < GridDimension; row++)
            {
                int safety = 0;
                while (GetSpriteRendererAt(column, row).sprite == null)
                {
                    for (int filler = row; filler < GridDimension - 1; filler++)
                    {
                        SpriteRenderer current = GetSpriteRendererAt(column, filler);
                        SpriteRenderer next = GetSpriteRendererAt(column, filler + 1);
                        current.sprite = next.sprite;
                    }

                    SpriteRenderer last = GetSpriteRendererAt(column, GridDimension - 1);
                    last.sprite = Sprites[Random.Range(0, Sprites.Count)];

                    safety++;
                    if (safety > GridDimension + 5)
                    {
                        Debug.LogError("FillHoles: слишком много итераций, прерываю (column=" + column + ", row=" + row + ")");
                        break;
                    }
                }
            }
        }
    }

    void GameOver()
    {
        IsGameOver = true;
        Debug.Log("GAME OVER");
        PlayerPrefs.SetInt("score", Score);

        if (GameOverMenu != null)
            GameOverMenu.SetActive(true);

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound(SoundType.TypeGameOver);
    }

    public void SwapTiles(Vector2Int tile1Position, Vector2Int tile2Position)
    {
        if (IsGameOver)
            return;

        GameObject tile1 = Grid[tile1Position.x, tile1Position.y];
        SpriteRenderer renderer1 = tile1.GetComponent<SpriteRenderer>();

        GameObject tile2 = Grid[tile2Position.x, tile2Position.y];
        SpriteRenderer renderer2 = tile2.GetComponent<SpriteRenderer>();

        Sprite temp = renderer1.sprite;
        renderer1.sprite = renderer2.sprite;
        renderer2.sprite = temp;

        bool changesOccur = CheckMatches();

        if (!changesOccur)
        {
            temp = renderer1.sprite;
            renderer1.sprite = renderer2.sprite;
            renderer2.sprite = temp;

            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySound(SoundType.TypePop);
        }
        else
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySound(SoundType.TypeMove);

            NumMoves--;

            int loopSafety = 0;
            do
            {
                FillHoles();
                loopSafety++;
                if (loopSafety > 50)
                {
                    Debug.LogError("SwapTiles: слишком много итераций FillHoles/CheckMatches, прерываю цикл.");
                    break;
                }
            } while (CheckMatches());

            if (NumMoves <= 0)
            {
                NumMoves = 0;
                GameOver();
            }
        }
    }
}