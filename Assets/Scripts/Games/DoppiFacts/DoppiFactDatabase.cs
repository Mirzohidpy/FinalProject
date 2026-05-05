using UnityEngine;

[CreateAssetMenu(fileName = "DoppiFactDatabase", menuName = "BrainCitizen/Doppi Fact Database")]
public class DoppiFactDatabase : ScriptableObject
{
    public DoppiFactData[] facts;
}
