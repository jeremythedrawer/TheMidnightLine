using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ActionData", menuName = "Data / Actions")]
public class ActionData : ScriptableObject
{
    public Action onShowKeyIcon;
    public Action onHideKeyIcon;
    public Action onOpenDialogueBubble;
    public Action onCloseDialogueBubble;

}
