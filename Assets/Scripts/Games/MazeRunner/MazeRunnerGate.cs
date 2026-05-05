using UnityEngine;

public class MazeRunnerGate : MonoBehaviour
{
    public int gateIndex;
    bool triggered;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (other.GetComponent<MazeRunnerPlayer>() == null) return;
        triggered = true;
        var gm = FindFirstObjectByType<MazeRunnerGameManager>();
        if (gm != null) gm.OnGateEntered(this);
    }

    public void Open()
    {
        gameObject.SetActive(false);
    }

    public void ResetTrigger()
    {
        triggered = false;
    }
}
