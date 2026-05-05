using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime card. Spawned per level by the UIManager from a template baked
/// into the scene. Handles the flip animation locally and reports clicks
/// upward via a static event.
/// </summary>
public class MemoryCard : MonoBehaviour
{
    public Image background;
    public Image faceImage;
    public TMP_Text backLabel;
    public Button button;

    [Header("Colors")]
    public Color backgroundFaceDownColor = new Color(0.118f, 0.153f, 0.380f);
    public Color backgroundFaceUpColor = Color.white;
    public Color backgroundMatchedColor = new Color(0.180f, 0.800f, 0.443f);

    [Header("Animation")]
    public float flipDuration = 0.3f;

    public static event System.Action<int> OnClicked;

    int cardIndex;
    bool isMatched;
    bool isFaceUp;
    Coroutine flipRoutine;

    public void Setup(int index, Sprite faceSprite)
    {
        cardIndex = index;
        if (faceImage != null) faceImage.sprite = faceSprite;
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(InvokeClicked);
            button.interactable = true;
        }
        isMatched = false;
        SetVisualFaceUp(false);
        ((RectTransform)transform).localScale = Vector3.one;
    }

    void InvokeClicked() => OnClicked?.Invoke(cardIndex);

    public void Flip(bool faceUp)
    {
        if (isMatched && !faceUp) return;
        if (flipRoutine != null) StopCoroutine(flipRoutine);
        flipRoutine = StartCoroutine(FlipCoroutine(faceUp));
    }

    public void MarkMatched()
    {
        isMatched = true;
        if (background != null) background.color = backgroundMatchedColor;
        if (button != null) button.interactable = false;
    }

    IEnumerator FlipCoroutine(bool toFaceUp)
    {
        float half = flipDuration * 0.5f;
        var rt = (RectTransform)transform;

        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(1f, 0f, t / half);
            rt.localScale = new Vector3(s, 1f, 1f);
            yield return null;
        }

        SetVisualFaceUp(toFaceUp);

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(0f, 1f, t / half);
            rt.localScale = new Vector3(s, 1f, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
        flipRoutine = null;
    }

    void SetVisualFaceUp(bool up)
    {
        isFaceUp = up;
        if (faceImage != null) faceImage.gameObject.SetActive(up);
        if (backLabel != null) backLabel.gameObject.SetActive(!up);
        if (background != null && !isMatched)
            background.color = up ? backgroundFaceUpColor : backgroundFaceDownColor;
    }
}
