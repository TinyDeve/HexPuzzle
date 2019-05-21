using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject hexPrefab;
    [SerializeField] private GameManager gm;

    [HideInInspector]
    public Hex[,] hexs;

    [Header("Properties")]
    public float gameSpeed;
    public int boardHeigth = 9;
    public int boardWitdh = 8;
    public int hexTypeNumber = 3;
    public Color[] colors;

    //Check the board where you left
    private bool checkedBefore;
    private int lastCheckX;
    private int lastCheckY;

    private Dictionary<Hex,int> booms = new Dictionary<Hex, int>();
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
    
    void Start()
    {
        if (hexTypeNumber > colors.Length)
        {
            Debug.LogWarning("Not enough sprites added at Board script");
            return;
        }

        hexs = new Hex[boardWitdh, boardHeigth];
        FillBoard();


        /*Used for debug
        for (int i = 0; i < boardWitdh; i++)
        {
            for (int j = 0; j < boardHeigth; j++)
            {

                AroundDebug(i, j);
            }
        }*/

    }

    public void FillBoard()
    {
        for (int i = 0; i < boardWitdh; i++)
        {
            for (int j = 0; j < boardHeigth; j++)
            {
                GameObject hexHolder = Instantiate(hexPrefab, transform) as GameObject;

                hexHolder.name = "Hex : " + i + "," + j;

                Hex hex = hexHolder.GetComponent<Hex>();

                hex.board = this;

                RandomHexAt(hex);

                PlaceHexAt(i, j, hex);

                //..make it around check for optimization
                while (CheckMatchesAt(hex).Count > 0)
                {
                    RandomHexAt(hex);
                }
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
                Hex hex = hexs[i, j];
                if (hex == null)
                {
                    continue;
                }
                hex.board = this;

                RandomHexAt(hex);


                //..make it around check for optimization
                while (CheckMatchesAt(hex).Count > 0)
                {
                    RandomHexAt(hex);
                }
            }
        }
    }

    void RandomHexAt(Hex hex)
    {
        int hexType = UnityEngine.Random.Range(0, hexTypeNumber);
        hex.ChangeType(hexType);
    }

    void PlaceHexAt(int x, int y, Hex hex)
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

    }

    private void TurnFinished()
    {
        Debug.Log("TurnFinished");
        //Check is any boom count is zero
        CheckBoomKill();

        //Check is any more move remain
        CheckMoveKill();


        //Everthing is good start game
        if (gm.gameMode == GameManager.GameMode.Moving)
        {
            gm.gameMode = GameManager.GameMode.Play;
        }


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
            Debug.Log("CheckBoomKill");
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
            Debug.Log("CheckMoveKill");
            gm.GameLossed();
            return;
        }
    }

    private bool CheckForMove()
    {
        //Check forward from last index
        for(int i = 0; i < boardHeigth; i++)
        {
            for (int j = 0;j< boardWitdh; j++)
            {
                if (CheckMoveAtIndexs(i, j))
                {
                    lastCheckX = i;
                    lastCheckY = j;
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

    private bool CheckMoveAtIndexs(int x,int y)
    {
        Hex[] around = GetAround(x, y);
        
        return CheckMoveFromAround(around);
    }

    public void AroundDebug(int x,int y)
    {
        if (CheckMoveFromAround(GetAround(x, y)))
        {
            foreach (Hex hex in GetAround(x, y))
            {
                if (hex == null) continue;
                hex.transform.localScale = new Vector3(0.5f, 0.5f);
            }
        }
    }

    private Hex[] GetAround(int x,int y)
    {
        Hex[] around = new Hex[6];

        around[0] = GetHexSave(x , y + 1);
        around[3] = GetHexSave(x , y - 1);

        if(x%2 == 0)
        {
            around[2] = GetHexSave(x + 1, y);
            around[4] = GetHexSave(x - 1, y);

            around[1] = GetHexSave(x + 1, y + 1);
            around[5] = GetHexSave(x - 1, y + 1);
        }
        else
        {
            around[1] = GetHexSave(x + 1, y);
            around[5] = GetHexSave(x - 1, y);


            around[2] = GetHexSave(x + 1, y - 1);
            around[4] = GetHexSave(x - 1, y - 1);
        }

        return around;
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
        int[] aroundArray = GetArrayFromAround(around);
        return CheckMoveFromArray(aroundArray);

        /*int lastHexType = -1;
        if (around[5] != null)
        {
            lastHexType = around[5].hexType;
        }
        

        for(int i = 0; i < 6; i++)
        {
            if (around[i] == null)
            {
                //No hex at location so assing a noHexType value
                lastHexType = -1;
                continue;
            }
            if (lastHexType == around[i].hexType)
            {
                int sameHexNumber = 0;
                foreach(Hex hex in around)
                {
                    if (hex.hexType == lastHexType)
                    {
                        sameHexNumber += 1;
                    }
                }
                if (sameHexNumber >= 3)
                {
                    return true;
                }

            }
            lastHexType = around[i].hexType;
        }
        return false;*/
    }

    private int[] GetArrayFromAround(Hex[] around)
    {
        int[] aroundArray = new int[6];

        for (int i = 0; i < 6; i++)
        {
            Hex hex = around[i];
            
            if(hex == null)
            {
                aroundArray[i] = -1;
                continue;
            }
            aroundArray[i] = hex.hexType;

        }
        return aroundArray;
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


        /*For turn debug at the end
        sellectHexs[0].ChangeToBoom(true);
        sellectHexs[1].ChangeToBoom(true);
        sellectHexs[2].ChangeToBoom(true);

        sellectHexs[0].SetBoomText(0);
        sellectHexs[1].SetBoomText(1);
        sellectHexs[2].SetBoomText(2);*/


        for (int j = 0; j < 3; j++)
        {
            firstPosition[j] = new Vector2(sellectHexs[j].xIndex,sellectHexs[j].yIndex);
        }

        /*
        foreach (Hex hex in sellectHexs){
        Debug.Log("Sellect :"+hex.name);
            hex.transform.localScale = new Vector2(1.5f, 1.5f);
        }*/

        turnTime = 0;

        StartCoroutine(TurnAndCheckMatches());

        //CheckAllMatches(sellectHexs[0], sellectHexs[1], sellectHexs[2]);
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
            if (gm.gameMode == GameManager.GameMode.Moving)
            {
                gm.gameMode = GameManager.GameMode.Play;
                
                //No Change
                
                gm.NoMatched();
            }
        }
        else
        {
           CheckAllMatches(sellectHexs[0], sellectHexs[1], sellectHexs[2]);
        }
    }
    #endregion


    #region Check Matches
    private bool IsSameHexAt(int x,int y,int hexType)
    {
        return IsInBoard(x, y) && IsExit(x,y) && hexs[x, y].hexType == hexType;
    }

    private bool IsExit(int x,int y)
    {
        return hexs[x, y];
    }

    List<Hex> LookForUpDownGroupMatches(Hex hex,int upDown)
    {
        List<Hex> matchedHexs = new List<Hex>();

        int xIndex = hex.xIndex;
        int yIndex = hex.yIndex + upDown;
        int hexType = hex.hexType;
        
        //Look up or down for matches
        if (IsSameHexAt(xIndex,yIndex,hexType))
        {
            matchedHexs.Add(hexs[xIndex, yIndex]);

            int leftRigthYIndex = yIndex;

            if (xIndex % 2 != 0 && upDown == +1)
            {
                leftRigthYIndex -= 1;
            }

            if(xIndex % 2 == 0 && upDown == -1)
            {
                leftRigthYIndex += 1;
            }

            //Look at left to complete 3
            xIndex -= 1;
            if(IsSameHexAt(xIndex, leftRigthYIndex,hexType))
            {
                matchedHexs.Add(hexs[xIndex, leftRigthYIndex]);
            }

            //Look at rigth to complete 3
            xIndex += 2;
            if (IsSameHexAt(xIndex, leftRigthYIndex, hexType))
            {
                matchedHexs.Add(hexs[xIndex, leftRigthYIndex]);
            }

        }

        return matchedHexs;

    }

    List<Hex> LookForLeftRigthGroupMatches(Hex hex, int leftRigth)
    {
        List<Hex> matchedHexs = new List<Hex>();

        int xIndex = hex.xIndex + leftRigth;
        int yIndex = hex.yIndex;
        int hexType = hex.hexType;

        //Look up or down for matches
        if (IsSameHexAt(xIndex, yIndex, hexType))
        {
            matchedHexs.Add(hexs[xIndex, yIndex]);

            int leftRigthYIndex = yIndex;

            if(xIndex % 2 != 0)
            {
                leftRigthYIndex += 1;
            }
            else
            {
                leftRigthYIndex -= 1;
            }

            //Look at left to complete 3
            if (IsSameHexAt(xIndex, leftRigthYIndex, hexType))
            {
                matchedHexs.Add(hexs[xIndex, leftRigthYIndex]);
            }

            //Look at rigth to complete 3
            if (IsSameHexAt(xIndex, leftRigthYIndex, hexType))
            {
                matchedHexs.Add(hexs[xIndex, leftRigthYIndex]);
            }

        }

        return matchedHexs;

    }

    List<Hex> CheckMatchesVertical(Hex hex)
    {

        List<Hex> upMatches = LookForUpDownGroupMatches(hex, 1);
        List<Hex> downMatches = LookForUpDownGroupMatches(hex, -1);

        List<Hex> allMatches = new List<Hex>();

        if (upMatches != null)
        {
            if (upMatches.Count >= (smallestMatchNumber - 1))
            {
                allMatches = CombineLinks(allMatches, upMatches);
            }
        }


        if (downMatches != null)
        {
            if (downMatches.Count >= (smallestMatchNumber - 1))
            {
                allMatches = CombineLinks(allMatches, downMatches);
            }
        }


        if (allMatches.Count >= (smallestMatchNumber - 1))
        {
            return allMatches;
        }

        return new List<Hex>();
    }


    List<Hex> CheckMatchesHorizontal(Hex hex)
    {

        List<Hex> upMatches = LookForLeftRigthGroupMatches(hex, 1);
        List<Hex> downMatches = LookForLeftRigthGroupMatches(hex, -1);

        List<Hex> allMatches = new List<Hex>();

        if (upMatches != null)
        {
            if (upMatches.Count >= (smallestMatchNumber - 1))
            {
                allMatches = CombineLinks(allMatches, upMatches);
            }
        }


        if (downMatches != null)
        {
            if (downMatches.Count >= (smallestMatchNumber - 1))
            {
                allMatches = CombineLinks(allMatches, downMatches);
            }
        }


        if (allMatches.Count >= (smallestMatchNumber - 1))
        {
            return allMatches;
        }

        return new List<Hex>();
    }


    List<Hex> CheckMatchesAt(Hex hex)
    {

        List<Hex> upMatches = CheckMatchesVertical(hex);
        List<Hex> downMatches = CheckMatchesHorizontal(hex);

        List<Hex> allMatches = new List<Hex>();

        if (upMatches != null)
        {
            if (upMatches.Count >= (smallestMatchNumber - 1))
            {
                allMatches = CombineLinks(allMatches, upMatches);
            }
        }


        if (downMatches != null)
        {
            if (downMatches.Count >= (smallestMatchNumber - 1))
            {
                allMatches = CombineLinks(allMatches, downMatches);
            }
        }


        allMatches.Add(hex);
        if (allMatches.Count > (smallestMatchNumber - 1))
        {
            return allMatches;
        }

        return new List<Hex>();
    }

    void CheckAllMatches(Hex hex1, Hex hex2 ,Hex hex3)
    {
        List<Hex> allMatches = new List<Hex>();

        allMatches = CombineLinks(CheckMatchesAt(hex1), CheckMatchesAt(hex2));
        allMatches = CombineLinks(allMatches, CheckMatchesAt(hex3));
        //Debug.Log("" + allMatches.Count);

        if (allMatches.Count > (smallestMatchNumber - 1))
        {
            //Debug.Log("Count Chaeck Matches");
            DisAppearFallAllCascade(allMatches);
        }
        else
        {
            StartCoroutine(TurnAndCheckMatches());
            //NoMatch Swap Back
        }
    }

    #endregion

    #region Fall Disappear Cascade
    void DisAppearFallAllCascade(List<Hex> allMatches)
    {
        StartCoroutine(DisAppearFallCascadeToutine(allMatches));
    }

    IEnumerator DisAppearFallCascadeToutine(List<Hex> allMatches)
    {
        //Debug.Log("Count Disappear Matches");

        yield return StartCoroutine(DisappearFall(allMatches));

        comboTimes = 0;


        StartCoroutine(Cascade());

        disappearedHexs = new List<Hex>();

        yield return null;
    }

    IEnumerator DisappearFall(List<Hex> allMatches)
    {
        List<int> disHexs = new List<int>(); ;
        List<Hex> falledHexs = new List<Hex>();
        List<Hex> matches = new List<Hex>();

        Debug.Log("All matches count :" + allMatches.Count);
        yield return new WaitForSeconds(gameSpeed / 2);

        isComplete = false;

        while (!isComplete)
        {
            //Disappear found matches
            disHexs = DisAppearHexs(allMatches);

            yield return new WaitForSeconds(gameSpeed / 2);

            //Fall coloumn that hex disappeared
            falledHexs = FallHexs(disHexs);

            Debug.Log("Wait for " + (maxFallDistance * gameSpeed) + "seconds for " + falledHexs.Count + " fall hexs");
            //Make combo count there
            comboTimes += 1;
            yield return new WaitForSeconds((maxFallDistance + 1) * gameSpeed);

            maxFallDistance = 0;

            matches = new List<Hex>();

            foreach (Hex fallHex in falledHexs)
            {
                matches = CombineLinks(matches, CheckMatchesAt(fallHex));
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


    IEnumerator Cascade()
    {
        List<int> hexIndexes = new List<int>();
        float timeToFinish = 0.0f;
        if (placeBoom)
        {
            int i = UnityEngine.Random.Range(0, disappearedHexs.Count);
            Hex hex = disappearedHexs[i];
            if (!booms.ContainsKey(hex))
            {
                hex.ChangeToBoom(true);
                hex.SetBoomText(5);
                booms.Add(hex, 5);
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

                Hex hex = disappearedHexs[hexCount - 1];

                //reswap object out of border
                int index = 0;

                //Debug.Log("Cascade at : " + i +" Wait : "+wait +" Hex Count : " + (hexCount-1));


                PlaceHexAt(i, j, hex);
                hex.transform.position = new Vector3(i, j, 0);

                do
                {
                    //..make it directinal for optimization
                    RandomHexAt(hex);
                    index += 1;
                    if (index > 1001)
                    {
                        //Debug.LogWarning("Infinte Loop");
                        break;
                    }
                } while (CheckMatchesAt(hex).Count > 0);

                hex.ChangePool(false);

                //fall to null point
                hex.Cascade(j, wait - 1);
                if(wait > timeToFinish)
                {
                    timeToFinish = wait;
                }
            }
        }
        yield return new WaitForSeconds(timeToFinish* gameSpeed);
        TurnFinished();
    }

    List<int> DisAppearHexs(List<Hex> allMatches)
    {
        List<int> fallXIndex = new List<int>();

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


    List<Hex> FallHexs(List<int> fallColumn)
    {
        List<Hex> movedHexs = new List<Hex>();
        foreach (int fll in fallColumn)
        {
            movedHexs = CombineLinks(movedHexs, FallColumn(fll));
        }
        return movedHexs;
    }

    List<Hex> FallColumn(int x)
    {
        List<Hex> movedHexs = new List<Hex>();
        for (int y = 0; y < boardHeigth - 1; y++)
        {
            if (hexs[x, y] == null)
            {
                //Debug.Log(x+" , "+y+"NotFound");
                for (int i = y + 1; i < boardHeigth; i++)
                {
                    if (hexs[x, i] != null)
                    {
                        Hex moveHex = hexs[x, i];
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

    bool IsInBoard(int x, int y)
    {
        if (x < 0 || x > boardWitdh - 1 || y < 0 || y > boardHeigth - 1)
        {
            //Debug.Log("Out Of Border");
            return false;
        }
        return true;
    }

    List<Hex> CombineLinks(List<Hex> list1, List<Hex> list2)
    {
        if (list1 == null && list2 == null)
        {
            return new List<Hex>();
        }
        else if (list1 == null)
        {
            return list2;
        }
        else if (list2 == null)
        {
            return list1;
        }

        foreach (Hex hex in list1)
        {
            if (!list2.Contains(hex))
            {
                list2.Add(hex);
            }
        }

        return list2;
    }
    List<int> CombineLinks(List<int> list1, List<int> list2)
    {
        if (list1 == null && list2 == null)
        {
            return new List<int>();
        }
        else if (list1 == null)
        {
            return list2;
        }
        else if (list2 == null)
        {
            return list1;
        }

        foreach (int xIndex in list2)
        {
            if (!list1.Contains(xIndex))
            {
                list1.Add(xIndex);
            }
        }

        return list1;
    }
}
