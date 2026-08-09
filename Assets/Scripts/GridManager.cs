using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    public List<Sprite> Sprites = new List<Sprite>();
    public GameObject TilePrefab;
    public int GridDimension = 8;
    public float Distance = 1.0f;
    private GameObject[,] Grid;

    void Awake()
    {
        Instance = this;
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

    // Возвращает SpriteRenderer клетки по координатам, или null если координаты вне сетки
    SpriteRenderer GetSpriteRendererAt(int column, int row)
    {
        if (column < 0 || column >= GridDimension || row < 0 || row >= GridDimension)
            return null;

        GameObject tile = Grid[column, row];
        SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
        return renderer;
    }

    // Ищет подряд идущие одинаковые клетки вдоль столбца (по горизонтали, увеличивая column)
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

    // Ищет подряд идущие одинаковые клетки вдоль строки (по вертикали, увеличивая row)
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

    // Проверяет всю сетку на совпадения от 3 и более, удаляет их (sprite = null)
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

        return matchedTiles.Count > 0;
    }

    // Сдвигает клетки вниз, заполняя пустоты новыми случайными спрайтами сверху
    void FillHoles()
    {
        for (int column = 0; column < GridDimension; column++)
        {
            for (int row = 0; row < GridDimension; row++)
            {
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
                }
            }
        }
    }

    public void SwapTiles(Vector2Int tile1Position, Vector2Int tile2Position)
    {
        GameObject tile1 = Grid[tile1Position.x, tile1Position.y];
        SpriteRenderer renderer1 = tile1.GetComponent<SpriteRenderer>();

        GameObject tile2 = Grid[tile2Position.x, tile2Position.y];
        SpriteRenderer renderer2 = tile2.GetComponent<SpriteRenderer>();

        // Меняем спрайты местами
        Sprite temp = renderer1.sprite;
        renderer1.sprite = renderer2.sprite;
        renderer2.sprite = temp;

        // Проверяем, появилось ли совпадение после обмена
        bool changesOccur = CheckMatches();

        if (!changesOccur)
        {
            // Совпадений нет — отменяем обмен, возвращаем как было
            temp = renderer1.sprite;
            renderer1.sprite = renderer2.sprite;
            renderer2.sprite = temp;
        }
        else
        {
            // Совпадение есть — заполняем дыры и проверяем цепные реакции
            do
            {
                FillHoles();
            } while (CheckMatches());
        }
    }
}