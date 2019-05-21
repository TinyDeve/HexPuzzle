using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour {

    public delegate void TouchEventHandler(Vector2 values);
    public delegate void SwipeEventHandler(Vector2 values,Vector2 valuess);

    public static event SwipeEventHandler SwipeEvent;
    public static event TouchEventHandler TapEvent;

    public GameManager gm;

    Vector2 m_touchMovement;
    Vector2 m_touchPosition;

    [Range(0, 250)]
    public int m_minSwipeDistance = 50;

    public bool m_useDiagnostic = false;
    
    void OnSwipeEnd()
    {
        if (SwipeEvent != null)
        {
            SwipeEvent(m_touchPosition,m_touchMovement+m_touchPosition);
        }
    }
    void OnTap()
    {
        if(TapEvent != null)
        {
            TapEvent(m_touchPosition);
        }
    }
    
    void Update()
    {
        if(gm.gameMode != GameManager.GameMode.Play)
        {
            return;
        }
        if (Input.touchCount > 0)
        {
            Touch touch = Input.touches[0];
            if (touch.phase == TouchPhase.Began)
            {
                m_touchPosition = touch.position;
                m_touchMovement = Vector2.zero;
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                m_touchMovement += touch.deltaPosition;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                if (m_touchMovement.magnitude > m_minSwipeDistance)
                {
                    OnSwipeEnd();
                    Diagnostic("Swipe detected", m_touchMovement.ToString() + " " + SwipeDiagnostic(m_touchMovement));
                }
                else
                {
                    OnTap();
                    Diagnostic("Tap detected", m_touchPosition.ToString());

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
