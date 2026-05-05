using UnityEngine;

public class MazeRunnerGoal : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<MazeRunnerPlayer>() == null) return;
        var gm = FindFirstObjectByType<MazeRunnerGameManager>();
        if (gm != null) gm.OnGoalReached();
    }
}
