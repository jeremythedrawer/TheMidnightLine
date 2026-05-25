using UnityEngine;

[CreateAssetMenu(fileName = "NotepadData", menuName = "Midnight Line SOs / Notepad")]
public class NotepadData : ScriptableObject
{
    public enum State
    {
        None,
        Stationary,
        Writing,
        Erasing,
        FlippingUp,
        FlippingDown,
    }

    public State curState;
}
