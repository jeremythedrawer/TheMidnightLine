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
    public PlayerInputsSO playerInputs;

    public AtlasTextRenderer scoreRenderer;
    public AtlasTextRenderer thankYouRenderer;

    [Header("Generated")]
    public Notepad notepad;

    public CancellationTokenSource ctsFadeBlack;
    public CancellationTokenSource ctsNotepad;

    public Vector3 naturalMovePos;

    public UIState curState;

    public int traitorsFound;

    private void Start()
    {
        FadeFromBlack();
        GetNotepad();
        GetScore();
        SetTexts();
    }
    private void OnEnable()
    {
        OnFinishFadeFromBlack += SetToNotepadState;
        Notepad.OnFinishRevealingOutcomes += WriteTraitorsFoundScore;
    }
    private void OnDisable()
    {
        OnFinishFadeFromBlack -= SetToNotepadState;
        Notepad.OnFinishRevealingOutcomes -= WriteTraitorsFoundScore;
    }
    private void Update()
    {
        UpdateState();
    }
    private void GetNotepad()
    {
        notepad = SceneController.GetNotepad(transform);
    }
    private void GetScore()
    {
        for (int i = 0; i < trip.traitorProfiles.Length; i++)
        {
            if (trip.traitorProfiles[i].found)
            {
                traitorsFound++;
            }
        }
    }
    private void SetTexts()
    {
        scoreRenderer.SetText("");
        thankYouRenderer.SetText("");
    }
    private void FadeFromBlack()
    {
        FadeBlack(fadeBlackMaterial, ctsFadeBlack, toFadeBlack: false);
    }
    public void WriteTraitorsFoundScore()
    {
        string text = "Traitors found: " + traitorsFound + " / " + trip.traitorProfiles.Length;
        scoreRenderer.WriteText(text, Notepad.WRITE_LETTER_TIME);
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

        if (scoreRenderer.completedWritingText && !thankYouRenderer.hasText && (playerInputs.mouseLeftDown || playerInputs.spacebarDown || playerInputs.mouseRightDown))
        {
            string text = "Thank you for playing! - Jeremy Edwards 2026";
            thankYouRenderer.WriteText(text, Notepad.WRITE_LETTER_TIME);
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
