using UnityEngine;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    public TextMeshProUGUI ScoreText;
    public TextMeshProUGUI HighScoreText;
    public GameObject HighScoreAlert;

    void OnEnable()
    {
        int score = PlayerPrefs.GetInt("score", 0);
        int highScore = PlayerPrefs.GetInt("highscore", 0);

        if (ScoreText != null)
            ScoreText.text = score.ToString();

        bool isNewRecord = score > highScore;

        if (isNewRecord)
        {
            highScore = score;
            PlayerPrefs.SetInt("highscore", highScore);
            PlayerPrefs.Save();
        }

        if (HighScoreText != null)
            HighScoreText.text = highScore.ToString();

        if (HighScoreAlert != null)
            HighScoreAlert.SetActive(isNewRecord);
    }
}