using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using LoveLetter.Networking;

public class LeaveBtn : MonoBehaviour
{
    [Header("Main Leave Button")]
    public Button button;
    public TMPro.TMP_Text buttonText;

    [Header("Confirm Buttons")]
    public Button confirmMainMenuButton;
    public Button confirmSessionButton;

    private enum LeaveState
    {
        Red,
        Orange,
        Green,
        ConfirmButtons
    }

    private LeaveState state = LeaveState.Red;
    private Coroutine timerRoutine;

    private void Awake()
    {
        ResetToRed();

        button.onClick.AddListener(OnLeaveButtonClicked);

        confirmMainMenuButton.gameObject.SetActive(false);
        confirmSessionButton.gameObject.SetActive(false);

        confirmMainMenuButton.onClick.AddListener(() =>
        {
            BasicSpawner.Instance.ReturnToMainMenu();
        });

        confirmSessionButton.onClick.AddListener(() =>
        {
            BasicSpawner.Instance.ReturnToSessionList();
        });
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
                ShowConfirmButtons();
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

    private void ShowConfirmButtons()
    {
        state = LeaveState.ConfirmButtons;

        button.gameObject.SetActive(false);

        confirmMainMenuButton.gameObject.SetActive(true);
        confirmSessionButton.gameObject.SetActive(true);

        RestartTimer();
    }

    private void HideConfirmButtons()
    {
        confirmMainMenuButton.gameObject.SetActive(false);
        confirmSessionButton.gameObject.SetActive(false);
        button.gameObject.SetActive(true);

        ResetToRed();
    }

    private void ResetToRed()
    {
        state = LeaveState.Red;

        buttonText.text = "Leave";
        button.image.color = Color.red;

        confirmMainMenuButton.gameObject.SetActive(false);
        confirmSessionButton.gameObject.SetActive(false);

        button.gameObject.SetActive(true);
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

        if (state == LeaveState.ConfirmButtons)
            HideConfirmButtons();
        else
            ResetToRed();

        timerRoutine = null;
    }
}
