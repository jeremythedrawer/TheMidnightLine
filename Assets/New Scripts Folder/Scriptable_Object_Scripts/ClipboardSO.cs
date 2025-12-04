using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Clipboard_SO", menuName = "Midnight Line SOs / Clipbaord SO")]
public class ClipboardSO : ScriptableObject
{
    public float onScreenYPos = 300;
    public float moveTime = 1;
    public List<ProfilePage> profilePages;

}
