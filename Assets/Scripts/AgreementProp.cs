using System;
using UnityEngine;

using static Scenes;
public class AgreementProp : MonoBehaviour
{
    public static event Action<Vector2> OnSpyEnter;
    public static event Action OnSpyExit;
    public static event Action OnAgreementCollect;
    public static event Action OnNotepadReturn;

    public AtlasRenderer atlasRenderer;
    public MeshRenderer shinyRenderer;

    public SceneData sceneData;
    public GameEventDataSO gameEventData;
    public NotepadData notepadData;
    public SpyStatsSO spyStats;


    [Header("Generated")]
    public bool spyAtProp;

    private void OnEnable()
    {
        Scenes.OnLoadStart += StartSceneInit;
        Scenes.OnLoadScore += ScoreSceneInit;
        StartUI.OnPlayAgain += PlayAgain;
        gameEventData.OnInteract.RegisterListener(NotepadCollected);
    }
    private void OnDisable()
    {
        Scenes.OnLoadStart -= StartSceneInit;
        Scenes.OnLoadScore -= ScoreSceneInit;

        StartUI.OnPlayAgain -= PlayAgain;

        gameEventData.OnInteract.UnregisterListener(NotepadCollected);
    }
    public void Update()
    {
        switch(sceneData.activeSceneType)
        {
            case SceneType.Start:
            {
                if (notepadData.collected) return;
                CheckSpyEnterExit();

            }
            break;
            case SceneType.Score:
            {
                if (!notepadData.collected) return;
                CheckSpyEnterExit();

            }
            break;
        }
    }
    private void StartSceneInit()
    {
        notepadData.collected = false;
    }
    private void ScoreSceneInit()
    {
        atlasRenderer.enabled = false;
        shinyRenderer.enabled = false;
    }
    private void CheckSpyEnterExit()
    {
        Bounds rendBounds = atlasRenderer.bounds;
        if (!spyAtProp && spyStats.bounds.max.x > rendBounds.min.x && spyStats.bounds.min.x < rendBounds.max.x)
        {
            OnSpyEnter?.Invoke(new Vector2(rendBounds.center.x, spyStats.bounds.max.y));
            spyAtProp = true;
        }
        else if (spyAtProp && (spyStats.bounds.max.x < rendBounds.min.x || spyStats.bounds.min.x > rendBounds.max.x))
        {
            OnSpyExit?.Invoke();
            spyAtProp = false;
        }
    }
    public void NotepadCollected()
    {
        switch(sceneData.activeSceneType)
        {
            case SceneType.Start:
            {
                if (spyAtProp && !notepadData.collected)
                {
                    OnAgreementCollect?.Invoke();
                    spyStats.playerInputsEnabled = false;
                    atlasRenderer.enabled = false;
                    shinyRenderer.enabled = false;
                }
            }
            break;
            case SceneType.Score:
            {
                if (spyAtProp && notepadData.collected)
                {
                    notepadData.collected = false;
                    OnNotepadReturn?.Invoke();

                    atlasRenderer.enabled = true;
                }
            }
            break;
        }
    }
    public void PlayAgain()
    {
        notepadData.collected = false;
        atlasRenderer.enabled = true;
        shinyRenderer.enabled = true;
    }
}
