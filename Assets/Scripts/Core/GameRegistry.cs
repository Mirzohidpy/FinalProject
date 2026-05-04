using UnityEngine;

/// <summary>
/// The full list of mini-games shown in the hub, in display order.
/// </summary>
[CreateAssetMenu(fileName = "GameRegistry", menuName = "BrainCitizen/Game Registry")]
public class GameRegistry : ScriptableObject
{
    public GameInfo[] games;
}
