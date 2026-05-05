using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class TimelineCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public TimelineEventData eventData;

    Image background;
    TMP_Text titleText;
    TMP_Text yearText;
    RectTransform rect;
    CanvasGroup canvasGroup;
    Canvas rootCanvas;
    Transform listParent;
    bool dragEnabled = true;

    Color baseColor;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        background = GetComponent<Image>();
        if (background != null) baseColor = background.color;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        rootCanvas = GetComponentInParent<Canvas>();

        var titleT = transform.Find("TitleText");
        if (titleT != null) titleText = titleT.GetComponent<TMP_Text>();
        var yearT = transform.Find("YearText");
        if (yearT != null) yearText = yearT.GetComponent<TMP_Text>();
    }

    public void Setup(TimelineEventData data, Transform listParentTransform)
    {
        eventData = data;
        listParent = listParentTransform;
        if (titleText != null) titleText.text = data.title;
        if (yearText != null)
        {
            yearText.text = "";
            yearText.gameObject.SetActive(false);
        }
        if (background != null) background.color = baseColor;
        dragEnabled = true;
    }

    public void Reveal(bool isInCorrectPosition, Color correctColor, Color wrongColor)
    {
        dragEnabled = false;
        if (yearText != null)
        {
            yearText.text = FormatYear(eventData.year);
            yearText.color = isInCorrectPosition ? correctColor : wrongColor;
            yearText.gameObject.SetActive(true);
        }
        if (background != null)
        {
            var c = isInCorrectPosition ? correctColor : wrongColor;
            background.color = new Color(c.r, c.g, c.b, 0.18f) + new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a) * 0.85f;
        }
    }

    static string FormatYear(int year)
    {
        if (year < 0) return $"{-year} BCE";
        return year.ToString();
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (!dragEnabled || rootCanvas == null || listParent == null) return;
        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.92f;
    }

    public void OnDrag(PointerEventData e)
    {
        if (!dragEnabled) return;
        rect.anchoredPosition += e.delta / rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!dragEnabled || listParent == null) return;
        int newIndex = ComputeNewIndex();
        transform.SetParent(listParent, false);
        transform.SetSiblingIndex(newIndex);
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
    }

    int ComputeNewIndex()
    {
        float myY = rect.position.y;
        int n = listParent.childCount;
        int newIndex = n;
        for (int i = 0; i < n; i++)
        {
            var sib = listParent.GetChild(i);
            if (!sib.gameObject.activeSelf) continue;
            if (sib == transform) continue;
            float sibY = ((RectTransform)sib).position.y;
            if (myY > sibY)
            {
                newIndex = i;
                break;
            }
        }
        return Mathf.Clamp(newIndex, 0, n - 1);
    }
}
