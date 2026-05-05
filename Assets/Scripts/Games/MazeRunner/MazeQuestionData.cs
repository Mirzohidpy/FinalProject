using UnityEngine;

[CreateAssetMenu(fileName = "MazeQuestion", menuName = "BrainCitizen/Maze Question")]
public class MazeQuestionData : ScriptableObject
{
    [TextArea(2, 4)]
    public string question;
    public string[] options;
    public int correctIndex;
    [TextArea(2, 4)]
    public string explanation;
}
