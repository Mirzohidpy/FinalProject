using UnityEngine;

[CreateAssetMenu(fileName = "CivicQuestion", menuName = "BrainCitizen/Civic Quiz Question")]
public class CivicQuizQuestion : ScriptableObject
{
    [TextArea(2, 4)]
    public string question;
    public string[] options;
    public int correctIndex;
    [Range(1, 5)]
    public int difficulty = 1;
    [TextArea(2, 4)]
    public string hint;
}
