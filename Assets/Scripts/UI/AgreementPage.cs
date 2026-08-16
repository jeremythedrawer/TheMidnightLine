using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

using static AtlasUI;
public class AgreementPage : MonoBehaviour
{
    public static Vector3 ACTIVE_POS = new Vector3(-1.70000005f, 0.800000012f, 0);
    
    public static event Action OnAgreementSigned;

    public PlayerInputsSO playerInputs;
    public CameraStatsSO camStats;
    public SceneData sceneData;
    public NotepadData notepadData;
    public SpyStatsSO spyStats;

    public LeftHand leftHand;

    public Page page;

    public IconUIElement spacebarButton;

    [Header("Generated")]
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
            if (page.activePlayerWriteText == "")
            {
                spacebarButton.UpdateButton(playerInputs);

                if (playerInputs.writeKeyDown)
                {
                    leftHand.SetState(LeftHand.State.Writing);
                }
            }
        }
    }
    public void Init()
    {
        offscreenPos.x = ACTIVE_POS.x;

        float uvPivotY = page.paperRenderer.sprite.uvPivot.y;
        float paperWorldSizeY = page.paperRenderer.bounds.size.y;
        offscreenPos.y = -camStats.camBounds.extents.y - paperWorldSizeY;
        offscreenPos.z = ACTIVE_POS.z;

        transform.localPosition = offscreenPos;

        page.InitAgreementPage();
        leftHand.Init();
        leftHand.SetState(LeftHand.State.OffScreen);

        spacebarButton.InitButton(ClickSpaceBarButton, EnterSpaceBarButton, ExitSpaceBarButton);
    }
    public void EnterSpaceBarButton(IconUIElement icon)
    {
        if (page.activePlayerWriteText == "")
        {
            icon.renderer.custom.x = 1;
        }
    }
    public void ExitSpaceBarButton(IconUIElement icon)
    {
        icon.renderer.custom.x = 0;
    }
    public void ClickSpaceBarButton(IconUIElement icon)
    {
        leftHand.SetState(LeftHand.State.Writing);
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
        MoveToInactivePosition();
        spyStats.playerInputsEnabled = true;
        notepadData.signedAgreement = true;
        
        OnAgreementSigned?.Invoke();
    }
    public async UniTask MovingToActivePosition()
    {    
        curPos = transform.localPosition;

        while ((ACTIVE_POS.y - curPos.y) > 0.05f)
        {
            curPos.y = Mathf.Lerp(curPos.y, ACTIVE_POS.y, Time.deltaTime * MOVE_DAMP);
            transform.localPosition = curPos;
            await UniTask.Yield();
        }
        atActivePos = true;
        page.SetPreviewPlayerWriteTexts(NotepadState.None);
        leftHand.SetState(LeftHand.State.Stationary);
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
        notepadData.collected = true;
        gameObject.SetActive(false);
        atActivePos = false;
    }
}
