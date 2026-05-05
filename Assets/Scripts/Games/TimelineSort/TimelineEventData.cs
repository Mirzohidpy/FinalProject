using UnityEngine;

public enum TimelineEra { Ancient, EarlyModern, Modern }

[CreateAssetMenu(fileName = "TimelineEvent", menuName = "BrainCitizen/Timeline Event")]
public class TimelineEventData : ScriptableObject
{
    public string title;
    public int year;          // negative for BCE (e.g. -753 = 753 BCE)
    public TimelineEra era;
    [TextArea(2, 4)]
    public string explanation;
}
