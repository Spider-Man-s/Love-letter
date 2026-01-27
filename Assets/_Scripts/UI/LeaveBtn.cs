using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using LoveLetter.Networking;

public class LeaveBtn : MonoBehaviour
{
    public Button button;
    public TMPro.TMP_Text buttonText;

    private enum LeaveState
    {
        Red,
        Orange,
        Green
    }

    private LeaveState state = LeaveState.Red;
    private Coroutine timerRoutine;

    private void Awake()
    {
        ResetToRed();
        button.onClick.AddListener(OnLeaveButtonClicked);
    }

    public void OnLeaveButtonClicked()
    {
        switch (state)
        {
            case LeaveState.Red:
                EnterOrange();
                break;

            case LeaveState.Orange:
                EnterGreen();
                break;

            case LeaveState.Green:
                ConfirmLeave();
                break;
        }
    }

    private void EnterOrange()
    {
        state = LeaveState.Orange;
        buttonText.text = "You sure?";
        button.image.color = new Color(1f, 0.55f, 0f);
        RestartTimer();
    }

    private void EnterGreen()
    {
        state = LeaveState.Green;
        buttonText.text = "Really sure?";
        button.image.color = Color.green;
        RestartTimer();
    }

    private void ConfirmLeave()
    {
        BasicSpawner.Instance.LeaveRoom();
    }

    private void ResetToRed()
    {
        state = LeaveState.Red;
        buttonText.text = "Leave";
        button.image.color = Color.red;
    }

    private void RestartTimer()
    {
        if (timerRoutine != null)
            StopCoroutine(timerRoutine);
        timerRoutine = StartCoroutine(ResetTimerCoroutine());
    }

    private IEnumerator ResetTimerCoroutine()
    {
        yield return new WaitForSeconds(4f);
        ResetToRed();
        timerRoutine = null;
    }
}
