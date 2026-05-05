using UnityEngine;

[CreateAssetMenu(fileName = "CivicQuizDatabase", menuName = "BrainCitizen/Civic Quiz Database")]
public class CivicQuizDatabase : ScriptableObject
{
    public CivicQuizQuestion[] questions;
}
