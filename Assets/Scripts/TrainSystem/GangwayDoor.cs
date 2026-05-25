using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEditor;
using UnityEngine;
public class GangwayDoor : MonoBehaviour
{
    const float DOOR_MOVE_TIME = 0.5f;
    const float DOOR_MOVE_AMOUNT = 0.1f;

    public Carriage carriage;
    public Gangway gangway;
    public Transform rightDoor;
    public Transform leftDoor;
    public BoxCollider2D wallCollider;
    public LayerSettingsSO layerSettings;
    public bool isLeftOfCarriage;
    [Header("Generated")]
    public float curX;
    public Vector3 rightPos;
    public Vector3 leftPos;
    public CancellationTokenSource ctsMove;
    public bool isOpen;

    private void Start()
    {
        rightPos = rightDoor.localPosition;
        leftPos = leftDoor.localPosition;
    }
    public void OpenDoors()
    {
        Debug.Log("Open Doors");
        ctsMove?.Cancel();
        ctsMove?.Dispose();

        ctsMove = new CancellationTokenSource();
        isOpen = true;

        Opening().Forget();
        wallCollider.enabled = false;
    }
    public void CloseDoors()
    {
        ctsMove?.Cancel();
        ctsMove?.Dispose();

        ctsMove = new CancellationTokenSource();

        Closing().Forget();
        wallCollider.enabled = true;
    }

    private async UniTask Closing()
    {
        float elaspedTime = (curX / DOOR_MOVE_AMOUNT) * DOOR_MOVE_TIME;
        try
        {
            while (elaspedTime > 0)
            {
                elaspedTime -= Time.deltaTime;

                curX = (elaspedTime / DOOR_MOVE_TIME) * DOOR_MOVE_AMOUNT;
                rightPos.x = curX;
                leftPos.x = -curX;
                rightDoor.localPosition = rightPos;
                leftDoor.localPosition = leftPos;
                await UniTask.Yield(PlayerLoopTiming.Update, ctsMove.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
    private async UniTask Opening()
    {
        float elaspedTime = (curX / DOOR_MOVE_AMOUNT) * DOOR_MOVE_TIME;
        try
        {
            while (elaspedTime < DOOR_MOVE_TIME)
            {
                elaspedTime += Time.deltaTime;

                curX = (elaspedTime / DOOR_MOVE_TIME) * DOOR_MOVE_AMOUNT;
                rightPos.x = curX;
                leftPos.x = -curX;
                rightDoor.localPosition = rightPos;
                leftDoor.localPosition = leftPos;
                await UniTask.Yield(cancellationToken: ctsMove.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

}
