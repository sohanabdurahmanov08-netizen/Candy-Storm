using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    public List<Sprite> Sprites = new List<Sprite>();
    public GameObject TilePrefab;
    public int GridDimension = 8;
    public float Distance = 1.0f;
    public float AnimationSpeed = 8f;

    private GameObject[,] Grid;
    private Vector3 positionOffset;

    public GameObject GameOverMenu;
    public TextMeshProUGUI MovesText;
    public TextMeshProUGUI ScoreText;

    public int StartingMoves = 50;
    public bool IsGameOver = false;
    public bool IsBusy = false;

    private int _numMoves;
    public int NumMoves
    {
        get { return _numMoves; }
        set { _numMoves = value; if (MovesText != null) MovesText.text = _numMoves.ToString(); }
    }

    private int _score;
    public int Score
    {
        get { return _score; }
        set { _score = value; if (ScoreText != null) ScoreText.text = _score.ToString(); }
    }

    void Awake()
    {
        Instance = this;
        IsGameOver = false;
        Score = 0;
        NumMoves = StartingMoves;
        if (GameOverMenu != null) GameOverMenu.SetActive(false);
    }

    void Start()
    {
        Grid = new GameObject[GridDimension, GridDimension];
        positionOffset = transform.position - new Vector3(GridDimension * Distance / 2.0f, GridDimension * Distance / 2.0f, 0);
        InitGrid();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            ShuffleBoard();
        }
    }

    Vector3 GetWorldPosition(int column, int row)
    {
        return new Vector3(column * Distance, row * Distance, 0) + positionOffset;
    }

    void InitGrid()
    {
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
                newTile.transform.position = GetWorldPosition(column, row);

                Grid[column, row] = newTile;
            }
        }
    }

    SpriteRenderer GetSpriteRendererAt(int column, int row)
    {
        if (column < 0 || column >= GridDimension || row < 0 || row >= GridDimension)
            return null;
        return Grid[column, row].GetComponent<SpriteRenderer>();
    }

    List<SpriteRenderer> FindColumnMatchForTile(int col, int row, Sprite sprite)
    {
        List<SpriteRenderer> result = new List<SpriteRenderer>();
        for (int i = col + 1; i < GridDimension; i++)
        {
            SpriteRenderer next = GetSpriteRendererAt(i, row);
            if (next == null || next.sprite != sprite) break;
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
            if (next == null || next.sprite != sprite) break;
            result.Add(next);
        }
        return result;
    }

    // Изменено: теперь метод возвращает максимальную длину собранной линии (0, если совпадений нет)
    int CheckMatches()
    {
        HashSet<SpriteRenderer> matchedTiles = new HashSet<SpriteRenderer>();
        int maxMatchLength = 0;

        for (int row = 0; row < GridDimension; row++)
        {
            for (int column = 0; column < GridDimension; column++)
            {
                SpriteRenderer current = GetSpriteRendererAt(column, row);
                if (current == null || current.sprite == null) continue;

                // Проверка по горизонтали
                List<SpriteRenderer> h = FindColumnMatchForTile(column, row, current.sprite);
                if (h.Count >= 2)
                {
                    matchedTiles.UnionWith(h);
                    matchedTiles.Add(current);
                    int length = h.Count + 1;
                    if (length > maxMatchLength) maxMatchLength = length;
                }

                // Проверка по вертикали
                List<SpriteRenderer> v = FindRowMatchForTile(column, row, current.sprite);
                if (v.Count >= 2)
                {
                    matchedTiles.UnionWith(v);
                    matchedTiles.Add(current);
                    int length = v.Count + 1;
                    if (length > maxMatchLength) maxMatchLength = length;
                }
            }
        }

        foreach (SpriteRenderer renderer in matchedTiles)
            renderer.sprite = null;

        if (matchedTiles.Count > 0)
            Score += matchedTiles.Count;

        return maxMatchLength;
    }

    IEnumerator MoveTileRoutine(GameObject tile, Vector3 targetPos)
    {
        while (Vector3.Distance(tile.transform.position, targetPos) > 0.01f)
        {
            tile.transform.position = Vector3.MoveTowards(tile.transform.position, targetPos, AnimationSpeed * Time.deltaTime);
            yield return null;
        }
        tile.transform.position = targetPos;
    }

    IEnumerator FillHolesRoutine()
    {
        List<Coroutine> animations = new List<Coroutine>();

        for (int column = 0; column < GridDimension; column++)
        {
            int emptyCount = 0;
            for (int row = 0; row < GridDimension; row++)
            {
                SpriteRenderer sr = GetSpriteRendererAt(column, row);
                if (sr.sprite == null)
                {
                    emptyCount++;
                }
                else if (emptyCount > 0)
                {
                    SpriteRenderer target = GetSpriteRendererAt(column, row - emptyCount);
                    target.sprite = sr.sprite;
                    sr.sprite = null;

                    GameObject fallingTile = Grid[column, row - emptyCount];
                    Vector3 startPos = GetWorldPosition(column, row);
                    Vector3 endPos = GetWorldPosition(column, row - emptyCount);
                    fallingTile.transform.position = startPos;
                    animations.Add(StartCoroutine(MoveTileRoutine(fallingTile, endPos)));
                }
            }

            for (int row = GridDimension - emptyCount; row < GridDimension; row++)
            {
                SpriteRenderer target = GetSpriteRendererAt(column, row);
                target.sprite = Sprites[Random.Range(0, Sprites.Count)];

                GameObject tile = Grid[column, row];
                Vector3 endPos = GetWorldPosition(column, row);
                Vector3 startPos = endPos + new Vector3(0, 3f, 0);
                tile.transform.position = startPos;
                animations.Add(StartCoroutine(MoveTileRoutine(tile, endPos)));
            }
        }

        foreach (Coroutine c in animations)
            yield return c;
    }

    void GameOver()
    {
        IsGameOver = true;
        Debug.Log("GAME OVER");
        PlayerPrefs.SetInt("score", Score);
        if (GameOverMenu != null) GameOverMenu.SetActive(true);
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySound(SoundType.TypeGameOver);
    }

    public void SwapTiles(Vector2Int tile1Position, Vector2Int tile2Position)
    {
        if (IsGameOver || IsBusy) return;
        StartCoroutine(SwapTilesRoutine(tile1Position, tile2Position));
    }

    IEnumerator SwapTilesRoutine(Vector2Int tile1Position, Vector2Int tile2Position)
    {
        IsBusy = true;

        GameObject tile1 = Grid[tile1Position.x, tile1Position.y];
        GameObject tile2 = Grid[tile2Position.x, tile2Position.y];
        SpriteRenderer renderer1 = tile1.GetComponent<SpriteRenderer>();
        SpriteRenderer renderer2 = tile2.GetComponent<SpriteRenderer>();

        Vector3 pos1 = tile1.transform.position;
        Vector3 pos2 = tile2.transform.position;

        yield return StartCoroutine(AnimateSwap(tile1, tile2, pos1, pos2));

        Sprite temp = renderer1.sprite;
        renderer1.sprite = renderer2.sprite;
        renderer2.sprite = temp;

        tile1.transform.position = pos1;
        tile2.transform.position = pos2;

        int initialMatchLength = CheckMatches();

        if (initialMatchLength == 0)
        {
            // Отмена хода, если совпадений нет
            yield return StartCoroutine(AnimateSwap(tile1, tile2, pos1, pos2));

            temp = renderer1.sprite;
            renderer1.sprite = renderer2.sprite;
            renderer2.sprite = temp;

            tile1.transform.position = pos1;
            tile2.transform.position = pos2;

            if (SoundManager.Instance != null) SoundManager.Instance.PlaySound(SoundType.TypePop);
        }
        else
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySound(SoundType.TypeMove);

            // Логика подчета ходов:
            if (initialMatchLength == 3)
            {
                NumMoves--; // 3 в ряд — минус ход
            }
            else if (initialMatchLength >= 5)
            {
                NumMoves++; // 5 в ряд и больше — плюс ход
            }
            // При initialMatchLength == 4 ничего не делаем (ход не отнимается)

            int loopSafety = 0;
            bool matched;
            do
            {
                yield return StartCoroutine(FillHolesRoutine());
                matched = CheckMatches() > 0;
                loopSafety++;
                if (loopSafety > 20) break;
            } while (matched);

            if (NumMoves <= 0)
            {
                NumMoves = 0;
                GameOver();
            }
        }

        IsBusy = false;
    }

    IEnumerator AnimateSwap(GameObject a, GameObject b, Vector3 posA, Vector3 posB)
    {
        Coroutine ca = StartCoroutine(MoveTileRoutine(a, posB));
        Coroutine cb = StartCoroutine(MoveTileRoutine(b, posA));
        yield return ca;
        yield return cb;
    }

    void ShuffleBoard()
    {
        if (IsGameOver || IsBusy) return;
        if (NumMoves <= 0) return;

        StartCoroutine(ShuffleRoutine());
    }

    IEnumerator ShuffleRoutine()
    {
        IsBusy = true;

        List<Sprite> allSprites = new List<Sprite>();
        for (int row = 0; row < GridDimension; row++)
        {
            for (int column = 0; column < GridDimension; column++)
            {
                allSprites.Add(GetSpriteRendererAt(column, row).sprite);
            }
        }

        for (int i = allSprites.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Sprite temp = allSprites[i];
            allSprites[i] = allSprites[j];
            allSprites[j] = temp;
        }

        int index = 0;
        for (int row = 0; row < GridDimension; row++)
        {
            for (int column = 0; column < GridDimension; column++)
            {
                GetSpriteRendererAt(column, row).sprite = allSprites[index];
                index++;
            }
        }

        NumMoves--;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound(SoundType.TypeMove);

        int loopSafety = 0;
        bool matched = CheckMatches() > 0;
        while (matched)
        {
            yield return StartCoroutine(FillHolesRoutine());
            matched = CheckMatches() > 0;
            loopSafety++;
            if (loopSafety > 20) break;
        }

        if (NumMoves <= 0)
        {
            NumMoves = 0;
            GameOver();
        }

        IsBusy = false;
    }
}