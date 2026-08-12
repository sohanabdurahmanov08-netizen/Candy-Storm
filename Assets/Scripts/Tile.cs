using UnityEngine;

public class Tile : MonoBehaviour
{
    private SpriteRenderer Renderer;
    public Vector2Int Position;

    private static Tile selected = null;

    void Awake()
    {
        Renderer = GetComponent<SpriteRenderer>();
    }

    void Select()
    {
        Renderer.color = Color.gray;
    }

    void Unselect()
    {
        Renderer.color = Color.white;
    }

    private void OnMouseDown()
    {
        if (GridManager.Instance == null || GridManager.Instance.IsGameOver || GridManager.Instance.IsBusy)
            return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound(SoundType.TypeSelect);

        if (selected != null)
        {
            if (selected == this) return;
            selected.Unselect();

            if (Vector2Int.Distance(selected.Position, Position) == 1)
            {
                GridManager.Instance.SwapTiles(Position, selected.Position);
                selected = null;
            }
            else
            {
                selected = this;
                Select();
            }
        }
        else
        {
            selected = this;
            Select();
        }
    }
}