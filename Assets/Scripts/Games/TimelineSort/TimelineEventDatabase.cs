using UnityEngine;

[CreateAssetMenu(fileName = "TimelineEventDatabase", menuName = "BrainCitizen/Timeline Event Database")]
public class TimelineEventDatabase : ScriptableObject
{
    public TimelineEventData[] events;
}
