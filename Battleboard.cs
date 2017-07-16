using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public delegate void BattleBoardStateChangedEventHandler(object sender, EventArgs e);

public class Battleboard : MonoBehaviour {

    // FSM:
    FSM<FSMState> fsm; // finite-state machine logic
    public FSM<FSMState> FSM { get { return fsm; } }

    FSMState inputEnabledState, inputDisabledState;
    public FSMState InputEnabledState { get { return inputEnabledState; } }
    public FSMState InputDisabledState { get { return inputDisabledState; } }

    public event BattleBoardStateChangedEventHandler StateChanged;

    // UI:
    public GameObject VictoryDefeatedCanvasPrefab;
    GameObject victoryDefeatedCanvas;

    public Button ButtonEndTurn;
    public string characterBoardDataFilename;

    public GameObject TurnQCanvasPrefab;
    GameObject turnQCanvas;


    public GameObject CellInfoPrefab;

    public int width = 5, height = 5;
    public GameObject cellPrefab;

    List<BattleboardCell> currentRoute;    

    public Camera camera;

    GameObject cellInformation;

    static BattleboardCell[,] cells;

    Character currentCharacter;

    Party playerParty, enemyParty;
    Queue<Character> turnQ;

    float adjacentCellsDistance;

    List<Character> charactersInAttack; // how many characters' state changes left until attack is considered complete

    BoardData LoadBoardData()
    {
        BoardData loadedData = null;

        /// for android -- www https://docs.unity3d.com/ScriptReference/Application-streamingAssetsPath.html
        string filePath = Path.Combine(Application.streamingAssetsPath, characterBoardDataFilename);

        if (File.Exists(filePath))
        {
            string dataAsJson = File.ReadAllText(filePath);
            loadedData = JsonUtility.FromJson<BoardData>(dataAsJson);
            Debug.Log("JSON loaded!");
        } else
        {
            Debug.Log("Cannot load characters' board data!"); // #localize
        }

        return loadedData;
    }

    bool CheckEndBattle()
    {
        if (playerParty.IsDead())
        {
            EndBattle(false);
            return true;
        }
        else if (enemyParty.IsDead())
        {
            EndBattle(true);
            return true;
        }

        return false;
    }

    void EndTurn()
    {
        // check if any party is dead
        if (!CheckEndBattle())
        {
            // peek next character
            if (turnQ.Count > 0)
            {
                CharacterHasCompletedTurn(currentCharacter);
                
                // the following short block goes thru queue in search of alive character to make him current
                Character poteintiallyCurrentCharacter;
                do
                {
                    turnQ.Enqueue(turnQ.Dequeue()); // queue previous character back to the end of line 
                    poteintiallyCurrentCharacter = turnQ.Peek();
                } while (poteintiallyCurrentCharacter.IsDead());

                PrepareCharacterForTurn(poteintiallyCurrentCharacter);
            }
        }
    }
    

    /*
     * the human player may end turn by pressing 'End turn' button,
     * but if s/he's out of action points (or maybe some other criteria will appear in the future),
     * turn must be ended automatically by script.
     * */
    void CheckAutomaticEndTurn()
    {
        if (0 == currentCharacter.AP) // out of action points
        {
            EndTurn();
        }
    }

    void CharacterHasCompletedTurn(Character character)
    {
        currentCharacter = null;
        character.IsCurrent = false;
    }

    void PrepareCharacterForTurn(Character character)
    {
        currentCharacter = character;
        character.IsCurrent = true;
        character.RefillAP();
    }

    void EndBattle(bool playerWon)
    {
        // some calculation of experience here
        // ...

        if(null == victoryDefeatedCanvas)
        {
            victoryDefeatedCanvas = Instantiate<GameObject>(VictoryDefeatedCanvasPrefab);
        }

        Transform textTr = victoryDefeatedCanvas.transform.Find("VictoryDefeatedText");
        Text text = textTr.GetComponent<Text>();
        text.text = playerWon ? "Victory!" : "Defeated";

        victoryDefeatedCanvas.SetActive(true);
    }

    private void Awake()
    {
        inputEnabledState = new BattleboardFSMStateInputEnabled(this);
        inputDisabledState = new FSMState(this); // default base state for disable state

        fsm = new FSM<FSMState>();
        fsm.StackStateChanged += Fsm_StackStateChanged;
    }

