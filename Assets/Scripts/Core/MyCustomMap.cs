using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyCustomMap
{
    static PowerUp[] PowersUp;
    static Block[] map;
    static int size;

    // https://gitee.com/yuanzhi0515/playground/blob/master/pommerman/forward_model.py
    // 初始化游戏板
    // same as obs["board"]

    static PommermanItem[,] board;

    // All the bonus covered by Wood
    // Should not be readable to anyone.
    static PommermanItem[,] hiddenBonus;

    static int playerId ;

    public static void CreateMap(int mapSize)
    {
        size = mapSize;
        int half = size >> 1;

        map = new Block[size * size];
        
        board = new PommermanItem[size, size];

        hiddenBonus = new PommermanItem[size, size];

        int players = MyPlayerPrefs.GetPlayers();

        var temp = new Vector2[]
        {
            // players position
            // z
            // ^
            // |
            // 2------3
            // |------|
            // 1------4
            // ----------> x
            // note: x is horizontal.
            // z is vertical.
            new Vector2(1f, 1f),
            new Vector2(1f, size - 2),
            new Vector2(size - 2, size - 2),
            new Vector2(size - 2, 1f),

            new Vector2(half, half),

            new Vector2(1f, half),
            new Vector2(half, 1f),
            new Vector2(size - 2, half),
            new Vector2(half, size - 2),
        };

        // 初始化地图为可破坏方块
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                int index = i * size + j;
                map[index] = Block.Breakable;
                board[i, j] = PommermanItem.Wood;
            }
        }

        // 设置玩家出生点周围为空地
        for (int i = 0; i < players; i++)
        {
            var list = FindLocation(temp[i]);

            for (int j = 0; j < list.Length; j++)
            {
                int x = Mathf.RoundToInt(list[j].x);
                int y = Mathf.RoundToInt(list[j].y);
                map[x * size + y] = Block.Born;     
                board[x, y] = PommermanItem.Passage;           
            }
        }

        // 根据随机设置的玩家位置，调整ID
        // adjust agent id by random player position.
        // playerId from 1 to 4.
        if(playerId == 0)
        {
            playerId = Random.Range(0, players) + 1;
        }
        // Agent0 position
        board[Mathf.RoundToInt(temp[playerId - 1].x), Mathf.RoundToInt(temp[playerId - 1].y)] = PommermanItem.Agent0 ;
        // Agent1~Agent3 position
        Vector2[] enemyPositions = new Vector2[players];
        //agent0, agent1, agent2, agent3
        if(playerId - 1 == 0){
            for(int i = 0; i < players - 1; i++)
            {
                enemyPositions[i] = temp[i + 1];
            }
        }else{
            //agent1, agent2, agent0, agent3
            //                playerId-1,
            int j = 0;
            for(int i = 0; i < players; i++)
            {
                if(i == playerId -1){
                    continue;
                }
                enemyPositions[j++] = temp[i];
            }
        }
        for (int i = 0; i < players - 1; i++)
        {
            board[Mathf.RoundToInt(enemyPositions[i].x), Mathf.RoundToInt(enemyPositions[i].y)] = PommermanItem.Agent1 + i; 
        }

        // 设置地图边界为墙壁
        for (int i = 0; i < size; i++)
        {
            map[i] = Block.Wall;
            map[(size - 1) * size + i] = Block.Wall;
            map[i * size] = Block.Wall;
            map[i * size + size - 1] = Block.Wall;

            board[i, 0] = PommermanItem.Rigid;
            board[size -1, i] = PommermanItem.Rigid;
            board[0, i] = PommermanItem.Rigid;
            board[i, size - 1] = PommermanItem.Rigid;
        }

        // 随机生成墙壁，确保墙壁附近可通行
        for (int i = 2; i < size - 2; i += 2)
        {
            for (int j = 2; j < size - 2; j += 2)
            {
                if (Random.Range(0, 100) < 50) // 50% 的概率生成墙壁
                {
                    if (IsWallValid(i, j))
                    {
                        int index = i * size + j;
                        map[index] = Block.Wall;
                        board[i, j] = PommermanItem.Rigid;
                    }
                }
            }
        }

        GenItems(mapSize);
    }

    // 检查墙壁是否合法（附近有可通行路径）
    static bool IsWallValid(int x, int y)
    {
        // 检查上下左右四个方向是否至少有一个方向可通行
        bool canPass = false;

        if (x > 0 && map[(x - 1) * size + y] != Block.Wall)
            canPass = true;
        if (x < size - 1 && map[(x + 1) * size + y] != Block.Wall)
            canPass = true;
        if (y > 0 && map[x * size + (y - 1)] != Block.Wall)
            canPass = true;
        if (y < size - 1 && map[x * size + (y + 1)] != Block.Wall)
            canPass = true;

        return canPass;
    }

    static void GenItems(int mapSize)
    {
        if(mapSize > 15)
            GenItems(new int[] { 34, 20, 50 }, 2);
        else if(mapSize == 15)
            GenItems(new int[] { 14, 10, 12 }, 2);
        else
            GenItems(new int[] { 2, 3, 4 }, 2);
    }

    static void GenItems(int[] powersUp, int distance)
    {
        int count = 0;
        int len = powersUp.Length;

        var list = new List<PowerUp>();
        bool done = false;

        var items = new Queue<BonusType>();
        int[] g = new int[len];

        for (int i = 0; i < len; i++)
        {
            g[i] = powersUp[i];
            count += g[i];
        }

        while(count > 0)
        {
            //FIXME: No kick
            int index = Random.Range(0, (int)BonusType.SpeedUp);

            if (g[index] > 0)
            {
                items.Enqueue((BonusType)(index + 1));
                g[index]--;
                count--;
            }
        }

        while(items.Count > 0)
        {
            int dx = Random.Range(0, size);
            int dy = Random.Range(0, size);

            int index = dx * size + dy;
             done = false;

            do
            {
                if (board[dx, dy] == PommermanItem.Wood)
                {
                    // board MUST BE Wood, since all the bonus toys should be hidden to anyone.
                    done = true;

                    for(int j = 0; j < list.Count; j++)
                    {
                        int diff = (int)Mathf.Abs(list[j].Location.x - dx);
                        diff += (int)Mathf.Abs(list[j].Location.z - dy);

                        if (diff < distance)
                        {
                            done = false;
                            break;
                        }
                    }

                    if(done)
                    {
                        BonusType bonusType = items.Dequeue();

                        var powerUp = new PowerUp
                        {
                            BonusType = bonusType,
                            Location = new Vector3(dx, .5f, dy)
                        };

                        list.Add(powerUp);
                        hiddenBonus[dx, dy] = ToPommermanItem(bonusType);
                    }
                }

                dx = Random.Range(0, size);
                dy = Random.Range(0, size);
                index = dx * size + dy;
            }
            while (!done);
        }

        PowersUp = list.ToArray();
    }

    public static int GetItem(Vector3 position)
    {
        int index = -1;

        for(int i = 0; i < PowersUp.Length; i++)
        {
            if(Vector3.Distance(position, PowersUp[i].Location) == 0)
            {
                index = i;
                break;
            }
        }

        return index > 0 ? (int)PowersUp[index].BonusType : -1;
    }

    public static Block GetBlock(int j, int k)
    {
        int index = j * size + k;
        return map[index];
    }

    public static bool CanWalk(Vector3 currPos, Vector3 nextPos)
    {
        PommermanItem currItem = board[Mathf.RoundToInt(currPos.x), Mathf.RoundToInt(currPos.z)];
        PommermanItem pommermanItem = board[Mathf.RoundToInt(nextPos.x), Mathf.RoundToInt(nextPos.z)];
        if(currItem == pommermanItem && currItem >= PommermanItem.Agent0 && currItem <= PommermanItem.Agent3){
            // agentX -> agentX
            return true;
        }else{
            return pommermanItem == PommermanItem.Passage
                    || pommermanItem == PommermanItem.IncrRange 
                    || pommermanItem == PommermanItem.ExtraBomb 
                    || pommermanItem == PommermanItem.Kick;
        }
    }

    static Vector2[] FindLocation(Vector2 pos)
    {
        int[] v = new int[] { -1, 0, 1 };
        var list = new List<Vector2>();

        for (int i = 0; i < v.Length; i++)
        {
            for (int j = 0; j < v.Length; j++)
            {
                var point = new Vector2(pos.x + v[i], pos.y + v[j]);

                if (point.x > 0 && point.y > 0)
                    list.Add(point);
            }
        }

        return list.ToArray();
    }
    //convert bonusType to PommermanItem
    static PommermanItem ToPommermanItem(BonusType bonusType )
    {
        switch(bonusType)
        {
            case BonusType.ExtraBomb:
                return PommermanItem.ExtraBomb;
            case BonusType.IncrRange:
                return PommermanItem.IncrRange;
            case BonusType.Kick:
                return PommermanItem.Kick;
            case BonusType.SpeedUp:
                //FIXME: no speedup function in Pommerman,
                //so, when using board[x,y] to judge whether
                // the speedup in this location,
                // I should turn to Unity function itself
                // or other variable.
                return PommermanItem.Passage;
            default:
                return PommermanItem.Passage;
        }
    }
    public static int GetPlayerId()
    {
        if(playerId == 0)
        {
            int players = MyPlayerPrefs.GetPlayers();
            playerId = Random.Range(0, players) + 1;
        }
        return playerId;
    }

    public static int GetBonusType(Vector3 position)
    {
        PommermanItem item = hiddenBonus[(int)position.x, (int)position.z];
        switch(item){
            case PommermanItem.ExtraBomb:
                return (int)BonusType.ExtraBomb;
            case PommermanItem.IncrRange:
                return (int)BonusType.IncrRange;
            case PommermanItem.Kick:
                return (int)BonusType.Kick;
            default:
                return -1;
        }
    }

    //Should I change all the board[x, z] from board[(int)x, (int)z]
    // to
    // board[Mathf.RoundToInt(x), Mathf.RoundToInt(z)]?
    // YES, I THINK SO.
    public static void RevealBonus(Vector3 position)
    {
        // remove the wood from the board
        // remove item in the hiddenBonus
        // add item to the board
        // reveal board <=== hiddenBonus
        board[Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z)] = PommermanItem.Passage;
        PommermanItem item = hiddenBonus[(int)position.x, (int)position.z];
        switch(item){
            case PommermanItem.ExtraBomb:               
            case PommermanItem.IncrRange:
            case PommermanItem.Kick:
                hiddenBonus[(int)position.x, (int)position.z] = PommermanItem.Passage;
                board[(int)position.x , (int)position.z] = item;
                break;
            default:
                //not found any bonus item
                // so I will do nothing
                return;
        }        
    }
    public static void RemoveBonus(Vector3 position)
    {
        //change bonus to passage 
        // because of bombing .
        board[Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z)] = PommermanItem.Passage;
    }
    public static void EatBonus(Vector3 position, Vector3 playerLocation)
    {
        //change bonus to player 
        // because of player collising.
        PommermanItem item = board[Mathf.RoundToInt(playerLocation.x), Mathf.RoundToInt(playerLocation.z)];
        bool crazy = false;
        if(item >= PommermanItem.Agent0 && item <= PommermanItem.Agent3)
        {
            board[Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z)] = item;
            //FIXME: player prev positon should be set to passage.
            board[Mathf.RoundToInt(playerLocation.x), Mathf.RoundToInt(playerLocation.z)] = PommermanItem.Passage;
        }else{
            //I am crazy
            crazy = true;
        }
    }
    //change agent in the board from prevlocation to curr location.
    public static void UpdateAgent(Vector3 prevLocation, Vector3 currLocation)
    {
        PommermanItem item = board[Mathf.RoundToInt(prevLocation.x) , Mathf.RoundToInt(prevLocation.z)];
        board[Mathf.RoundToInt(currLocation.x) , Mathf.RoundToInt(currLocation.z)] = item;
    }
    public static void SetBoard(Vector3 position, PommermanItem item)
    {
        //use round function, not use (int) conversion.
        board[Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z)] = item;
    }
}
