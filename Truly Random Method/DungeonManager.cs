using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/**
* A persistant singleton monobehaviour that controls that creation 
* of the dungeon, the player, and the enemies.
* For the calculations and illustrations, check notebook 8, page 23
*/ 

public class DungeonManager : MonoBehaviour
{
    public GameObject m_ExitObject = null;
    public GameObject m_WallObject = null;
    public GameObject m_FloorObject = null;
    public GameObject m_DoorObject = null;
    public int m_ChanceForRoom = 75;
    public int m_JumperSpawnChance = 100;
    public TexturePack[] m_WallTexturePacks;

    public int Level;

    private GameObject m_DungeonParent;
    private float m_FontTimer = 0;
    private float m_FontFreq = .25f;
    private int m_FontFlashCount = 6;
    private int m_FontAlpha = 0;
    private int m_WallTexturePackIndex;

    private Cell[,] m_Cells; 

    private float m_SavedPlayerHealth = 100f;

    private int m_SizeX = 0;
    private int m_SizeY = 0;
    private int m_NumFeatures = 0;
    private int m_FeatureSize = 0;
    private Text _levelText = null;

    private static DungeonManager _instance;
    public static DungeonManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DungeonManager>();
                DontDestroyOnLoad(_instance);
            }

            return _instance;
        }
    }

    protected void Awake()
    {
        if (_instance != null && _instance != this)
        {
            _instance.Init();
            Destroy(this.gameObject);
            return;
        }

        
        _instance = this;
        transform.parent = null;
        DontDestroyOnLoad(_instance.gameObject);

        Init();
    }

    //Unity Functions

    protected void Init()
    {
        CameraFade.Instance.FadeIn();
        _levelText = GameObject.Find("Level Text").GetComponent<Text>();

        m_FontFlashCount = 6;
        ClearDungeon();
        Level++;
        _levelText.text = "LEVEL  " + Level;
        GameObject goPlayer = GameObject.FindGameObjectWithTag("Player");
        goPlayer.GetComponent<PlayerActions>().SetHealth(m_SavedPlayerHealth);

        int iSize;
        int iNumFeat;
        int iNumEnemies;
        int iFeatureSize;
        switch (Level)
        {
            case 1:
                iSize = 50;
                iNumFeat = 15;
                iNumEnemies = 8;
                iFeatureSize = 10;
                break;
            case 2:
                iSize = 50;
                iNumFeat = 20;
                iNumEnemies = 20;
                iFeatureSize = 10;
                break;
            case 3:
                iSize = 100;
                iNumFeat = 30;
                iNumEnemies = 30;
                iFeatureSize = 10;
                break;
            case 4:
                iSize = 100;
                iNumFeat = 30;
                iNumEnemies = 40;
                iFeatureSize = 10;
                break;
            case 5:
                iSize = 200;
                iNumFeat = 40;
                iNumEnemies = 60;
                iFeatureSize = 10;
                break;
            case 6:
                iSize = 200;
                iNumFeat = 40;
                iNumEnemies = 70;
                iFeatureSize = 15;
                break;
            default:
                iSize = 200;
                iNumFeat = 100;
                iNumEnemies = 100;
                iFeatureSize = 15;
                break;
        }

        GenerateDungeon(iSize, iSize, iNumFeat, iFeatureSize, iNumEnemies);
        BuildDungeon();
        MusicManager.Instance.PlayBGTrack();
    }

    private void Update()
    {
        if (m_FontFlashCount > 0)
        {
            m_FontTimer += Time.deltaTime;
            if (m_FontTimer >= m_FontFreq)
            {
                m_FontTimer = 0f;
                m_FontFlashCount--;
                m_FontAlpha = m_FontAlpha == 1 ? 0 : 1;
                _levelText.CrossFadeAlpha(m_FontAlpha, .10f, false);
            }
        }
    }

    private void GenerateDungeon(int a_SizeX, int a_SizeY, int a_NumFeatures, int a_FeatureSize, int a_NumEnemies)
    {
        int iAttempts = 0;
        while (true)
        {
            m_WallTexturePackIndex = 0;
            iAttempts++;
            if (iAttempts > 10)
            {
                Debug.LogError("Fatal Error: Dungeon Generator is broken");
                Application.Quit();
            }

            ClearDungeon();
            m_DungeonParent = new GameObject("Dungeon Parent");
            m_SizeX = a_SizeX;
            m_SizeY = a_SizeY;
            m_NumFeatures = a_NumFeatures;
            m_FeatureSize = a_FeatureSize;

            if (!AddRooms()
                || !MakeExit() 
                || !AddEnemies(a_NumEnemies))
            {
                continue;
            }

            break;
        }

    }

    private bool AddRooms()
    {
        //Step 1
        //Fill the dungeon with walls
        m_Cells = new Cell[m_SizeX, m_SizeY];

        for (int x = 0; x < m_SizeX; x++)
        {
            for (int y = 0; y < m_SizeY; y++)
            {
                if (x == 0 || x == m_SizeX - 1 ||
                    y == 0 || y == m_SizeY - 1)
                {
                    //Borders
                    m_Cells[x, y] = new Cell(Cell.CellType.WALL, x, y);
                }
                else
                {
                    m_Cells[x, y] = new Cell(Cell.CellType.NULL, x, y);
                }
            }
        }

        //Step 2
        //Dig a single room
        MakeRoom(m_SizeX / 2, m_SizeY / 2, m_FeatureSize, m_FeatureSize, Random.Range(0, 4));

        int iFeatureCount = 1;
        bool bDone = false;

        //Main Loop
        int iTries = 0;
        int iInteractions = 1000;
        if (Level >= 5)
        {
            iInteractions = 5000;
            if (Level >= 10)
            {
                iInteractions = 10000;
            }
        }

        for (iTries = 0; iTries < iInteractions; ++iTries)
        {
            if (iFeatureCount > m_NumFeatures)
                break;

            int newx = 0;
            int xmod = 0;
            int newy = 0;
            int ymod = 0;
            int iAdjacentDir = -1;

            for (int iTesting = 0; iTesting < 1000 && iAdjacentDir <= -1; ++iTesting)
            {
                newx = Random.Range(1, m_SizeX - 1);
                newy = Random.Range(1, m_SizeY - 1);
                iAdjacentDir = -1;

                if (GetCellType(newx, newy) != Cell.CellType.WALL)
                    continue;

                //Test directions - Start at random
                int iStartDir = Random.Range(0, 4);
                int iTestDir;
                bDone = false;
                for (int i = 0; i < 4 && !bDone; ++i)
                {
                    iTestDir = iStartDir + i;
                    iTestDir = iTestDir % 4;

                    switch (iTestDir)
                    {
                        //North
                        case 0:
                            if (GetCellType(newx, newy + 1) == Cell.CellType.FLOOR)
                            {
                                iAdjacentDir = 0;
                                xmod = 0;
                                ymod = -1;
                                bDone = true;
                            }
                            break;

                        //West
                        case 1:
                            if (GetCellType(newx - 1, newy) == Cell.CellType.FLOOR)
                            {
                                iAdjacentDir = 1;
                                xmod = 1;
                                ymod = 0;
                                bDone = true;
                            }
                            break;

                        //South
                        case 2:
                            if (GetCellType(newx, newy - 1) == Cell.CellType.FLOOR)
                            {
                                iAdjacentDir = 2;
                                xmod = 0;
                                ymod = 1;
                                bDone = true;
                            }
                            break;

                        //East
                        case 3:
                            if (GetCellType(newx + 1, newy) == Cell.CellType.FLOOR)
                            {
                                iAdjacentDir = 3;
                                xmod = -1;
                                ymod = 0;
                                bDone = true;
                            }
                            break;

                    }

                    //Door code
                    //If there is a room ALREADY
                    if (iAdjacentDir > -1)
                    {
                        if (GetCellType(newx, newy + 1) == Cell.CellType.DOOR
                            || GetCellType(newx - 1, newy) == Cell.CellType.DOOR
                            || GetCellType(newx, newy - 1) == Cell.CellType.DOOR
                            || GetCellType(newx + 1, newy) == Cell.CellType.DOOR)
                        {
                            iAdjacentDir = -1;
                        }
                    }

                    //Make a room in the opposite direction (xmod, ymod) of the floor
                    // tile if we are standing on a door
                    if (iAdjacentDir > -1)
                    {
                        int rand = Random.Range(0, 101);
                        if (rand < m_ChanceForRoom)
                        {
                            if (MakeRoom((newx + xmod), (newy + ymod), m_FeatureSize, m_FeatureSize, iAdjacentDir))
                            {
                                iFeatureCount++;
                                SetCell(newx, newy, Cell.CellType.DOOR);
                                SetCell(newx + xmod, newy + ymod, Cell.CellType.FLOOR);
                            }
                        }
                        else
                        {
                            if (MakeCorridor((newx + xmod), (newy + ymod), m_FeatureSize, iAdjacentDir))
                            {
                                iFeatureCount++;
                                SetCell(newx, newy, Cell.CellType.DOOR);
                                SetCell(newx + xmod, newy + ymod, Cell.CellType.FLOOR);
                            }
                        }

                    }
                }
            }
        }

        return true;
    }

    private bool MakeCorridor(int a_PosX, int a_PosY, int a_MaxLen, int a_AdjacentDir)
    {
        int len = Random.Range(5, a_MaxLen);
        int dir = a_AdjacentDir;

        switch (dir)
        {
            //North
            case 0:
                //Check the space
                for (int iY = a_PosY; iY > (a_PosY - len); --iY)
                {
                    if (iY < 0 || iY >= m_SizeY) return false;

                    for (int iX = (a_PosX - 1); iX <= (a_PosX + 1); ++iX)
                    {
                        if (iX < 0 || iX >= m_SizeX) return false;
                        if (m_Cells[iX, iY].Type != Cell.CellType.NULL) return false;
                    }
                }

                // Mark the tiles
                for (int iY = a_PosY; iY > (a_PosY - len); --iY)
                {
                    for (int iX = (a_PosX - 1); iX <= (a_PosX + 1); ++iX)
                    {
                        if (iX == (a_PosX - 1) ||
                            iX == (a_PosX + 1) ||
                            iY == a_PosY ||
                            iY == (a_PosY - len + 1))
                        {
                            SetCell(iX, iY, Cell.CellType.WALL);
                        }
                        else
                        {
                            SetCell(iX, iY, Cell.CellType.FLOOR);
                        }
                    }
                }
                break;

            //East
            case 1:
                //Check the space
                for (int iY = (a_PosY - 1); iY <= (a_PosY + 1); ++iY)
                {
                    if (iY < 0 || iY >= m_SizeY) return false;

                    for (int iX = a_PosX; iX < (a_PosX + len); ++iX)
                    {
                        if (iX < 0 || iX >= m_SizeX) return false;
                        if (m_Cells[iX, iY].Type != Cell.CellType.NULL) return false;
                    }
                }

                // Mark the tiles
                for (int iY = (a_PosY - 1); iY <= (a_PosY + 1); ++iY)
                {
                    for (int iX = a_PosX; iX < (a_PosX + len); ++iX)
                    {
                        if (iX == (a_PosX) ||
                            iX == (a_PosX + (len - 1)) ||
                            iY == (a_PosY - 1) ||
                            iY == (a_PosY + 1))
                        {
                            SetCell(iX, iY, Cell.CellType.WALL);
                        }
                        else
                        {
                            SetCell(iX, iY, Cell.CellType.FLOOR);
                        }
                    }
                }
                break;

            //South
            case 2:
                //Check the space
                for (int iY = a_PosY; iY < (a_PosY + len); ++iY)
                {
                    if (iY < 0 || iY >= m_SizeY) return false;

                    for (int iX = (a_PosX - 1); iX <= (a_PosX + 1); ++iX)
                    {
                        if (iX < 0 || iX >= m_SizeX) return false;
                        if (m_Cells[iX, iY].Type != Cell.CellType.NULL) return false;
                    }
                }

                // Mark the tiles
                for (int iY = a_PosY; iY < (a_PosY + len); ++iY)
                {
                    for (int iX = (a_PosX - 1); iX <= (a_PosX + 1); ++iX)
                    {
                        if (iX == (a_PosX - 1) ||
                            iX == (a_PosX + 1) ||
                            iY == (a_PosY) ||
                            iY == (a_PosY + (len - 1)))
                        {
                            SetCell(iX, iY, Cell.CellType.WALL);
                        }
                        else
                        {
                            SetCell(iX, iY, Cell.CellType.FLOOR);
                        }
                    }
                }
                break;

            //West
            case 3:
                //Check the space
                for (int iY = (a_PosY - 1); iY <= (a_PosY + 1); ++iY)
                {
                    if (iY < 0 || iY >= m_SizeY) return false;

                    for (int iX = (a_PosX); iX > (a_PosX - (len)); --iX)
                    {
                        if (iX < 0 || iX >= m_SizeX) return false;
                        if (m_Cells[iX, iY].Type != Cell.CellType.NULL) return false;
                    }
                }

                // Mark the tiles
                for (int iY = (a_PosY - 1); iY <= (a_PosY + 1); ++iY)
                {
                    for (int iX = (a_PosX); iX > (a_PosX - (len)); --iX)
                    {
                        if (iX == (a_PosX) ||
                            iX == (a_PosX - (len - 1)) ||
                            iY == (a_PosY - 1) ||
                            iY == (a_PosY + 1))
                        {
                            SetCell(iX, iY, Cell.CellType.WALL);
                        }
                        else
                        {
                            SetCell(iX, iY, Cell.CellType.FLOOR);
                        }
                    }
                }
                break;
        }

        return true;
    }

    private bool MakeRoom(int a_PosX, int a_PosY, int a_MaxLengthX, int a_MaxLengthY, int a_Dir)
    {
        int xLen = Random.Range(4, a_MaxLengthX);
        int yLen = Random.Range(4, a_MaxLengthY);

        int dir = a_Dir;

        switch (dir)
        {
            //North
            case 0:
                //Check the space
                for (int iY = a_PosY; iY > (a_PosY - yLen); --iY)
                {
                    if (iY < 0 || iY >= m_SizeY) return false;

                    for (int iX = (a_PosX - xLen / 2); iX < (a_PosX + (xLen + 1) / 2); ++iX)
                    {
                        if (iX < 0 || iX >= m_SizeX) return false;
                        if (m_Cells[iX, iY].Type != Cell.CellType.NULL) return false;
                    }
                }

                // Mark the tiles
                for (int iY = a_PosY; iY > (a_PosY - yLen); --iY)
                {
                    for (int iX = (a_PosX - xLen / 2); iX < (a_PosX + (xLen + 1) / 2); ++iX)
                    {
                        if (iX == (a_PosX - xLen / 2) ||
                            iX == (a_PosX + (xLen - 1) / 2) ||
                            iY == a_PosY ||
                            iY == (a_PosY - yLen + 1))
                        {
                            SetCell(iX, iY, Cell.CellType.WALL);
                        }
                        else
                        {
                            SetCell(iX, iY, Cell.CellType.FLOOR);
                        }
                    }
                }
                break;

            //East
            case 1:
                //Check the space
                for (int iY = (a_PosY - yLen / 2); iY < (a_PosY + (yLen + 1) / 2); ++iY)
                {
                    if (iY < 0 || iY >= m_SizeY) return false;

                    for (int iX = a_PosX; iX < (a_PosX + xLen); ++iX)
                    {
                        if (iX < 0 || iX >= m_SizeX) return false;
                        if (m_Cells[iX, iY].Type != Cell.CellType.NULL) return false;
                    }
                }

                // Mark the tiles
                for (int iY = (a_PosY - yLen / 2); iY < (a_PosY + (yLen + 1) / 2); ++iY)
                {
                    for (int iX = a_PosX; iX < (a_PosX + xLen); ++iX)
                    {
                        if (iX == (a_PosX) ||
                            iX == (a_PosX + (xLen - 1)) ||
                            iY == (a_PosY - yLen / 2) ||
                            iY == (a_PosY + (yLen - 1) / 2))
                        {
                            SetCell(iX, iY, Cell.CellType.WALL);
                        }
                        else
                        {
                            SetCell(iX, iY, Cell.CellType.FLOOR);
                        }
                    }
                }
                break;

            //South
            case 2:
                //Check the space
                for (int iY = a_PosY; iY < (a_PosY + yLen); ++iY)
                {
                    if (iY < 0 || iY >= m_SizeY) return false;

                    for (int iX = (a_PosX - xLen / 2); iX < (a_PosX + (xLen + 1) / 2); ++iX)
                    {
                        if (iX < 0 || iX >= m_SizeX) return false;
                        if (m_Cells[iX, iY].Type != Cell.CellType.NULL) return false;
                    }
                }

                // Mark the tiles
                for (int iY = a_PosY; iY < (a_PosY + yLen); ++iY)
                {
                    for (int iX = (a_PosX - xLen / 2); iX < (a_PosX + (xLen + 1) / 2); ++iX)
                    {
                        if (iX == (a_PosX - xLen / 2) ||
                            iX == (a_PosX + (xLen - 1) / 2) ||
                            iY == (a_PosY) ||
                            iY == (a_PosY + (yLen - 1)))
                        {
                            SetCell(iX, iY, Cell.CellType.WALL);
                        }
                        else
                        {
                            SetCell(iX, iY, Cell.CellType.FLOOR);
                        }
                    }
                }
                break;

            //West
            case 3:
                //Check the space
                for (int iY = (a_PosY - yLen / 2); iY < (a_PosY + (yLen + 1) / 2); ++iY)
                {
                    if (iY < 0 || iY >= m_SizeY) return false;

                    for (int iX = (a_PosX); iX > (a_PosX - (xLen)); --iX)
                    {
                        if (iX < 0 || iX >= m_SizeX) return false;
                        if (m_Cells[iX, iY].Type != Cell.CellType.NULL) return false;
                    }
                }

                // Mark the tiles
                for (int iY = (a_PosY - yLen / 2); iY < (a_PosY + (yLen + 1) / 2); ++iY)
                {
                    for (int iX = (a_PosX); iX > (a_PosX - (xLen)); --iX)
                    {
                        if (iX == (a_PosX) ||
                            iX == (a_PosX - (xLen - 1)) ||
                            iY == (a_PosY - yLen / 2) ||
                            iY == (a_PosY + (yLen - 1) / 2))
                        {
                            SetCell(iX, iY, Cell.CellType.WALL);
                        }
                        else
                        {
                            SetCell(iX, iY, Cell.CellType.FLOOR);
                        }
                    }
                }
                break;
        }

        return true;
    }

    private bool MakeExit()
    {
        //Start & End
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        float fDist = 0f;

        //Pick a floor for the player
        Cell cell = GetRandomCellOType(Cell.CellType.FLOOR);
        if (cell == null)
        {
            Debug.LogError("No more empty floor cells");
            return false;
        }

        player.transform.position = GetCellCenter(cell.X, cell.Y);

        //Get the furthest cell distance for the exit

        float fMaxDist = 0f;
        for (int x = 0; x < m_SizeX; ++x)
        {
            for (int y = 0; y < m_SizeY; ++y)
            {
                if (GetCellType(x, y) != Cell.CellType.FLOOR)
                    continue;

                fDist = Vector3.Distance(player.transform.position, GetCellCenter(x, y));
                if (fDist >= fMaxDist)
                    fMaxDist = fDist;
            }
        }

        bool bDone = false;
        int iAttempts = 0;
        while (!bDone)
        {
            iAttempts++;
            if (iAttempts > 50)
            {
                Debug.LogError("Couldn't create exit object");
                return false;
            }

            //Pick a floor tile
            cell = GetRandomCellOType(Cell.CellType.FLOOR);
            if (cell == null)
            {
                Debug.LogError("No more empty floor cells");
                return false;
            }

            fDist = Vector3.Distance(player.transform.position, GetCellCenter(cell.X, cell.Y));
            if (fDist > fMaxDist * .75f)
            {
                bDone = true;
                var exit = (GameObject) Instantiate(m_ExitObject);
                exit.transform.position = GetCellCenter(cell.X, cell.Y);
                exit.transform.parent = m_DungeonParent.transform;
            }
        }

        return true;
    }

    private bool AddEnemies(int a_NumEnemies)
    {
        EnemyManager.Instance.CreateEnemyParent();
        //Adding enemies and others
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        float fDist = 0f;

        bool bDone = false;
        while (!bDone)
        {
            //Pick a cell at random
            Cell cell = GetRandomCellOType(Cell.CellType.FLOOR);
            if (cell == null)
            {
                Debug.LogError("No more empty floor cells");
                return false;
            }

            fDist = Vector3.Distance(player.transform.position, GetCellCenter(cell.X, cell.Y));
            if (fDist < 10f || EnemyManager.Instance.IsCellFilledWithEnemy(cell.X, cell.Y))
                continue;

            Vector3 vPos = GetCellCenter(cell.X, cell.Y);
            int rnd = Random.Range(0, 101);

            if (rnd < m_JumperSpawnChance)
                EnemyManager.Instance.CreateEnemy(EnemyManager.EnemyTypes.JUMPER, vPos);
            else
                EnemyManager.Instance.CreateEnemy(EnemyManager.EnemyTypes.EVOLVER, vPos);

            if (EnemyManager.Instance.EnemyCount >= a_NumEnemies)
                bDone = true;
        }

        return true;
    }

    private void BuildDungeon()
    {
        Cell.CellType type;
        GameObject go;
        for (int i = 0; i < m_Cells.GetLength(0); ++i)
        {
            for (int j = 0; j < m_Cells.GetLength(1); ++j)
            {
                type = m_Cells[i, j].Type;

                switch (type)
                {
                    case Cell.CellType.WALL:
                        go = (GameObject)Instantiate(m_WallObject);
                        go.transform.position = GetCellCenter(i, j);
                        go.transform.parent = m_DungeonParent.transform;
                        go.GetComponent<Renderer>().material.mainTexture = GetWallTexture();
                        break;
                    case Cell.CellType.FLOOR:
                        go = (GameObject)Instantiate(m_FloorObject);
                        go.transform.position = GetCellCenter(i, j);
                        go.transform.parent = m_DungeonParent.transform;
                        break;
                    case Cell.CellType.DOOR:
                        go = (GameObject)Instantiate(m_FloorObject);
                        go.transform.position = GetCellCenter(i, j);
                        go.transform.parent = m_DungeonParent.transform;
                        break;
                    //case Cell.CellType.NULL:
                    //    go = (GameObject) Instantiate(m_NullObject);
                    //    go.transform.position = GetCellCenter(i, j);
                    //    break;
                    //case Cell.CellType.CORRIDOR:
                    //    go = (GameObject) Instantiate(m_NullObject);
                    //    go.transform.position = GetCellCenter(i, j);
                    //    break;
                }
            }
        }
    }

    private Vector3 GetCellCenter(int a_X, int a_Y)
    {
        Vector3 vPos = Vector3.zero;
        vPos.x = a_X - (m_Cells.GetLength(0) * .5f);
        vPos.z = a_Y - (m_Cells.GetLength(1) * .5f);
        return vPos;
    }

    private void SetCell(int a_X, int a_Y, Cell.CellType a_Type)
    {
        m_Cells[a_X, a_Y].ChangeType(a_Type);
    }

    private void ClearDungeon()
    {
        GameObject go = GameObject.Find("Dungeon Parent");
        if (go != null)
            Destroy(go);

        EnemyManager.Instance.ClearEnemies();
    }

    private List<Cell> GetCellsOfType(Cell.CellType type)
    {
        var cells = new List<Cell>();
        foreach (var cell in m_Cells)
        {
            if (cell.Type == type)
                cells.Add(cell);
        }

        return cells;
    }

    private Cell GetRandomCellOType(Cell.CellType type)
    {
        var cells = GetCellsOfType(type);
        return cells[Random.Range(0, cells.Count)];
    }

    private Texture GetWallTexture()
    {
        int randIndex = Random.Range(0, m_WallTexturePacks[m_WallTexturePackIndex].Textures.Length);
        return m_WallTexturePacks[m_WallTexturePackIndex].Textures[randIndex];
    }


    //Public Functions

    public Cell.CellType GetClosestCell(Vector3 a_Pos, out int a_X, out int a_Y)
    {
        float fDist = 0f;
        a_X = 0;
        a_Y = 0;
        float fMinDist = float.MaxValue;
        Cell.CellType type = Cell.CellType.NULL;

        for (int i = 0; i < m_SizeX; ++i)
        {
            for (int j = 0; j < m_SizeY; ++j)
            {
                fDist = Vector3.SqrMagnitude(GetCellCenter(i, j) - a_Pos);

                if (fDist < fMinDist)
                {
                    fMinDist = fDist;
                    type = GetCellType(i, j);
                    a_X = i;
                    a_Y = j;
                }
            }
        }

        return type;
    }

    public Cell.CellType GetClosestCell(Vector3 a_Pos)
    {
        float fDist = 0f;
        float fMinDist = float.MaxValue;
        Cell.CellType type = Cell.CellType.NULL;

        for (int i = 0; i < m_SizeX; ++i)
        {
            for (int j = 0; j < m_SizeY; ++j)
            {
                fDist = Vector3.SqrMagnitude(GetCellCenter(i, j) - a_Pos);

                if (fDist < fMinDist)
                {
                    fMinDist = fDist;
                    type = GetCellType(i, j);
                }
            }
        }

        return type;
    }

    public Cell.CellType GetCellType(int a_X, int a_Y)
    {
        return m_Cells[a_X, a_Y].Type;
    }

    public void SetSavedPlayerHealth(float a_Health)
    {
        m_SavedPlayerHealth = a_Health;
    }

    public void OnExit()
    {
        //Reset the player saved health
        //Reset the laststand
        m_SavedPlayerHealth = 100f;
    }

}