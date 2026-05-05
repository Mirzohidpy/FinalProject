using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The 10x10 letter grid. Owns its cells, handles pointer drag input,
/// and fires a static event to the GameManager when the player releases
/// a selection.
/// </summary>
public class WordSearchGrid : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Layout")]
    public RectTransform cellsParent;
    public GameObject cellTemplate;

    [Header("Colors")]
    public Color cellNormalBg = Color.white;
    public Color cellNormalText = new Color(0.118f, 0.153f, 0.380f);
    public Color cellSelectedBg = new Color(0.180f, 0.800f, 0.443f);
    public Color cellSelectedText = Color.white;
    public Color cellFoundBg = new Color(0.180f, 0.800f, 0.443f, 0.85f);
    public Color cellFoundText = Color.white;

    public static event System.Action<int, int, int, int> OnPlayerSelection;

    int gridSize;
    Image[,] cellBgs;
    TMP_Text[,] cellTexts;
    bool[,] cellFound;
    bool dragging;
    Vector2Int startCell;
    Vector2Int currentEndCell;

    public void BuildLetters(char[,] grid)
    {
        int n = grid.GetLength(0);
        gridSize = n;

        // Wipe any existing clones (template stays as a child)
        if (cellsParent != null)
        {
            for (int i = cellsParent.childCount - 1; i >= 0; i--)
            {
                var child = cellsParent.GetChild(i).gameObject;
                if (child != cellTemplate) Destroy(child);
            }
        }

        cellBgs = new Image[n, n];
        cellTexts = new TMP_Text[n, n];
        cellFound = new bool[n, n];

        cellTemplate.SetActive(false);
        for (int r = 0; r < n; r++)
        {
            for (int c = 0; c < n; c++)
            {
                var go = Instantiate(cellTemplate, cellsParent);
                go.SetActive(true);
                go.name = $"Cell_{r}_{c}";

                var img = go.GetComponent<Image>();
                var txt = go.GetComponentInChildren<TMP_Text>(true);
                txt.text = grid[r, c].ToString();
                img.color = cellNormalBg;
                txt.color = cellNormalText;
                img.raycastTarget = false;

                cellBgs[r, c] = img;
                cellTexts[r, c] = txt;
            }
        }
    }

    public void MarkFound(int sr, int sc, int er, int ec)
    {
        foreach (var rc in CellsBetween(sr, sc, er, ec))
        {
            cellFound[rc.x, rc.y] = true;
            cellBgs[rc.x, rc.y].color = cellFoundBg;
            cellTexts[rc.x, rc.y].color = cellFoundText;
        }
    }

    public void ClearSelection()
    {
        if (cellBgs == null) return;
        for (int r = 0; r < gridSize; r++)
        {
            for (int c = 0; c < gridSize; c++)
            {
                if (cellFound[r, c])
                {
                    cellBgs[r, c].color = cellFoundBg;
                    cellTexts[r, c].color = cellFoundText;
                }
                else
                {
                    cellBgs[r, c].color = cellNormalBg;
                    cellTexts[r, c].color = cellNormalText;
                }
            }
        }
    }

    public void OnPointerDown(PointerEventData ev)
    {
        if (cellBgs == null) return;
        if (TryGetCell(ev, out var cell))
        {
            dragging = true;
            startCell = currentEndCell = cell;
            Repaint();
        }
    }

    public void OnDrag(PointerEventData ev)
    {
        if (!dragging) return;
        if (TryGetCell(ev, out var cell))
        {
            var snapped = SnapToLine(startCell, cell);
            if (snapped != currentEndCell)
            {
                currentEndCell = snapped;
                Repaint();
            }
        }
    }

    public void OnPointerUp(PointerEventData ev)
    {
        if (!dragging) return;
        dragging = false;
        OnPlayerSelection?.Invoke(startCell.x, startCell.y, currentEndCell.x, currentEndCell.y);
    }

    bool TryGetCell(PointerEventData ev, out Vector2Int cell)
    {
        cell = default;
        if (cellsParent == null) return false;

        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                cellsParent, ev.position, ev.pressEventCamera, out local))
            return false;

        var rect = cellsParent.rect;
        float u = (local.x - rect.xMin) / rect.width;
        float v = (rect.yMax - local.y) / rect.height;
        int col = Mathf.FloorToInt(u * gridSize);
        int row = Mathf.FloorToInt(v * gridSize);
        if (row < 0 || row >= gridSize || col < 0 || col >= gridSize) return false;
        cell = new Vector2Int(row, col);
        return true;
    }

    static Vector2Int SnapToLine(Vector2Int start, Vector2Int target)
    {
        int dr = target.x - start.x;
        int dc = target.y - start.y;
        if (dr == 0 && dc == 0) return start;

        int adr = Mathf.Abs(dr);
        int adc = Mathf.Abs(dc);
        int sdr = dr > 0 ? 1 : (dr < 0 ? -1 : 0);
        int sdc = dc > 0 ? 1 : (dc < 0 ? -1 : 0);

        if (adr > 2 * adc) return new Vector2Int(target.x, start.y);
        if (adc > 2 * adr) return new Vector2Int(start.x, target.y);
        int len = Mathf.Min(adr, adc);
        return new Vector2Int(start.x + sdr * len, start.y + sdc * len);
    }

    void Repaint()
    {
        ClearSelection();
        if (!dragging) return;
        foreach (var rc in CellsBetween(startCell.x, startCell.y, currentEndCell.x, currentEndCell.y))
        {
            cellBgs[rc.x, rc.y].color = cellSelectedBg;
            cellTexts[rc.x, rc.y].color = cellSelectedText;
        }
    }

    static IEnumerable<Vector2Int> CellsBetween(int sr, int sc, int er, int ec)
    {
        int dr = er - sr;
        int dc = ec - sc;
        int sdr = dr > 0 ? 1 : (dr < 0 ? -1 : 0);
        int sdc = dc > 0 ? 1 : (dc < 0 ? -1 : 0);
        int steps = Mathf.Max(Mathf.Abs(dr), Mathf.Abs(dc));
        for (int i = 0; i <= steps; i++)
            yield return new Vector2Int(sr + sdr * i, sc + sdc * i);
    }
}