    private void Fsm_StackStateChanged(object sender, EventArgs e)
    {
        UpdateCellInformationOnStateChanged();
        onStateChanged(); // for ex., WholeGameManager is listening to change cursor
    }

    // Use this for initialization
    void Start() {

        var loadedData = LoadBoardData();

        // I. Create board
        cells = new BattleboardCell[width, height];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                cells[i, j] = Instantiate(cellPrefab, transform.position + new Vector3(i, 0, j), cellPrefab.transform.rotation).GetComponent<BattleboardCell>();
                
                var redefinedCell = loadedData.cells.Where(p => p.x == i && p.y == j).FirstOrDefault<CellData>();
                if (null != redefinedCell)
                {
                    cells[i, j].walkable = redefinedCell.walkable;
                }

                if (cells[i, j].walkable)
                {
                    cells[i, j].Clicked += onCellClicked;
                    cells[i, j].MouseEnter += onCellMouseEnter;
                    cells[i, j].MouseExit += onCellMouseExit;
                }
            }
        }

        // II. Create characters
        playerParty = new Party();
        enemyParty = new Party();

        foreach (var _party in loadedData.parties)
        {
            var isEnemyParty = _party.enemy;
            foreach (var _char in _party.characters)
            {
                var _x = _char.x;
                var _y = _char.y;

                GameObject character = InstantiateCharacter(_char.prefab, _x, _y);
                cells[_x, _y].Character = character.GetComponent<Character>();
                if (_char.maxHP != 0) // yeah, so can't set 0 hp/ap in data file, but do we need it really?
                {
                    cells[_x, _y].Character.MaxHP = _char.maxHP;
                }
                if (_char.maxAP != 0)
                {
                    cells[_x, _y].Character.MaxAP = _char.maxAP;
                }
                if (!String.IsNullOrEmpty(_char._name))
                {
                    cells[_x, _y].Character.Name = _char._name;
                }                
                cells[_x, _y].Character.transform.eulerAngles = _char.Direction;
                cells[_x, _y].Character.Dead += onCharacterDead;
                cells[_x, _y].Character.Moved += onCharacterMoved;
                cells[_x, _y].Character.Attacked += onCharacterAttacked;
                cells[_x, _y].Character.Hit += onCharacterHit;
                
                if (isEnemyParty)
                {                    
                    enemyParty.AddMember(cells[_x, _y].Character);
                } else
                {                    
                    playerParty.AddMember(character.GetComponent<Character>());
                }
                cells[_x, _y].Character.Avatar.GetComponentInChildren<Image>().color = isEnemyParty ? Color.red : Color.green;
                // just #temporarily turn on halo effect for enemies
                Behaviour halo = (Behaviour)cells[_x, _y].Character.GetComponent("Halo");
                halo.enabled = isEnemyParty;                
            }
        }

        /// III Create turn queue
        turnQ = new Queue<Character>();
        // some algorithm of forming the Q (player order may be set upfront, like in Banner Saga)
        for (int i = 0; i < playerParty.Count(); i++)
        {
            turnQ.Enqueue(playerParty[i]);
        }
        for (int i = 0; i < enemyParty.Count(); i++)
        {
            turnQ.Enqueue(enemyParty[i]);
        }


        PositionCharactersAvatars();

        PrepareCharacterForTurn(turnQ.Peek());


        /// IV Other stuff

        // calculate it once and forever
        adjacentCellsDistance = Vector3.Distance(cells[0, 0].transform.position, cells[1, 1].transform.position);
        // some event listeners
        ButtonEndTurn.onClick.AddListener(TaskOnButtonEndTurnClick);
                
        fsm.PushState(inputEnabledState); // input enabled is the default state
    }

    void PositionCharactersAvatars()
    {
        if(null == turnQCanvas)
        {
            turnQCanvas = Instantiate<GameObject>(TurnQCanvasPrefab);
        }

        var turnQSize = turnQ.Count;

        int r = 0;
        foreach (var character in turnQ)
        {
            character.DemonstrateAvatar();
            character.Avatar.transform.SetParent(turnQCanvas.transform, false);            
            r++;
        }
    }

    void TaskOnButtonEndTurnClick()
    {
        if(fsm.GetCurrentState().EndTurnAllowed())
        {
            EndTurn();
        } else
        {
            Debug.Log("End turn button not allowed in this state");
        }
    }

    private void onCharacterAttacked(object sender, EventArgs e)
    {
        AttackOrHitComplete(sender as Character);
    }

    private void onCharacterHit(object sender, EventArgs e)
    {

        AttackOrHitComplete(sender as Character);
    }

    void AttackOrHitComplete(Character character, bool checkForEndBattle = false) // this method is introduced to just avoid DRY in the abobe two handlers
    {
        charactersInAttack.Remove(character);
        if (0 == charactersInAttack.Count) // otherwise, attack or hit event is not yet received
        {
            fsm.GetCurrentState().GoToStateInputEnabled();

            if(checkForEndBattle)
            {
                CheckEndBattle();
            } else
            {
                CheckAutomaticEndTurn();
            }
            
        }
    }

    private void onCharacterMoved(object sender, EventArgs e)
    {
        List<BattleboardCell> route = (e as CharacterMovedEventArgs).Route;
        route[0].Character = null; // now start cell is vacant...
        route[route.Count - 1].Character = sender as Character; // ...and target cell is occupied

        fsm.GetCurrentState().GoToStateInputEnabled();

        CheckAutomaticEndTurn();
    }

    private void onCharacterDead(object sender, EventArgs e)
    {
        var cell = FindCellOfCharacter(sender as Character);
        cell.Character = null; // now it's vacant

        AttackOrHitComplete(sender as Character, true);
    }

    bool AreCellsAdjacent(Vector3 cell1, Vector3 cell2)
    {
        return Vector3.Distance(cell1, cell2) <= adjacentCellsDistance;
    }

    BattleboardCell FindCellOfCharacter(Character character)
    {
        // http://stackoverflow.com/questions/3150678/using-linq-with-2d-array-select-not-found
        var query = from BattleboardCell item in cells
                    where item.Character == character
                    select item;

        return query.First<BattleboardCell>(); // will throw exception, if 'query' equals 'null'
    }

    List<BattleboardCell> BuildRoute(BattleboardCell start, BattleboardCell target)
    {

        if (start == target)
        {
            return null;
        }

        // prepare data for the A* algorithm
        var width = cells.GetLength(0);
        var height = cells.GetLength(1);

        Cell[,] grid = new Cell[width, height];
        Cell gridStart = null, gridTarget = null;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                grid[i, j] = new Cell(new Vector2(i, j), cells[i, j].IsVacant());
                if (cells[i, j] == start)
                {
                    gridStart = grid[i, j];
                }
                if (cells[i, j] == target)
                {
                    gridTarget = grid[i, j];
                }
            }
        }

        if (null == gridStart || null == gridTarget)
        {
            return null;
        }

        AStar aStar = new AStar(grid, gridStart, gridTarget);
        List<Cell> route = aStar.Search();

        if (null == route)
        {
            return null;
        }

        // convert the route to list of battleboard cells
        List<BattleboardCell> resultRoute = new List<BattleboardCell>();
        foreach (var el in route)
        {
            var curCell = cells[Convert.ToInt32(el.Position.x), Convert.ToInt32(el.Position.y)];
            resultRoute.Add(curCell);
        }

        return resultRoute;
    }

    void MarkInRouteCells(bool mark = true)
    {
        if (null == currentRoute)
        {
            return;
        }
        
        for (var i = 1; i < currentRoute.Count; i++) // start from the 2nd node, bc no need for higlighting the one character stands at
        {

            Color markColor;
            if (!mark)
            {
                markColor = Color.clear;
            }
            else
            {
                if (i <= currentCharacter.WalkAP) // <= instead of just 'lower', bc of, again, the 1st node under the character
                {
                    markColor = BattleboardCell.enoughAPColor;
                }
                else
                {
                    markColor = BattleboardCell.noAPColor;
                }
            }

            currentRoute[i].ExternalModificatorColor = markColor;
        }
    }

    void ShowCellInformation(BattleboardCell cell, string information, bool achtung = false)
    {
        Color blue = new Color(0f, 0.03f, 0.56f);
        Color red = new Color(0.56f, 0f, 0.13f);

        var x = cell.transform.position.x;
        var y = cell.transform.position.y;

        if (null == cellInformation)
        {
            cellInformation = Instantiate(CellInfoPrefab);
        }

        cellInformation.transform.position = CellInfoPrefab.transform.position + cell.transform.position;

        // face it straight to the camera w/ animation -- animation effect just irritates
        //StopCoroutine(FaceCellInformationToCamera());
        //StartCoroutine(FaceCellInformationToCamera());
        cellInformation.transform.rotation = camera.transform.rotation;

        var textComp = cellInformation.GetComponentInChildren<Text>();
        textComp.text = information;
        textComp.color = achtung ? red : blue;
        cellInformation.SetActive(true);
    }

    IEnumerator FaceCellInformationToCamera()
    {
        while (cellInformation.transform.rotation != camera.transform.rotation)
        {
            cellInformation.transform.rotation = Quaternion.Lerp(cellInformation.transform.rotation, camera.transform.rotation, Time.deltaTime * 4.5f);
            yield return null;
        }
    }

    public void HideCellInformation()
    {
        if (cellInformation != null)
        {
            cellInformation.transform.rotation = Quaternion.identity;
            cellInformation.SetActive(false);
        }
    }
    
    private void onCellMouseEnter(object sender, EventArgs e)
    {

        if (!fsm.GetCurrentState().CellMouseEnterAllowed())
        {
            Debug.Log("Cell mouse enter not allowed in this state");
            return;
        }

        int APAmountRequired = 0;        
        string cellInformationText = "";
        bool cellInformationTextAchtung = false;

        // Battleboard decides, what action to do if cell clicked (bc Battleboard is kinda CellManager)
        BattleboardCell cell = sender as BattleboardCell;

        if (cell.IsVacant())
        {
            BattleboardCell currentCell = FindCellOfCharacter(currentCharacter);

            currentRoute = BuildRoute(currentCell, cell);

            var routeRealLength = currentRoute.Count - 1;
            APAmountRequired = currentCharacter.WalkCost * routeRealLength;
            if (APAmountRequired > 0) // we need this check here in order not to create cellinfo when mouse is on current character's cell with 0 AP
            {
                // #localize:
                cellInformationText = currentCharacter.APEnoughForWalk(routeRealLength) ?
                                        "Walk here\nfor " + APAmountRequired + " AP" + Utility.GetIntegerEnding(APAmountRequired) :                                        
                                        cellInformationText = String.Format("{0} to walk here\nRequired: {1}", Log.NOT_ENOUGH_AP, APAmountRequired);
                cellInformationTextAchtung = !currentCharacter.APEnoughForWalk(routeRealLength);
            }

            MarkInRouteCells();
        }

        if (cell.IsOccupiedByCharacterOfParty(cell, enemyParty))
        {
            if (AreCellsAdjacent(cell.transform.position, currentCharacter.transform.position))
            {
                APAmountRequired = currentCharacter.AttackCost;

                cell.ExternalModificatorColor = currentCharacter.APEnoughForAttack() ?
                                                    BattleboardCell.enoughAPColor :
                                                    BattleboardCell.noAPColor;
                // #localize:
                cellInformationText = currentCharacter.APEnoughForAttack() ?
                                        String.Format("Attack {0} {3}\nfor {1} AP{2}", cell.Character.GetHealthStatus(), APAmountRequired, Utility.GetIntegerEnding(APAmountRequired), cell.Character.Name) :
                                        String.Format("{0} for attack {2} {3}\nRequired: {1}", Log.NOT_ENOUGH_AP, APAmountRequired, cell.Character.GetHealthStatus(), cell.Character.Name);
                cellInformationTextAchtung = !currentCharacter.APEnoughForAttack();
                
            } else // show enemy's name and health status
            {
                // #localize:
                cellInformationText = String.Format("{0}\n{1}", cell.Character.Name, cell.Character.GetHealthStatus());                
            }
        }

        // if it's out teammate, show his/her name and exact HP
        if (cell.IsOccupiedByCharacterOfParty(cell, playerParty))
        {
            // #localize:
            cellInformationText = String.Format("{0}\nHP: {1}", cell.Character.Name, cell.Character.HP);        
        }

        if(!String.IsNullOrEmpty(cellInformationText))
        {
            ShowCellInformation(cell, cellInformationText, cellInformationTextAchtung);
        }

    }    

    void DestroyRoute()
    {
        MarkInRouteCells(false);
        currentRoute = null;
    }

    private void onCellMouseExit(object sender, EventArgs e)
    {
        if (!fsm.GetCurrentState().CellMouseExitAllowed())
        {
            Debug.Log("Cell mouse exit not allowed in this state");
            return;
        }

        (sender as BattleboardCell).ExternalModificatorColor = Color.clear;

        DestroyRoute();
        HideCellInformation();
    }
    
    private void onCellClicked(object sender, EventArgs e)
    {
        if (!fsm.GetCurrentState().CellClickAllowed())
        {
            Debug.Log("Cell click not allowed in this state");
            return;
        }        

        // Battleboard decides, what action to do if cell clicked (bc Battleboard is kinda CellManager)
        BattleboardCell cell = sender as BattleboardCell;

        if(currentRoute != null)
        {
            if (currentCharacter.APEnoughForWalk(currentRoute.Count - 1))
            {
                fsm.GetCurrentState().GoToStateInputDisabled();

                // it's much easier and clearer to make a copy of the route, and correct it,
                // taking into account amount of APs, than setting a bunch of conditions here, in Character,
                // and in Character's Moving State
                var routeCopy = currentRoute.ToList<BattleboardCell>(); // copying using linq
                currentCharacter.MoveByRoute(routeCopy);
                DestroyRoute(); // we don't need it anymore; in fact, we could emulate onMouseExit, but it would confuse code reading
            }
            else
            {
                Log.Output(Log.NOT_ENOUGH_AP);
            }
        }

        if(cell.IsOccupiedByCharacterOfParty(cell, enemyParty))
        {
            // attempt to attack
            if (AreCellsAdjacent(cell.transform.position, currentCharacter.transform.position))
            {
                if(currentCharacter.APEnoughForAttack())
                {
                    fsm.GetCurrentState().GoToStateInputDisabled();


                    if (null == charactersInAttack)
                    {
                        charactersInAttack = new List<Character>();
                    }
                    else
                    {
                        charactersInAttack.Clear();
                    }
                    charactersInAttack.Add(currentCharacter);
                    charactersInAttack.Add(cell.Character);


                    currentCharacter.AttackThe(cell.Character);
                    (sender as BattleboardCell).ExternalModificatorColor = Color.clear; // in fact, we could emulate onMouseExit, but it would confuse code reading
                } else
                {
                    Log.Output(Log.NOT_ENOUGH_AP);
                }
                
            } else
            {
                Log.Output("Target is too far"); // #localize
            }
        }

        // if cell is occupied by a character from player's party -- don't do anything
        
    }    

    GameObject InstantiateCharacter(string prefabName, int x, int y)
    {
        // "In Unity you usually don't use path names to access assets, instead you expose a reference to an asset by declaring a member-variable, and then assign it in the inspector. When using this technique Unity can automatically calculate which assets are used when building a player. This radically minimizes the size of your players to the assets that you actually use in the built game. When you place assets in "Resources" folders this can not be done, thus all assets in the "Resources" folders will be included in a build."
        GameObject prefab = (GameObject)Resources.Load(prefabName);
        return Instantiate(prefab, transform.position + new Vector3(x, 0, y), prefab.transform.rotation);
    }
    	
	// Update is called once per frame
	void Update () {        
    }

    void UpdateCellInformationOnStateChanged()
    {
        var currentState = fsm.GetCurrentState();
        if (null == currentState)
        {
            return;
        }

        if (currentState == InputDisabledState)
        {
            // update cell
            HideCellInformation();
        }


        if (currentState == InputEnabledState)
        {
            // "generate" onMouseEnter        
            Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition); // Create a ray from the mouse cursor on screen in the direction of the camera.        
            RaycastHit cellHit; // Create a RaycastHit variable to store information about what was hit by the ray.
            var cellMask = LayerMask.GetMask("BattleboardCells");
            if (Physics.Raycast(camRay, out cellHit, 1000f, cellMask)) // Perform the raycast and if it hits something on the floor layer...
            {
                var scriptInstance = cellHit.collider.gameObject.GetComponent<BattleboardCell>();
                onCellMouseEnter(scriptInstance, EventArgs.Empty);
            }
        }     
    }

    void onStateChanged()
    {
        if (StateChanged != null)
            StateChanged(this, EventArgs.Empty);
    }
}
