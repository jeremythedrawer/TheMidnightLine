using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ActionData", menuName = "Data / Actions")]
public class ActionData : ScriptableObject
{
    public Action onShowKeyIcon;
    public Action onHideKeyIcon;

    public Action onOpenDialogueBubble;
    public Action onCloseDialogueBubble;
    public Action onGiveNotepad;
    public Action onShowCarriageMap;
    public Action onHideCarriageMap;
    public Action onBeginTrip;
    public Action onCreatedPassengerProfiles;

    public Action onFocus;
    public Action onUnfocus;
    public Action onFocusSwitchPassenger;
}
