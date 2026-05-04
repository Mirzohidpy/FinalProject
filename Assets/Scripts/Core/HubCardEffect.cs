using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Hover and click feedback for hub cards: smooth scale on pointer enter,
/// press-down on pointer down. Disables itself for locked cards.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class HubCardEffect : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    public bool interactable = true;
    public float hoverScale = 1.035f;
    public float pressScale = 0.97f;
    public float lerpSpeed = 14f;

    RectTransform rt;
    Vector3 targetScale = Vector3.one;
    bool hovering;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        targetScale = Vector3.one;
        hovering = false;
        if (rt != null) rt.localScale = Vector3.one;
    }

    void Update()
    {
        rt.localScale = Vector3.Lerp(rt.localScale, targetScale,
            Time.unscaledDeltaTime * lerpSpeed);
    }

    public void OnPointerEnter(PointerEventData _)
    {
        if (!interactable) return;
        hovering = true;
        targetScale = Vector3.one * hoverScale;
    }

    public void OnPointerExit(PointerEventData _)
    {
        if (!interactable) return;
        hovering = false;
        targetScale = Vector3.one;
    }

    public void OnPointerDown(PointerEventData _)
    {
        if (!interactable) return;
        targetScale = Vector3.one * pressScale;
    }

    public void OnPointerUp(PointerEventData _)
    {
        if (!interactable) return;
        targetScale = hovering ? Vector3.one * hoverScale : Vector3.one;
    }
}
