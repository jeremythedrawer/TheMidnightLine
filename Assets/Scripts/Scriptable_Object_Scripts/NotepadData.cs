using UnityEngine;
using static AtlasUI;

[CreateAssetMenu(fileName = "NotepadData", menuName = "Midnight Line SOs / Notepad")]
public class NotepadData : ScriptableObject
{
   public NotepadState curState;
   public NotepadState prevState;
}
