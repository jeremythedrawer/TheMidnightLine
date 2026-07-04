using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using static AtlasUI;
public class ScoreUI : MonoBehaviour
{
    public Material fadeBlackMaterial;

    public TripSO trip;
    public GameEventDataSO gameEventData;
    public CameraStatsSO cameraStats;
    
    public AtlasTextRenderer scoreRenderer;

    [Header("Generated")]
    public Notepad notepad;

    public CancellationTokenSource ctsFadeBlack;
    public CancellationTokenSource ctsNotepad;

    public Vector3 naturalMovePos;

    public UIState curState;

    public int traitorsRevealed;
    private void Start()
    {
        FadeFromBlack();
        GetNotepad();
    }
    private void OnEnable()
    {
        gameEventData.OnTraitorsFoundScoreUpdate.RegisterListener(SetTraitorsFoundScore);
        OnFinishFadeFromBlack += SetToNotepadState;
    }
    private void OnDisable()
    {
        gameEventData.OnTraitorsFoundScoreUpdate.UnregisterListener(SetTraitorsFoundScore);
        OnFinishFadeFromBlack -= SetToNotepadState;
    }
    private void Update()
    {
        UpdateState();
    }
    private void GetNotepad()
    {
        notepad = SceneController.GetNotepad(transform);
    }
    private void FadeFromBlack()
    {
        FadeBlack(fadeBlackMaterial, ctsFadeBlack, toFadeBlack: false);
    }
    public void InitTraitorsFoundScore()
    {
        scoreRenderer.SetText("Traitors found: " + 0 + " / " + trip.traitorProfiles.Length);
    }
    public void SetTraitorsFoundScore()
    {
        traitorsRevealed++;
        scoreRenderer.SetText("Traitors found: " + traitorsRevealed + " / " + trip.traitorProfiles.Length);
    }
    private void SetToNotepadState()
    {
        SetState(UIState.Notepad);
    }
    private void SetState(UIState newState)
    {
        if (curState == newState) return;
        ExitState();
        curState = newState;
        EnterState();
    }
    public void EnterState()
    {
        switch (curState)
        {
            case UIState.Notepad:
            {
                notepad.gameObject.SetActive(true);
                notepad.enabled = true;
                notepad.active = true;
                naturalMovePos = NotepadActivePos;
                ctsNotepad?.Cancel();
            }
            break;
            case UIState.None:
            {
            }
            break;
        }
    }
    private void UpdateState()
    {
        switch (curState)
        {
            case UIState.Notepad:
            {
                UpdateNaturalPos(NotepadActivePos, ref naturalMovePos);
                notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, naturalMovePos, Time.deltaTime * MOVE_DAMP);
            }
            break;
        }
    }
    private void ExitState()
    {
        switch (curState)
        {
            case UIState.Notepad:
            {
                MoveUIElement(notepad, NotepadInactivePos, ref ctsNotepad, curState);
                notepad.enabled = false;
            }
            break;
        }
    }
}
