using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : IComparable<Cell>
{
    Vector2 position; // (x,y) on the grid
    public Vector2 Position { get { return position; } }

    bool walkable;
    public bool Walkable { get { return walkable; } }
    
    public Cell Parent { get; set; }
        
    public float G { get; set; }
    public float F { get; set; } // F = G + H    

    public Cell(Vector2 position, bool walkable = true)
    {
        this.position = position;
        this.walkable = walkable;
    }

    // compare based on F
    // Default comparer for Part type.
    public int CompareTo(Cell compareCell)
    {
        // A null value means that this object is greater.
        if (compareCell == null)
            return 1;

        else
            return this.F.CompareTo(compareCell.F);
    }    
}

public class AStar
{
    const float orthoCost = 10;

    Cell[,] grid;
    Cell start, target;

    List<Cell> openList, closedList;

    public AStar(Cell[,] grid, Cell start, Cell target)
    {
        this.grid = grid;
        this.start = start;
        this.target = target;        
    }

    public List<Cell> Search()
    {
        openList = new List<Cell>();
        closedList = new List<Cell>();

        openList.Add(start);

        bool thereIsPossibilityOfPath = true;
        bool targetReached = false;

        while (thereIsPossibilityOfPath && !targetReached)
        {
            Cell currentCell = GetBestF();
            openList.Remove(currentCell);            
            closedList.Add(currentCell);            

            AddAdjacentWalkables(currentCell);

            thereIsPossibilityOfPath = Convert.ToBoolean(openList.Count);
            targetReached = currentCell == target;
        }

        if(targetReached)
        {
            return GetRoute(target);
        }

        return null;
    }

    List<Cell> GetRoute(Cell endCell)
    {
        List<Cell> route = new List<Cell>();

        route.Add(endCell);

        Cell currentCell = endCell;
        while (currentCell != start)
        {
            currentCell = currentCell.Parent;
            route.Add(currentCell);
        }

        route.Reverse();
        return route;
    }

    Cell GetBestF()
    {
        openList.Sort();
        return openList[0];
    }

    void AddAdjacentWalkables(Cell parent)
    {
        // as long as character cannot move diagonally, we just need to to check orthogonally adjacent cells

        int grid_w = grid.GetLength(0);
        int grid_h = grid.GetLength(1);

        //left:        
        Vector2 leftPosition = parent.Position + Vector2.left;
        AddOneAdjacentWalkable(leftPosition, leftPosition.x, grid_w, parent);

        //right:        
        Vector2 rightPosition = parent.Position + Vector2.right;
        AddOneAdjacentWalkable(rightPosition, rightPosition.x, grid_w, parent);

        //up:        
        Vector2 upPosition = parent.Position + Vector2.up;
        AddOneAdjacentWalkable(upPosition, upPosition.y, grid_h, parent);

        //down:        
        Vector2 downPosition = parent.Position + Vector2.down;
        AddOneAdjacentWalkable(downPosition, downPosition.y, grid_h, parent);

    }

    void AddOneAdjacentWalkable(Vector2 cellPosition, float axisToCheckValue, int axisMax, Cell parent)
    {
        if (axisToCheckValue < 0 || axisToCheckValue >= axisMax) // element index is out of grid
        {
            return;
        }

        Cell _cell = grid[Convert.ToInt32(cellPosition.x), Convert.ToInt32(cellPosition.y)];
        if (_cell.Walkable && !closedList.Contains(_cell))
        {
            if (openList.Contains(_cell))
            {
                /*
                 * If it is on the open list already, check to see if this path to that square is better, using G cost as the measure. A lower G cost means that this is a better path. If so, change the parent of the square to the current square, and recalculate the G and F scores of the square. If you are keeping your open list sorted by F score, you may need to resort the list to account for the change.
                 * */
                // good explanation at ~7:40 here: https://www.youtube.com/watch?v=ySN5Wnu88nE
                var GThruCurrentParent = orthoCost + parent.G;
                if(GThruCurrentParent < _cell.G)
                {
                    var prevG = _cell.G;
                    _cell.G = GThruCurrentParent;
                    _cell.F = _cell.G + (_cell.F - prevG); // don't want to waste CPU time to recalculate H
                    _cell.Parent = parent;
                }
            }
            else
            {
                openList.Add(_cell);
                _cell.Parent = parent;
                _cell.G = orthoCost + parent.G;

                // Manhattan Distance heuristic
                var _dist = _cell.Position - target.Position;
                var _H = (Mathf.Abs(_dist.x) + Mathf.Abs(_dist.y)) * orthoCost; 

                _cell.F = _cell.G + _H;
            }
        }        
    }    
}
 