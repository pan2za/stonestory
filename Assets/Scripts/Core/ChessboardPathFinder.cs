using System;
using System.Collections.Generic;

public  class ChessboardPathFinder
{
    // 棋盘大小
    private  int rows, cols;
    // 标记已访问的位置，防止循环
    private  bool[,] visited;
    // 存储所有找到的路径
    private  List<List<(int x, int y)>> allPaths = new List<List<(int x, int y)>>();
    // 移动方向（上下左右）
    private readonly (int dx, int dy)[] directions = {
        (-1, 0), (1, 0), (0, -1), (0, 1)
    };

    public  ChessboardPathFinder(int rows, int cols)
    {
        this.rows = rows;
        this.cols = cols;
        visited = new bool[rows, cols];
    }

    // 主方法：获取所有从start到end的路径
    public  List<List<(int x, int y)>> FindAllPaths((int x, int y) start, (int x, int y) end)
    {
        allPaths.Clear();
        List<(int x, int y)> currentPath = new List<(int x, int y)> { start };
        visited[start.x, start.y] = true;
        
        DFS(start, end, currentPath);
        
        // 重置访问标记
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                visited[i, j] = false;
                
        return allPaths;
    }

    // 深度优先搜索
    private  void DFS((int x, int y) current, (int x, int y) end, List<(int x, int y)> path)
    {
        // 到达终点，记录路径
        if (current.x == end.x && current.y == end.y)
        {
            allPaths.Add(new List<(int x, int y)>(path));
            return;
        }

        // 尝试所有可能的移动方向
        foreach (var dir in directions)
        {
            int newX = current.x + dir.dx;
            int newY = current.y + dir.dy;

            // 检查是否越界或已访问
            if (IsValidMove(newX, newY))
            {
                visited[newX, newY] = true;
                path.Add((newX, newY));
                
                DFS((newX, newY), end, path);
                
                // 回溯：移除当前节点，取消访问标记
                path.RemoveAt(path.Count - 1);
                visited[newX, newY] = false;
            }
        }
    }

    // 检查移动是否合法
    private  bool IsValidMove(int x, int y)
    {
        if( x >= 0 && x < rows && y >= 0 && y < cols && !visited[x, y]){
            if(MyCustomMap.CanWalk(x, y)){
                //判断该点是在炸弹射程内吗？
                //该点上下左右是否有炸弹，炸弹在n秒爆炸。用户在m到k秒到达该点。
                return true;
            }
        }
        return false;
    }

    // 主方法：获取所有从start到end的路径,找到没有炸弹的路径
    public  List<List<(int x, int y)>> FindAllPathsWithoutBombs((int x, int y) start, (int x, int y) end)
    {
        List<List<(int x, int y)>> pathsWithoutBomb = new List<List<(int x, int y)>>();
        var paths = FindAllPaths(start, end);
        foreach (var path in paths)
        {
            bool bad = false;
            for (int i = 0; i < path.Count; i++)
            {
                var point = path[i];
                //FIXME: 暂时只判断前5个点是否有炸弹，有炸弹则不走。
                if(i > 0 && i < 5){
                    if(MyCustomMap.IsBitSet(point.x, point.y, PommermanItem.Bomb)){
                        bad = true;
                        break;
                    }
                }
            }
            if(!bad){
                pathsWithoutBomb.Add(path);
            }
        }                
        return pathsWithoutBomb;
    }

}