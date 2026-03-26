using UnityEngine;

/// <summary>
/// ScriptableObject that holds the full pool of headlines.
/// Create one asset and assign HeadlineData assets into the list.
/// Right-click in Project > Create > BrainCitizen > Headline Database
/// </summary>
[CreateAssetMenu(fileName = "HeadlineDatabase", menuName = "BrainCitizen/Headline Database")]
public class HeadlineDatabase : ScriptableObject
{
    public HeadlineData[] headlines;
}
