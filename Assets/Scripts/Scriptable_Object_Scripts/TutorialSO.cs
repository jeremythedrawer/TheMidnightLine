using UnityEngine;


[CreateAssetMenu(fileName = "TutorialSO", menuName = "Midnight Line SOs / Tutorial SO")]

public class TutorialSO : ScriptableObject
{
    public TextAsset conversation;
    internal string[] lines;
    internal int curConvoIndex;
    internal int prevConvoIndex;
}
