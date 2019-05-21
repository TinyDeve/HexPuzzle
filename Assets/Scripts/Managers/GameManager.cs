using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public enum GameMode
    {
        Play,  //Game wait for input from user
        Moving,//When hexs are moving
        NoGame,//When game at uı
    }

    public GameMode gameMode;

    [Header("References")]
    [SerializeField] private Board board;
    [SerializeField] private UIManager uIManager;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject sellectHolder;
    [SerializeField] private Text scoreText;

    [Space]

    [Header("Properties")]
    [Range(0,180)]
    [SerializeField] private float m_minTurnAngle;
    [Range(0, 50)]
    [SerializeField] private float xMarginPercentage;
    [Range(0, 50)]
    [SerializeField] private float yMarginPercentage;
    [Range(.1f, .4f)]//Amount of margin for better touch detection
    [SerializeField] private float tileMarginPercentage;

    [SerializeField] private int boomAtScore = 1000;


    //Game variables
    private int score;
    private int xTurnValue = 0;
    private int yTurnValue = 0;
    private int boomNumber = 0;

    private Vector2 sellectTouchPosition;

    private float m_boardWitdh;
    private float m_boardHeigth;

    //Scren variables
    private float m_orthographicSize;
    private float eachTilePixel; //Amount of pixel each hex tile is acupied
    private float screenHeigth;
    private float screenWitdh;
    private float tilePixel;
    private float selectPixel;
    private Vector2 orjinPoint;



    private void Start()
    {
        gameMode = GameMode.NoGame;
        m_boardWitdh = board.boardWitdh;
        m_boardHeigth = board.boardHeigth;

        screenHeigth = Screen.height;
        screenWitdh = Screen.width;
        
        scoreText.text = score.ToString();

        //Camera positon and orthographic size are adjusted
        //To fit board in any type of device
        AdjustCameraAndTiles();
    }

    public void Restart()
    {
        gameMode = GameMode.Play;
        sellectHolder.SetActive(false);
        score = 0;
        boomNumber = 0;
        scoreText.text = score.ToString();
        board.RefillBoard();
    }

    public void ChangeScore(int scoreIncrease)
    {
        score += scoreIncrease*5;
        if(score/boomAtScore > boomNumber)
        {
            board.AddBoom();
            boomNumber += 1;
        }
        scoreText.text = score.ToString();
    }

    public void NoMatched()
    {
        sellectHolder.SetActive(true);
    }

    public void ShowSellect()
    {
        sellectHolder.SetActive(true);
    }
    
    public void GameLossed()
    {
        gameMode = GameMode.NoGame;
        uIManager.EndGame();
        //Check at board send mesage to here
    }


    private void AdjustCameraAndTiles()
    {
        mainCamera.transform.position = new Vector3((m_boardWitdh - 1) / 2, (m_boardHeigth - 1) / 2, -10);
        
        float screenRatio = screenHeigth / screenWitdh;

        float xOrthographicSize = ((m_boardWitdh) / 2) * (50 / (50 - xMarginPercentage));

        xOrthographicSize = xOrthographicSize * screenRatio;

        float yOrthographicSize = ((m_boardHeigth) / 2) * (50 / (50 - yMarginPercentage));

        if (yOrthographicSize > xOrthographicSize)
        {
            m_orthographicSize = yOrthographicSize;

            tilePixel = (screenHeigth * (1 - (yMarginPercentage / 50))) / m_boardHeigth;
        }
        else
        {
            m_orthographicSize = xOrthographicSize;

            tilePixel = (screenWitdh * (1 - (xMarginPercentage / 50))) / m_boardWitdh;
        }


        orjinPoint.x = (screenWitdh - tilePixel * m_boardWitdh) / 2  + tilePixel/2;
        orjinPoint.y = (screenHeigth - tilePixel * m_boardHeigth) / 2 ;

        Debug.Log("TilePixel : " + tilePixel + "OrjinPoint :" + orjinPoint.ToString());
        //m_orthographicSize = (yOrthographicSize > xOrthographicSize) ? yOrthographicSize : xOrthographicSize;

        mainCamera.orthographicSize = m_orthographicSize;
    }
    
    #region Input Handler
    private void OnEnable()
    {
        InputManager.SwipeEvent += SwipeHandler;
        InputManager.TapEvent += TapHandler;
    }

    private void OnDisable()
    {
        InputManager.SwipeEvent -= SwipeHandler;
        InputManager.TapEvent -= TapHandler;
    }

    private void SwipeHandler(Vector2 swipeStartPos, Vector2 swipeEndPos)
    {
        if(sellectTouchPosition == null)
        {
            return;
        }


        Vector2 firstTap = swipeStartPos - sellectTouchPosition;
        Vector2 endTap = swipeEndPos - sellectTouchPosition;

        float angle = Vector2.SignedAngle(firstTap, endTap);

        //Debug.Log(angle.ToString());

        if (Mathf.Abs(angle) > m_minTurnAngle)
        {
            gameMode = GameMode.Moving;
            board.SellectHexsStartTurn(xTurnValue, yTurnValue, angle < 0);
            sellectHolder.SetActive(false);
            Debug.Log(angle < 0 ? "Return clockwise" : "Return counterClockwise");
        }
    }

    private void TapHandler(Vector2 touchPosition)
    {
        Vector2 tileTouchPixel = touchPosition - orjinPoint;

        float xPixelMargin = (tileTouchPixel.x % tilePixel);
        float yPixelMargin = (tileTouchPixel.y % (tilePixel/2));

        if (tileTouchPixel.x > 0 && tileTouchPixel.y > 0)
        {

            int xTileIndex = (int)(tileTouchPixel.x / tilePixel);
            int yTileIndex = (int)(tileTouchPixel.y / (tilePixel / 2));

            Debug.Log(xTileIndex + " , " + yTileIndex);

            if (xTileIndex >= (m_boardWitdh-1) || yTileIndex >= (m_boardHeigth) * 2 -1)
            {
                return;
            }
            //Debug.Log(xTileIndex + " , " + yTileIndex);



            if(yTileIndex == 0)
            {
                return;
            }



            sellectHolder.transform.position = new Vector3(xTileIndex + 0.5f, yTileIndex * 0.5f, 1);
            int direction = xTileIndex % 2 == 0 ? 1 : -1;
            direction *= yTileIndex % 2 == 0 ? -1 : 1;
            sellectHolder.transform.localScale = new Vector3(direction, 1, 1);
            sellectTouchPosition = touchPosition;
            xTurnValue = xTileIndex;
            yTurnValue = yTileIndex;
            sellectHolder.SetActive(true);
        }
    }

    #endregion
}
