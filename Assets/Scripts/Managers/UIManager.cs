using UnityEngine;

public class UIManager : MonoBehaviour
{

    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject startUI;
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject finishUI;



    void Start()
    {
        startUI.SetActive(true);
    }

    public void StartGame()
    {
        gameManager.gameMode = GameManager.GameMode.Play;
        startUI.SetActive(false);
        gameUI.SetActive(true);
    }


    public void EndGame()
    {
        gameUI.SetActive(false);
        finishUI.SetActive(true);
    }



    public void RestartGame()
    {
        finishUI.SetActive(false);
        gameUI.SetActive(true);
        gameManager.Restart();
    }
}
