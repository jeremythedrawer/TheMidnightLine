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

    public SceneData sceneData;

    public AtlasTextRenderer scoreRenderer;
    public AtlasTextRenderer thankYouRenderer;
    public AtlasTextRenderer playAgainRenderer;
    public AtlasTextRenderer quitRenderer;

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
        HandleTextSkips();
        HandleQuitButton();
        HandlePlayAgainButton();
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
        playAgainRenderer.SetText("");
        quitRenderer.SetText("");
    }
    private void FadeFromBlack()
    {
        FadeBlack(fadeBlackMaterial, ctsFadeBlack, toFadeBlack: false);
    }
    public void WriteTraitorsFoundScore()
    {
        string text = "Traitors found: " + traitorsFound + " / " + trip.traitorProfiles.Length;
        scoreRenderer.WriteText(text, Notepad.WRITE_LETTER_TIME, WriteThankYou);
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
                notepad.EnterNotepad();
                naturalMovePos = NotepadActiveLocalPos;
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
                UpdateNaturalPos(NotepadActiveLocalPos, ref naturalMovePos);
                notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, naturalMovePos, Time.deltaTime * MOVE_DAMP);
            }
            break;
        }
    }
    private void HandleTextSkips()
    {
        if (playerInputs.spacebarDown)
        {
            if (scoreRenderer.hasText && !scoreRenderer.completedWritingText)
            {
                scoreRenderer.ctsWrite.Cancel();
            }
            else if (thankYouRenderer.hasText && !thankYouRenderer.completedWritingText)
            {
                thankYouRenderer.ctsWrite.Cancel();
            }
            else if (playAgainRenderer.hasText && !playAgainRenderer.completedWritingText)
            {
                playAgainRenderer.ctsWrite.Cancel();
                quitRenderer.ctsWrite.Cancel();
            }
        }
    }
    private void WriteThankYou()
    {
        string text = "Thank you for playing! - Jeremy Edwards 2026";
        thankYouRenderer.WriteText(text, Notepad.WRITE_LETTER_TIME, WriteButtons);
    }
    private void WriteButtons()
    {
        WriteQuit();
        WritePlayAgain();
    }
    private void WriteQuit()
    {
        string text = "Quit";
        quitRenderer.WriteText(text, Notepad.WRITE_LETTER_TIME);
    }
    private void WritePlayAgain()
    {
        string text = "Play Again?";
        playAgainRenderer.WriteText(text, Notepad.WRITE_LETTER_TIME);
    }
    private void HandlePlayAgainButton()
    {
        if (CursorController.IsInsideBounds(playAgainRenderer.background_renderer.bounds))
        {
            playAgainRenderer.SetColorText(Color.white);
            playAgainRenderer.background_renderer.SetSliceCustom(w: 1);
            if (playerInputs.mouseLeftDown)
            {
                SetFadeToBlack();
                gameEventData.OnReset.Raise();
            }
        }
        else
        {
            playAgainRenderer.SetColorText(Color.black);
            playAgainRenderer.background_renderer.SetSliceCustom(w: 0);
        }
    }

    private void SetFadeToBlack()
    {
        SetFadeBlack(fadeBlackMaterial, toFadeBlack: true);
        Scenes.SetTripScene(sceneData);
    }
    private void HandleQuitButton()
    {
        if (CursorController.IsInsideBounds(quitRenderer.background_renderer.bounds))
        {
            quitRenderer.SetColorText(Color.white);
            quitRenderer.background_renderer.SetSliceCustom(w: 1);
            if (playerInputs.mouseLeftDown)
            {
                Application.Quit();
            }
        }
        else
        {
            quitRenderer.SetColorText(Color.black);
            quitRenderer.background_renderer.SetSliceCustom(w: 0);
        }
    }
    private void ExitState()
    {
        switch (curState)
        {
            case UIState.Notepad:
            {
                MoveUIElement(notepad.transform, NotepadInactiveLocalPos, ref ctsNotepad, curState);
                notepad.enabled = false;
            }
            break;
        }
    }
}
