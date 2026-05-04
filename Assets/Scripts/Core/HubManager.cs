using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds the hub UI from the GameRegistry at runtime.
/// Groups games by category into sections, each containing a 2-column grid of cards.
/// </summary>
public class HubManager : MonoBehaviour
{
    [Header("Data")]
    public GameRegistry registry;

    [Header("UI")]
    public Transform sectionsContainer;
    public GameObject sectionTemplate;   // child must include "HeaderText" (TMP) and "Grid" (GridLayoutGroup)
    public GameObject cardTemplate;

    [Header("Ready vs Locked colors")]
    public Color readyAccent   = new Color(0.153f, 0.682f, 0.376f);
    public Color lockedAccent  = new Color(0.5f,   0.5f,   0.6f);
    public Color cardReadyBg   = new Color(1f,     1f,     1f,    0.96f);
    public Color cardLockedBg  = new Color(0.85f,  0.85f,  0.9f,  0.45f);
    public Color textPrimary   = new Color(0.118f, 0.153f, 0.380f);
    public Color textLocked    = new Color(0.45f,  0.45f,  0.5f);

    void Start()
    {
        if (registry == null || registry.games == null)
        {
            Debug.LogError("HubManager has no GameRegistry assigned.");
            return;
        }

        sectionTemplate.SetActive(false);
        cardTemplate.SetActive(false);

        var byCategory = new Dictionary<GameCategory, List<GameInfo>>();
        foreach (var game in registry.games)
        {
            if (game == null) continue;
            if (!byCategory.TryGetValue(game.category, out var list))
            {
                list = new List<GameInfo>();
                byCategory[game.category] = list;
            }
            list.Add(game);
        }

        BuildSection(GameCategory.CivicAwareness, "CIVIC AWARENESS", byCategory);
        BuildSection(GameCategory.MentalSkills,   "MENTAL SKILLS",   byCategory);
    }

    void BuildSection(GameCategory category, string heading,
        Dictionary<GameCategory, List<GameInfo>> byCategory)
    {
        if (!byCategory.TryGetValue(category, out var games) || games.Count == 0)
            return;

        var section = Instantiate(sectionTemplate, sectionsContainer);
        section.SetActive(true);
        section.name = $"Section_{category}";

        foreach (var t in section.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.name == "HeaderText")
            {
                t.text = heading;
                break;
            }
        }

        var grid = section.transform.Find("Grid");
        if (grid == null)
        {
            Debug.LogError("Section template missing a 'Grid' child.");
            return;
        }

        foreach (var game in games)
            BuildCard(grid, game);
    }

    void BuildCard(Transform parent, GameInfo game)
    {
        var card = Instantiate(cardTemplate, parent);
        card.name = $"Card_{game.gameNumber:D2}_{game.sceneName}";
        card.SetActive(true);

        var image = card.GetComponent<Image>();
        if (image != null)
            image.color = game.isImplemented ? cardReadyBg : cardLockedBg;

        Color accent = game.isImplemented ? readyAccent : lockedAccent;
        Color textColor = game.isImplemented ? textPrimary : textLocked;

        foreach (var t in card.GetComponentsInChildren<TMP_Text>(true))
        {
            switch (t.name)
            {
                case "NumberText":
                    t.text = game.gameNumber.ToString("D2");
                    t.color = accent;
                    break;
                case "NameText":
                    t.text = game.displayName;
                    t.color = textColor;
                    break;
                case "TaglineText":
                    t.text = game.tagline;
                    t.color = textColor;
                    break;
                case "StatusText":
                    t.text = game.isImplemented ? "PLAY" : "LOCKED";
                    t.color = accent;
                    break;
            }
        }

        var button = card.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = game.isImplemented;
            if (game.isImplemented)
            {
                string sceneName = game.sceneName;
                button.onClick.AddListener(() => SceneManager.LoadScene(sceneName));
            }
        }

        var effect = card.GetComponent<HubCardEffect>();
        if (effect != null) effect.interactable = game.isImplemented;
    }
}
