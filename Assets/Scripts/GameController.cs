using TMPro;
using UnityEngine;

public enum GameDifficulty
{
    Easy = 0,
    Medium = 1,
    Hard = 2
}

public class GameController : MonoBehaviour
{
    public BubbleWrap bubbleWrap;
    public GameObject mainMenuSystem;

    public AudioSource audioSource;
    public AudioClip winNoise;

    // TODO: Would be cleaner to split pause-win-lose screen items into their own class.

    public GameObject pauseWinLoseScreen;
    public TMP_Text pauseWinLoseScreenText;
    public GameObject pauseWinLoseScreenContinueButton;

    public string winMessage = "Congrats, you win!";
    public string loseMessage = "You blew up! Try again?";
    public string pauseMessage = "Game paused.";

    public (int, float)[] difficultyConfigs =
    {
        (5, 0.15f),
        (7, 0.15f),
        (9, 0.15f),
    };

    private GameDifficulty lastSetDifficulty = GameDifficulty.Medium;

    private void Awake()
    {
        ReturnToMenu();
    }

    public void StartGame(GameDifficulty difficulty)
    {
        lastSetDifficulty = difficulty;

        pauseWinLoseScreen.SetActive(false);
        mainMenuSystem.SetActive(false);

        (int hCount, float bombRatio) = difficultyConfigs[(int)difficulty];
        bubbleWrap.Initialize(hCount, bombRatio, true);
    }

    public void RestartGame()
    {
        StartGame(lastSetDifficulty);
    }

    public void NotifyLost()
    {
        pauseWinLoseScreenContinueButton.SetActive(false);
        pauseWinLoseScreenText.SetText(loseMessage);
        pauseWinLoseScreen.SetActive(true);
    }

    public void NotifyWin()
    {
        pauseWinLoseScreenContinueButton.SetActive(false);
        pauseWinLoseScreenText.SetText(winMessage);
        pauseWinLoseScreen.SetActive(true);

        audioSource.clip = winNoise;
        audioSource.Play();
    }

    public void NotifyPause(bool isPaused)
    {
        if (!mainMenuSystem.activeSelf)
        {
            if (isPaused)
            {
                pauseWinLoseScreenContinueButton.SetActive(true);
                pauseWinLoseScreenText.SetText(pauseMessage);
                pauseWinLoseScreen.SetActive(true);
            }
            else
            {
                pauseWinLoseScreen.SetActive(false);
            }
        }
    }

    public void ReturnToMenu()
    {
        mainMenuSystem.SetActive(true);
        pauseWinLoseScreen.SetActive(false);
        bubbleWrap.Initialize(5, 0, false);
    }

    #region BUTTON_EVENT_CALLBACKS

    public void OnClickEasyGame()
    {
        StartGame(GameDifficulty.Easy);
    }

    public void OnClickMediumGame()
    {
        StartGame(GameDifficulty.Medium);
    }

    public void OnClickHardGame()
    {
        StartGame(GameDifficulty.Hard);
    }

    public void OnClickRestartGame()
    {
        RestartGame();
    }

    public void OnClickReturnToMenu()
    {
        ReturnToMenu();
    }

    public void OnClickContinue()
    {
        bubbleWrap.SetPaused(false);
    }

    #endregion
}
