using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ActionData", menuName = "Data / Actions")]
public class ActionData : ScriptableObject
{
    public Action onShowKeyIcon;
    public Action onHideKeyIcon;

    public Action onOpenDialogueBubble;
    public Action onCloseDialogueBubble;
    public Action onBeginTrip;
    public Action onCreatedPassengerProfiles;
    public Action onAtFirstStation;

    public Action onTraitorBoarded;
    public Action onTraitorDisembarked;
}
