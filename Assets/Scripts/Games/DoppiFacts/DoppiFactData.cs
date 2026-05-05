using UnityEngine;

[CreateAssetMenu(fileName = "DoppiFact", menuName = "BrainCitizen/Doppi Fact")]
public class DoppiFactData : ScriptableObject
{
    [TextArea(2, 4)]
    public string statement;
    public bool isTrue;
    [TextArea(2, 4)]
    public string explanation;
}
