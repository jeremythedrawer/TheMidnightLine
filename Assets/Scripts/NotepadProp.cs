using System;
using UnityEngine;

public class NotepadProp : MonoBehaviour
{
    public static event Action<Vector2> OnSpyEnter;
    public static event Action OnSpyExit;

    public AtlasRenderer atlasRenderer;
    public MeshRenderer shinyRenderer;

    public GameEventDataSO gameEventData;
    public NotepadData notepadData;
    public SpyStatsSO spyStats;


    [Header("Generated")]
    public bool spyAtProp;

    private void OnEnable()
    {
        notepadData.collected = false;

        gameEventData.OnInteract.RegisterListener(NotepadCollected);
    }
    private void OnDisable()
    {
        gameEventData.OnInteract.UnregisterListener(NotepadCollected);
    }

    public void Update()
    {
        if (notepadData.collected) return;

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
        if (spyAtProp && !notepadData.collected)
        {
            notepadData.collected = true;
            gameEventData.OnNotepadCollect?.Raise();

            atlasRenderer.enabled = false;
            shinyRenderer.enabled = false;
        }
    }
}
