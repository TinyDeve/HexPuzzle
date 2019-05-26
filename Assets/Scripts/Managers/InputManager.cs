using UnityEngine;

public class InputManager : MonoBehaviour {

    public delegate void TouchEventHandler(Vector2 values);
    public delegate void SwipeEventHandler(Vector2 values,Vector2 valuess);

    public static event SwipeEventHandler SwipeEvent;
    public static event TouchEventHandler TapEvent;

    [HideInInspector]
    public bool noInput;

    [Range(0, 250)]
    [SerializeField] private int m_minSwipeDistance = 50;


    private bool m_useDiagnostic = false;

    private Vector2 m_touchMovement;
    private Vector2 m_touchStartPosition;
    private Vector2 m_touchEndPosition;

    private Touch touch;



    void OnSwipeEnd()
    {
        SwipeEvent?.Invoke(m_touchStartPosition, m_touchEndPosition);
    }
    void OnTap()
    {
        TapEvent?.Invoke(m_touchStartPosition);
    }

    private void Start()
    {
        m_minSwipeDistance *= m_minSwipeDistance;
        noInput = true;
    }

    private void Update()
    {
        if(noInput)//Changed to not get data from manager but assigned by manager when game mode changed
        {
            return;
        }
        if (Input.touchCount > 0)
        {
            touch = Input.GetTouch(0);//Changed because Input.touches create an array every frame then array become garbage
            if (touch.phase == TouchPhase.Began)
            {
                m_touchStartPosition = touch.position;
                m_touchMovement = Vector2.zero;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                m_touchEndPosition = touch.position;
                m_touchMovement = m_touchEndPosition-m_touchStartPosition;
                if (m_touchMovement.sqrMagnitude > m_minSwipeDistance)//sqrMagnitude is les expensive function
                {
                    m_touchEndPosition = touch.position;
                    OnSwipeEnd();
                }
                else
                {
                    OnTap();

                }
            }
        }
    }

    void Diagnostic(string text1, string text2)
    {
        if (m_useDiagnostic)
        {
            Debug.Log(text1 + " " + text2);
        }
    }

    string SwipeDiagnostic(Vector2 swipeMovement)
    {
        string direction = "";

        // horizontal
        if (Mathf.Abs(swipeMovement.x) > Mathf.Abs(swipeMovement.y))
        {
            direction = (swipeMovement.x >= 0) ? "right" : "left";

        }
        // vertical
        else
        {
            direction = (swipeMovement.y >= 0) ? "up" : "down";

        }

        return direction;
    }
}
