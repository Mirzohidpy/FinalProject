using UnityEngine;

[CreateAssetMenu(fileName = "MazeQuestionDatabase", menuName = "BrainCitizen/Maze Question Database")]
public class MazeQuestionDatabase : ScriptableObject
{
    public MazeQuestionData[] questions;
}
