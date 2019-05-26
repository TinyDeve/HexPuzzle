using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class Hex : MonoBehaviour
{
    public Board board;

    [Header("Properties")]
    public int hexType;
    public int xIndex;
    public int yIndex;

    [Header("Components")]
    [SerializeField]private SpriteRenderer spriteRenderer;
    [SerializeField] private MeshRenderer boomMesh;
    [SerializeField] private TextMesh boomText;
    [SerializeField] private Sprite boomSprite;
    [SerializeField] private Sprite hexSprite;
    [SerializeField] private Transform hexTransform;

    [Header("Status")]
    public bool isPooled = false;
    public bool isBoom = false;
    public GameObject hexPrefab;

    public void SetBoomText(int i)
    {
        boomText.text = i + "";
    }

    public void ChangeToBoom(bool boom)
    {
        isBoom = boom;
        spriteRenderer.sprite = boom ? boomSprite : hexSprite;
        boomMesh.enabled = boom;
    }

    public void ChangeType(int type)
    {
        hexType = type;
        spriteRenderer.color = board.colors[type];
    }

    public void ChangePool(bool isPool)
    {
        spriteRenderer.enabled = !isPool;
        isPooled = isPool;

        if(isBoom && isPool)
        {
            ChangeToBoom(false);
            board.RemoveBoomKey(this);
        }
    }

    public void Start()
    {
        board = Board.instance;

        board.SetHex(this);

        hexType = Random.Range(0, board.hexTypeNumber);
        ChangeType(hexType);
    }

    public void PlaceAt(int x,int y)
    {
        board.hexs[x, y] = this;
        xIndex = x;
        yIndex = y;

        float yPosition = y;
        if (x % 2 == 0)
        {
            yPosition = y + 0.5f;
        }

        hexTransform.position = new Vector3(x, yPosition, 0);
    }

    public void MoveTo(int x, int y)
    {
        //Position half block up if x is even 
        float yPosition = y;
        if (x % 2 == 0)
        {
            yPosition = y + 0.5f;
        }

        StartCoroutine(MoveObjectTo(x,yPosition, y));
    }
    public void MoveTo(Vector2 vector2)
    {
        //Position half block up if x is even
        MoveTo((int)vector2.x, (int)vector2.y);
    }

    IEnumerator MoveObjectTo(int x,float yPosition ,int y)
    {
        float time = board.gameSpeed;
        Vector3 startPos = transform.position;


        Vector3 endPos = new Vector3(x, yPosition, 0);

        bool moveEnd = false;

        float elapsedTime = 0.0f;

        while (!moveEnd)
        {
            if ((endPos - transform.position).magnitude < 0.01f)
            {
                //Debug.Log(x+" , "+y+"MoveFinished");
                moveEnd = true;
                transform.position = endPos;
                board.hexs[x, y] = this;
                xIndex = x;
                yIndex = y;
            }

            elapsedTime += Time.deltaTime;

            float lerpValue = elapsedTime / time;

            transform.position = Vector3.Lerp(startPos, endPos, lerpValue);

            yield return null;
        }
    }

    public void Fall(int y)
    {
        board.hexs[xIndex, yIndex] = null;
        board.hexs[xIndex, y] = this;
        //Debug.Log("Fall To :" + y);
        StartCoroutine(FallTo(y));
    }

    IEnumerator FallTo(int y)
    {
        int distance = yIndex - y;
        float time = board.gameSpeed * distance;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + new Vector3(0, -distance, 0);

        bool moveEnd = false;

        float elapsedTime = 0.0f;

        while (!moveEnd)
        {
            if ((endPos - transform.position).magnitude < 0.01f)
            {
                moveEnd = true;
                //Debug.Log("Fall To :" + y + " End");
                yIndex = y;
                transform.position = endPos;//new Vector3(xIndex,y,0);
                continue;
            }

            elapsedTime += Time.deltaTime;

            float lerpValue = elapsedTime / time;

            transform.position = Vector3.Lerp(startPos, endPos, lerpValue);

            yield return null;
        }
    }

    public void Cascade(int y, int wait)
    {
        //Position half block up if x is even 
        float yPosition = y;
        if (xIndex % 2 == 0)
        {
            yPosition = y + 0.5f;
        }

        board.hexs[xIndex, y] = this;
        //Debug.Log("Fall To :" + y);
        StartCoroutine(CascadeRoutine(y,yPosition, wait));
    }

    IEnumerator CascadeRoutine(int y,float yPosition, int wait)
    {
        float distance = board.boardHeigth - yPosition;
        float time = board.gameSpeed * distance;
        transform.position = new Vector3(xIndex, board.boardHeigth, 0);
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + new Vector3(0, -distance, 0);

        bool moveEnd = false;

        float elapsedTime = 0.0f;

        yield return new WaitForSeconds(wait * board.gameSpeed);

        while (!moveEnd)
        {
            if ((endPos - transform.position).magnitude < 0.01f)
            {
                moveEnd = true;
                //Debug.Log("Fall To :" + y + " End");
                yIndex = y;
                transform.position = endPos;//new Vector3(xIndex,y,0);
                continue;
            }

            elapsedTime += Time.deltaTime;

            float lerpValue = elapsedTime / time;

            transform.position = Vector3.Lerp(startPos, endPos, lerpValue);

            yield return null;
        }
    }

}
