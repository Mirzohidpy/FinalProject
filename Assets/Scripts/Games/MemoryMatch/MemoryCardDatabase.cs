using UnityEngine;

[CreateAssetMenu(fileName = "MemoryCardDatabase", menuName = "BrainCitizen/Memory Card Database")]
public class MemoryCardDatabase : ScriptableObject
{
    public MemoryCardData[] cards;
}
