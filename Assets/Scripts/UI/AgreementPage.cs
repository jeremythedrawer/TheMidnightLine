using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

using static AtlasUI;
public class AgreementPage : MonoBehaviour
{
    public static event Action OnAgreementSigned;

    public PlayerInputsSO playerInputs;
    public CameraStatsSO camStats;
    public SceneData sceneData;
    public NotepadData notepadData;
    public SpyStatsSO spyStats;

    public LeftHand leftHand;

    public Page page;

    [Header("Generated")]
    public Vector3 activePos;
    public Vector3 offscreenPos;
    public Vector3 curPos;
    public bool atActivePos;

    public CancellationTokenSource ctsMove;

    private void Start()
    {
        Init();
    }
    private void OnEnable()
    {
        Scenes.OnLoadScore += DisableSelf;
        AgreementProp.OnAgreementCollect += MoveToActivePosition;
        
        LeftHand.OnAtStartWritePos += WriteSignature;
        LeftHand.OnFinishWriting += FinishWritingSignature;
    }
    private void OnDisable()
    {
        Scenes.OnLoadScore -= DisableSelf;
        
        AgreementProp.OnAgreementCollect -= MoveToActivePosition;

        LeftHand.OnAtStartWritePos -= WriteSignature;
        LeftHand.OnFinishWriting -= FinishWritingSignature;
    }
    private void Update()
    {
        if (atActivePos)
        {
            if (playerInputs.spacebarDown && page.activePlayerWriteText == "")
            {
                leftHand.SetState(LeftHand.State.Writing);
            }
        }
    }
    public void Init()
    {
        activePos = transform.localPosition;
        offscreenPos.x = activePos.x;

        float uvPivotY = page.paperRenderer.sprite.uvPivot.y;
        float paperWorldSizeY = page.paperRenderer.bounds.size.y;
        offscreenPos.y = -camStats.camBounds.extents.y - paperWorldSizeY;
        offscreenPos.z = activePos.z;

        transform.localPosition = offscreenPos;

        page.InitAgreementPage();
        leftHand.Init();
        leftHand.SetState(LeftHand.State.OffScreen);
    }
    public void DisableSelf()
    {
        gameObject.SetActive(false);
    }
    public void MoveToActivePosition()
    {
        ctsMove?.Cancel();
        ctsMove = new CancellationTokenSource();
        MovingToActivePosition().Forget();
    }
    public void MoveToInactivePosition()
    {
        ctsMove?.Cancel();
        ctsMove = new CancellationTokenSource();
        MovingToInactivePosition().Forget();

    }
    private void WriteSignature()
    {
        page.WritePlayerWriteText();
    }
    private void FinishWritingSignature()
    {
        leftHand.SetState(LeftHand.State.Stationary);
        leftHand.MoveToLeftOfPaper();
        OnAgreementSigned?.Invoke();
        MoveToInactivePosition();
        spyStats.playerInputsEnabled = true;
        notepadData.signedAgreement = true;
    }
    public async UniTask MovingToActivePosition()
    {    
        curPos = transform.localPosition;

        while ((activePos.y - curPos.y) > 0.05f)
        {
            curPos.y = Mathf.Lerp(curPos.y, activePos.y, Time.deltaTime * MOVE_DAMP);
            transform.localPosition = curPos;
            await UniTask.Yield();
        }
        atActivePos = true;
        page.SetPreviewPlayerWriteTexts(NotepadState.None);
        leftHand.SetState(LeftHand.State.Stationary);
        leftHand.MoveToLeftOfPaper();
    }
    public async UniTask MovingToInactivePosition()
    {
        curPos = transform.localPosition;

        while ((curPos.y - offscreenPos.y) > 0.05f)
        {
            curPos.y = Mathf.Lerp(curPos.y, offscreenPos.y, Time.deltaTime * MOVE_DAMP);
            transform.localPosition = curPos;
            await UniTask.Yield();
        }
        gameObject.SetActive(false);
        notepadData.collected = true;
        atActivePos = false;

    }
}
