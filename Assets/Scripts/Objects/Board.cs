using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class Board : MonoBehaviour
{
    public static Board instance;

    public void Awake()
    {
        if (instance == null)
            instance = this;
    }


    [Header("References")]
    [SerializeField] private GameObject hexPrefab;
    [SerializeField] private GameManager gm;

    [HideInInspector]
    public Hex[,] hexs;
    [HideInInspector]
    public int fillX = 0;
    [HideInInspector]
    public int fillY = 0;

    [Header("Properties")]
    public float gameSpeed;
    public int boardHeigth = 9;
    public int boardWitdh = 8;
    public int hexTypeNumber = 3;
    public Color[] colors;

    //Check the board where you left
    private int lastCheckX = 0;
    private int lastCheckY = 0;

    private Dictionary<Hex, int> booms = new Dictionary<Hex, int>();
    private List<Hex> disappearedHexs = new List<Hex>();
    private Hex[] sellectHexs = new Hex[3];
    private Vector2[] firstPosition = new Vector2[5];
    private int smallestMatchNumber = 3;
    private int turnTime = 0;
    private int comboTimes = 0;
    private int maxFallDistance;
    private bool turnDirectionClockWise = true;
    private bool isComplete = false;
    private bool placeBoom = false;

    private GameObject hexHolder;
    private Hex hexObjeHolder;

    private static List<Hex> m_emptyHexList = new List<Hex>() { };
    private List<Hex> m_allMatches = new List<Hex>();
    private List<Hex> m_allMatchesAt = new List<Hex>();
    private int[] m_aroundIntArray = new int[6];
    private Hex[] m_aroundHexArray = new Hex[6];
    private List<int> disHexs = new List<int>();
    private List<Hex> falledHexs = new List<Hex>();
    private List<Hex> matches = new List<Hex>();
    private List<int> hexIndexes = new List<int>();
    private List<int> fallXIndex = new List<int>();
    private List<Hex> movedHexs = new List<Hex>();
    private Hex nextBoomHex;
    private Hex fallHex;
    private Hex moveHex;

    void Start()
    {
        if (hexTypeNumber > colors.Length)
        {
            //Debug.LogWarning("Not enough sprites added at Board script");
            return;
        }

        hexs = new Hex[boardWitdh, boardHeigth];
        FillBoard();

    }

    public void FillBoard()
    {
        for (int i = 0; i < boardWitdh; i++)
        {
            for (int j = 0; j < boardHeigth; j++)
            {
                hexHolder = Instantiate(hexPrefab, transform) as GameObject;

                hexHolder.name = "Hex : " + i + "," + j;
            }
        }
    }

    public void SetHex(Hex hex)
    {
        hexs[fillX, fillY] = hex;
        hex.PlaceAt(fillX, fillY);

        fillX += 1;
        if (fillX == boardWitdh)
        {
            fillY += 1;
            fillX = 0;

            if (fillY == boardHeigth)
            {
                RefillBoard();
            }
        }
    }



    public void RefillBoard()
    {
        RemoveAllBooms();

        for (int i = 0; i < boardWitdh; i++)
        {
            for (int j = 0; j < boardHeigth; j++)
            {
                hexObjeHolder = hexs[i, j];
                if (hexObjeHolder == null)
                {
                    continue;
                }

                RandomHexAt(hexObjeHolder);

                //..make it around check for optimization
                while (IsAnyMatchAtFill(i, j, hexObjeHolder.hexType))
                {
                    RandomHexAt(hexObjeHolder);
                }
            }
        }
    }

    private void RandomHexAt(Hex hex)
    {
        int hexType = Random.Range(0, hexTypeNumber);
        hex.ChangeType(hexType);
    }

    /*Function moved to hex obje to remove transform call and garbage creation due to hex variable
    private void PlaceHexAt(int x, int y, Hex hex)
    {
        hex.xIndex = x;
        hex.yIndex = y;


        hexs[x, y] = hex;

        //Position half block up if x is even 
        float yPosition = y;
        if(x%2 == 0)
        {
            yPosition = y + 0.5f;
        }

        hex.transform.position = new Vector3(x, yPosition, 0);

    }*/

    private void TurnFinished()
    {
        //Debug.Log("TurnFinished");
        //Check is any boom count is zero
        CheckBoomKill();

        //Check is any more move remain
        CheckMoveKill();

        gm.PlayMoveGameMode(true);
    }

    #region HandleBooms

    private void RemoveAllBooms()
    {
        foreach (Hex hex in booms.Keys)
        {
            hex.ChangeToBoom(false);
        }
        booms.Clear();
    }

    public void CheckBoomKill()
    {

        bool killGame = false;

        for (int i = 0; i < booms.Count; i++)
        {

            Hex hex = booms.Keys.ElementAt(i);
            int value = booms[hex];

            if (value == 1)
            {
                killGame = true;
            }

            hex.SetBoomText(value - 1);

            booms[hex] = value - 1;
        }

        if (killGame)
        {
            //Debug.Log("CheckBoomKill");
            gm.GameLossed();
            return;
        }
    }

    public void AddBoom()
    {
        placeBoom = true;
    }

    public void RemoveBoomKey(Hex hex)
    {
        if (booms.ContainsKey(hex))
        {
            booms.Remove(hex);
        }
    }

    #endregion

    #region CheckLoss

    private void CheckMoveKill()
    {

        if (CheckForMove())
        {
            gm.ShowSellect();
        }
        else
        {
            gm.GameLossed();
            return;
        }
    }

    private bool CheckForMove()
    {
        if (CheckMoveAtIndexs(lastCheckX, lastCheckY))
        {
            return true;
        }

        //Check forward from last index
        for (int i = 0; i < boardHeigth; i++)
        {
            for (int j = 0; j < boardWitdh; j++)
            {
                if (CheckMoveAtIndexs(i, j))
                {
                    lastCheckX = j;
                    lastCheckY = i;
                    return true;
                }
            }
        }
        /*Optimization
        //First check old tile
        //Then check from tile to end
        //At last from zero to tile
        for (int i = 0; i < lastCheckX; i++)
        {
            for (int j = 0; j < lastCheckY; j++)
            {
                if (CheckMoveAtIndexs(i, j))
                {
                    lastCheckX = i;
                    lastCheckY = j;
                    return true;
                }
            }
        }*/
        return false;
    }

    private bool CheckMoveAtIndexs(int x, int y)
    {
        GetAround(x, y,m_aroundHexArray);

        return CheckMoveFromAround(m_aroundHexArray);
    }

    public void AroundDebug(int x, int y)
    {
        GetAround(x, y, m_aroundHexArray);
        if (CheckMoveFromAround(m_aroundHexArray))
        {
            foreach (Hex hex in m_aroundHexArray)
            {
                if (hex == null) continue;
                hex.transform.localScale = new Vector3(0.5f, 0.5f);
            }
        }
    }

    private void GetAround(int x, int y, Hex[] hexArray)
    {

        hexArray[0] = GetHexSave(x , y + 1);
        hexArray[3] = GetHexSave(x , y - 1);

        if(x%2 == 0)
        {
            hexArray[2] = GetHexSave(x + 1, y);
            hexArray[4] = GetHexSave(x - 1, y);

            hexArray[1] = GetHexSave(x + 1, y + 1);
            hexArray[5] = GetHexSave(x - 1, y + 1);
        }
        else
        {
            hexArray[1] = GetHexSave(x + 1, y);
            hexArray[5] = GetHexSave(x - 1, y);


            hexArray[2] = GetHexSave(x + 1, y - 1);
            hexArray[4] = GetHexSave(x - 1, y - 1);
        }
    }

    private Hex GetHexSave(int x,int y)
    {
        if (IsInBoard(x, y))
        {
            return hexs[x, y];
        }
        return null;
    }

    private bool CheckMoveFromAround(Hex[] around)
    {
        GetArrayFromAround(around,m_aroundIntArray);
        return CheckMoveFromArray(m_aroundIntArray);
    }

    private void GetArrayFromAround(Hex[] around,int[] aroundInt)
    {
        for (int i = 0; i < 6; i++)
        {
            Hex hex = around[i];
            
            if(hex == null)
            {
                aroundInt[i] = -1;
                continue;
            }
            aroundInt[i] = hex.hexType;

        }
    }

    private bool CheckMoveFromArray(int[] aroundArray)
    {
        
        int lastHexType = aroundArray[5];


        for (int i = 0; i < 6; i++)
        {
            if (aroundArray[i] == -1)
            {
                lastHexType = -1;
                continue;
            }
            if (lastHexType == aroundArray[i])
            {
                int sameHexNumber = 0;
                foreach (int hexType in aroundArray)
                {
                    if (hexType == lastHexType)
                    {
                        sameHexNumber += 1;
                    }
                }
                if (sameHexNumber >= 3)
                {
                    return true;
                }

            }
            lastHexType = aroundArray[i];
        }
        return false;
    }

    #endregion

    #region Move Hexs
    public void SellectHexsStartTurn(int i,int y,bool direction)
    {
        sellectHexs = new Hex[3];
        firstPosition = new Vector2[3];

        turnDirectionClockWise = direction;
        
        //Sellect Them
        int baseYIndex;
        int xAdd = i % 2 == 0 ? 1 : -1;

        if(y % 2 == 0)
        {
            baseYIndex = y / 2;
            sellectHexs[2] = hexs[i+(int)(0.5f - xAdd * 0.5f), baseYIndex - 1];
            sellectHexs[0] = hexs[i, baseYIndex];
            sellectHexs[1] = hexs[i + 1, baseYIndex];
        }
        else
        {
            baseYIndex = (y - 1) / 2;
            sellectHexs[2] = hexs[i+(int)(0.5f + xAdd*0.5f) , baseYIndex + 1];
            sellectHexs[1] = hexs[i, baseYIndex];
            sellectHexs[0] = hexs[i + 1, baseYIndex];
        }


        for (int j = 0; j < 3; j++)
        {
            firstPosition[j] = new Vector2(sellectHexs[j].xIndex,sellectHexs[j].yIndex);
        }
        turnTime = 0;

        StartCoroutine(TurnAndCheckMatches());
    }

    private void TurnSellectedHexs()
    {
        turnTime += 1;
        int turnIndex = 0;
        int turnDirectionIndex = turnDirectionClockWise ? 1 : -1;
        //Turn Them
        for (int i = 0; i < 3; i++)
        {
            turnIndex = (3+i + turnDirectionIndex*turnTime) % 3;
            sellectHexs[i].MoveTo(firstPosition[turnIndex]);
        }
    }

    private IEnumerator TurnAndCheckMatches()
    {

        TurnSellectedHexs();

        yield return new WaitForSeconds(gameSpeed*2);

        
        if (turnTime == 3)
        {
            gm.PlayMoveGameMode(true);
            gm.NoMatched();
        }
        else
        {
           CheckAllMatches(sellectHexs[0], sellectHexs[1], sellectHexs[2]);
        }
    }
    #endregion

    #region Check Matches
    private bool IsAnyMatchAtFill(int x,int y,int hexType)
    {
        GetAround(x, y, m_aroundHexArray);
        GetArrayFromAround(m_aroundHexArray, m_aroundIntArray);

        int matchCount = 0;

        int hexTypeAtBack = m_aroundIntArray[5];
        int hexTypeAtFront = -1;
        int checkHexType = -1;

        for (int i = 0; i < 6; i++)
        {
            checkHexType = m_aroundIntArray[i];
            hexTypeAtFront = m_aroundIntArray[(i + 1) % 6];
            //if checkType is null continue
            if (checkHexType == -1)
            {
                hexTypeAtBack = -1;
                continue;
            }
            //if center hex and around is same type look back for third hex piece
            if (hexType == checkHexType && (hexType == hexTypeAtBack || hexType == hexTypeAtFront))
            {
                matchCount += 1;
            }
            hexTypeAtBack = checkHexType;
        }

        if (matchCount >= (smallestMatchNumber - 1))
        {
            return true;
        }

        return false;
    }
    private List<Hex> CheckMatchesAroundAt(Hex hex)
    {
        GetAround(hex.xIndex, hex.yIndex, m_aroundHexArray);
        GetArrayFromAround(m_aroundHexArray, m_aroundIntArray);

        m_allMatchesAt.Clear();

        int hexTypeAtBack = m_aroundIntArray[5];
        int hexType = hex.hexType;
        int hexTypeAtFront = -1;
        int checkHexType = -1;

        for (int i = 0; i < 6; i++)
        {
            checkHexType = m_aroundIntArray[i];
            hexTypeAtFront = m_aroundIntArray[(i+1)%6];
            //if checkType is null continue
            if (checkHexType == -1)
            {
                hexTypeAtBack = -1;
                continue;
            }
            //if center hex and around is same type look back for third hex piece
            if (hexType == checkHexType && (hexType == hexTypeAtBack || hexType == hexTypeAtFront))
            {
                m_allMatchesAt.Add(m_aroundHexArray[i]);
            }
            hexTypeAtBack = checkHexType;
        }

        if(m_allMatchesAt.Count >= (smallestMatchNumber - 1))
        {
            m_allMatchesAt.Add(hex);
            return m_allMatchesAt;
        }
        m_allMatchesAt.Clear();
        return m_allMatchesAt;
    }


    private void CheckAllMatches(Hex hex1, Hex hex2 ,Hex hex3)
    {
        m_allMatches.Clear();
        m_allMatches = CombineLinks(m_allMatches, CheckMatchesAroundAt(hex1));
        m_allMatches = CombineLinks(m_allMatches, CheckMatchesAroundAt(hex2));
        m_allMatches = CombineLinks(m_allMatches, CheckMatchesAroundAt(hex3));

        if (m_allMatches.Count > (smallestMatchNumber - 1))
        {
            //Debug.Log("Count Chaeck Matches");
            DisAppearFallAllCascade(m_allMatches);
        }
        else
        {
            StartCoroutine(TurnAndCheckMatches());
            //NoMatch Swap Back
        }
    }

    #endregion

    #region Fall Disappear Cascade
    private void DisAppearFallAllCascade(List<Hex> allMatches)
    {
        StartCoroutine(DisAppearFallCascadeToutine(allMatches));
    }

    private IEnumerator DisAppearFallCascadeToutine(List<Hex> allMatches)
    {
        //Debug.Log("Count Disappear Matches");

        yield return StartCoroutine(DisappearFall(allMatches));

        comboTimes = 0;


        StartCoroutine(Cascade());

        yield return null;
    }

    private IEnumerator DisappearFall(List<Hex> allMatches)
    {
        disHexs.Clear();
        falledHexs.Clear();

        //Debug.Log("All matches count :" + allMatches.Count);
        yield return new WaitForSeconds(gameSpeed / 2);

        isComplete = false;

        while (!isComplete)
        {
            //Disappear found matches
            disHexs = DisAppearHexs(allMatches);

            yield return new WaitForSeconds(gameSpeed / 2);

            //Fall coloumn that hex disappeared
            falledHexs = FallHexs(disHexs);

            //Debug.Log("Wait for " + (maxFallDistance * gameSpeed) + "seconds for " + falledHexs.Count + " fall hexs");
            //Make combo count there
            comboTimes += 1;
            yield return new WaitForSeconds((maxFallDistance + 1) * gameSpeed);

            maxFallDistance = 0;

            matches.Clear();

            foreach (Hex fallHex in falledHexs)
            {
                matches = CombineLinks(matches, CheckMatchesAroundAt(fallHex));
            }

            //Debug.Log("Combo : " + comboTimes + " Match count :" + matches.Count);

            if (matches.Count == 0)
            {
                //Debug.Log("Count is zero");
                isComplete = true;
                break;
            }
            else
            {

                yield return StartCoroutine(DisappearFall(matches));
            }
        }
        yield return null;
    }


    private IEnumerator Cascade()
    {
        hexIndexes.Clear();
        float timeToFinish = 0.0f;
        if (placeBoom)
        {
            int i = UnityEngine.Random.Range(0, disappearedHexs.Count);
            nextBoomHex = disappearedHexs[i];
            if (!booms.ContainsKey(nextBoomHex))
            {
                nextBoomHex.ChangeToBoom(true);
                nextBoomHex.SetBoomText(5);
                booms.Add(nextBoomHex, 5);
                placeBoom = false;
            }
        }

        foreach (Hex hex in disappearedHexs)
        {
            if (!hexIndexes.Contains(hex.xIndex))
            {
                hexIndexes.Add(hex.xIndex);
            }
        }

        int hexCount = 0;

        foreach (int i in hexIndexes)
        {
            int wait = 0;
            for (int j = 0; j < boardHeigth; j++)
            {

                //look for null objects
                if (hexs[i, j] != null)
                {
                    continue;
                }
                wait += 1;
                hexCount += 1;

                fallHex = disappearedHexs[hexCount - 1];

                //reswap object out of border
                int index = 0;

                //Debug.Log("Cascade at : " + i +" Wait : "+wait +" Hex Count : " + (hexCount-1));

                fallHex.PlaceAt(i, j);

                do
                {
                    //..make it directinal for optimization
                    RandomHexAt(fallHex);
                    index += 1;
                    if (index > 1001)
                    {
                        //Debug.LogWarning("Infinte Loop");
                        break;
                    }
                } while (IsAnyMatchAtFill(fallHex.xIndex, fallHex.yIndex, fallHex.hexType));

                fallHex.ChangePool(false);

                //fall to null point
                fallHex.Cascade(j, wait - 1);
                if(wait > timeToFinish)
                {
                    timeToFinish = wait;
                }
            }
        }
        yield return new WaitForSeconds(timeToFinish* gameSpeed);


        disappearedHexs.Clear();

        TurnFinished();
    }

    private List<int> DisAppearHexs(List<Hex> allMatches)
    {
        fallXIndex.Clear();

        foreach (Hex hex in allMatches)
        {
            if (hex == null || hex.isPooled)
            {
                continue;
            }
            hex.ChangePool(true);

            disappearedHexs.Add(hex);

            //Debug.Log("Disappear at "+ hex.xIndex+" , "+ hex.yIndex);
            hexs[hex.xIndex, hex.yIndex] = null;

            int xFallIndex = hex.xIndex;
            if (!fallXIndex.Contains(xFallIndex))
            {
                fallXIndex.Add(xFallIndex);
            }
        }

        gm.ChangeScore(allMatches.Count);
        /*
        One move done
        count one bomb
        */

        return fallXIndex;
    }


    private List<Hex> FallHexs(List<int> fallColumn)
    {
        movedHexs.Clear();
        foreach (int fll in fallColumn)
        {
            FallColumn(fll);
        }
        return movedHexs;
    }

    private List<Hex> FallColumn(int x)
    {
        for (int y = 0; y < boardHeigth - 1; y++)
        {
            if (hexs[x, y] == null)
            {
                //Debug.Log(x+" , "+y+"NotFound");
                for (int i = y + 1; i < boardHeigth; i++)
                {
                    if (hexs[x, i] != null)
                    {
                        moveHex = hexs[x, i];
                        movedHexs.Add(moveHex);

                        moveHex.Fall(y);


                        maxFallDistance = (maxFallDistance > (i - y)) ? maxFallDistance : (i - y);

                        break;
                    }
                }
            }
        }
        return movedHexs;
    }

    #endregion

    private bool IsInBoard(int x, int y)
    {
        if (x < 0 || y < 0 || x > boardWitdh - 1  || y > boardHeigth - 1)
        {
            //Debug.Log("Out Of Border");
            return false;
        }
        return true;
    }

    private List<Hex> CombineLinks(List<Hex> list1, List<Hex> list2)
    {
        foreach (Hex hex in list2)
        {
            if (!list1.Contains(hex))
            {
                list1.Add(hex);
            }
        }

        return list1;
    }
}
