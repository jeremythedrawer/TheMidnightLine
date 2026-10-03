using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ActionData", menuName = "Data / Actions")]
public class ActionData : ScriptableObject
{
    public Action onShowKeyIcon;
    public Action onHideKeyIcon;
    public Action onSuspect;
    public Action onUnsuspect;
    
    public Action onFocusPassenger;
    public Action onUnmaskPassenger;
    public Action onSuspectPassenger;
    public Action onUnsuspectPassenger;

    public Action onFocusCarriage;
    public Action onCloseDialogueBubble;
    public Action onBeginTrip;
    public Action onCreatedPassengerProfiles;
    public Action onAtFirstStation;

    public Action onTraitorBoarded;
    public Action onTraitorDisembarked;
}
