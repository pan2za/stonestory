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

    static ulong[,] board;

    // All the bonus covered by Wood
    // Should not be readable to anyone.
    static ulong[,] hiddenBonus;

    static int playerId ;

    public static int GetSize(){
        return size;
    }

    public static void CreateMap(int mapSize)
    {
        size = mapSize;
        int half = size >> 1;

        map = new Block[size * size];
        
        board = new ulong[size, size];

        hiddenBonus = new ulong[size, size];

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

        // clear all
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                board[i, j] = 0;
            }
        }
        // 初始化地图为可破坏方块
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                int index = i * size + j;
                map[index] = Block.Breakable;
                SetCollisionBit(ref board[i, j], PommermanItem.Wood);
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
                SetCollisionBit(ref board[x, y], PommermanItem.Passage);          
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

        SetCollisionBit(ref board[Mathf.RoundToInt(temp[playerId - 1].x), Mathf.RoundToInt(temp[playerId - 1].y)], PommermanItem.Agent0) ;
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
            SetBit(ref board[Mathf.RoundToInt(enemyPositions[i].x), Mathf.RoundToInt(enemyPositions[i].y)], PommermanItem.Agent1 + i); 
        }

        // 设置地图边界为墙壁
        for (int i = 0; i < size; i++)
        {
            map[i] = Block.Wall;
            map[(size - 1) * size + i] = Block.Wall;
            map[i * size] = Block.Wall;
            map[i * size + size - 1] = Block.Wall;

            SetCollisionBit(ref board[i, 0], PommermanItem.Rigid);
            SetCollisionBit(ref board[size -1, i], PommermanItem.Rigid);
            SetCollisionBit(ref board[0, i], PommermanItem.Rigid);
            SetCollisionBit(ref board[i, size - 1], PommermanItem.Rigid);
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
                        SetCollisionBit(ref board[i, j], PommermanItem.Rigid);
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
            GenItems(new int[] { 4, 4, 4 }, 2);
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
                if (IsBitSet(board[dx, dy], PommermanItem.Wood))
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
                        SetCollisionBit(ref hiddenBonus[dx, dy], ToPommermanItem(bonusType));
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


    public static bool CanWalk(int x, int z){
        if(x < 0 || z < 0 || x > size - 1 || z > size - 1){
            return false;
        }

        // 不是空地是不行的。
        ulong nextItem = board[x, z];
        if(!IsBitSet(nextItem, PommermanItem.Passage)){
            return false;
        }
        return true;        
    }
    public static bool CanWalk(Vector3 nextPos)
    {
        return CanWalk(Mathf.RoundToInt(nextPos.x) , Mathf.RoundToInt(nextPos.z) );
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
                return PommermanItem.SpeedUp;
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
        ulong item = hiddenBonus[(int)position.x, (int)position.z];
        if(IsBitSet(item, PommermanItem.ExtraBomb)){
            return (int)BonusType.ExtraBomb;
        }
        if(IsBitSet(item, PommermanItem.IncrRange)){
            return (int)BonusType.IncrRange;
        }
        if(IsBitSet(item, PommermanItem.Kick)){
            return (int)BonusType.Kick;
        }
        if(IsBitSet(item, PommermanItem.SpeedUp)){
            return (int)BonusType.SpeedUp;
        }
        return -1;
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
        int x = Mathf.RoundToInt(position.x);
        int z = Mathf.RoundToInt(position.z);
        SetBit(ref board[x, z], PommermanItem.Passage);
        ulong item = hiddenBonus[x, z];
        if(IsBitSet(item, PommermanItem.ExtraBomb)){
            SetBit(ref hiddenBonus[x, z], PommermanItem.Passage);
            SetBit(ref board[x , z], PommermanItem.ExtraBomb);
        }
        if(IsBitSet(item, PommermanItem.IncrRange)){
            SetBit(ref hiddenBonus[x, z], PommermanItem.Passage);
            SetBit(ref board[x , z], PommermanItem.IncrRange);
        }
        if(IsBitSet(item, PommermanItem.Kick)){
            SetBit(ref hiddenBonus[x, z], PommermanItem.Passage);
            SetBit(ref board[x , z], PommermanItem.Kick);
        }
        if(IsBitSet(item, PommermanItem.SpeedUp)){
            SetBit(ref hiddenBonus[x, z], PommermanItem.Passage);
            SetBit(ref board[x , z], PommermanItem.SpeedUp);
        } 
    }
    public static void RemoveBonus(Vector3 position)
    {
        //change bonus to passage 
        // because of bombing .
        SetBit(ref board[Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z)], PommermanItem.Passage);
        SetBit(ref hiddenBonus[Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z)], PommermanItem.Passage);
    }
    public static void EatBonus(Vector3 position, Vector3 playerLocation)
    {
        //FIXME: why? should remove bonus!
        //change bonus to player 
        // because of player collising.
        ulong item = board[Mathf.RoundToInt(playerLocation.x), Mathf.RoundToInt(playerLocation.z)];
        bool crazy = false;
        if(IsBitSet(item, PommermanItem.Agent0) 
                || IsBitSet(item, PommermanItem.Agent1)
                || IsBitSet(item, PommermanItem.Agent2)
                || IsBitSet(item, PommermanItem.Agent3)
        )
        {
            if(IsBitSet(item, PommermanItem.Agent0)) {
                SetBit(ref board[Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z)], PommermanItem.Agent0);
            }
            if(IsBitSet(item, PommermanItem.Agent1)) {
                SetBit(ref board[Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z)], PommermanItem.Agent1);
            }
            if(IsBitSet(item, PommermanItem.Agent2)) {
                SetBit(ref board[Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z)], PommermanItem.Agent2);
            }
            if(IsBitSet(item, PommermanItem.Agent3)) {
                SetBit(ref board[Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z)], PommermanItem.Agent3);
            }
            //FIXME: player prev positon should be set to passage.
            SetBit(ref board[Mathf.RoundToInt(playerLocation.x), Mathf.RoundToInt(playerLocation.z)],  PommermanItem.Passage);
            SetBit(ref hiddenBonus[Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z)], PommermanItem.Passage);
        }else{
            //I am crazy
            crazy = true;
        }
    }
    //change agent in the board from prevlocation to curr location.
    public static void UpdateAgent(Vector3 prevLocation, Vector3 position)
    {
        ulong item = board[Mathf.RoundToInt(prevLocation.x) , Mathf.RoundToInt(prevLocation.z)];
        if(IsBitSet(item, PommermanItem.Agent0)) {
            SetBit(ref board[Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z)], PommermanItem.Agent0);
        }
        if(IsBitSet(item, PommermanItem.Agent1)) {
            SetBit(ref board[Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z)], PommermanItem.Agent1);
        }
        if(IsBitSet(item, PommermanItem.Agent2)) {
            SetBit(ref board[Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z)], PommermanItem.Agent2);
        }
        if(IsBitSet(item, PommermanItem.Agent3)) {
            SetBit(ref board[Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z)], PommermanItem.Agent3);
        }
    }
    public static void SetBoard(Vector3 position, PommermanItem item)
    {
        //use round function, not use (int) conversion.
        SetBit(ref board[Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z)], item);
    }
    // 清空指定位（将指定位置为0）
    public static void ClearBit(ref ulong value, PommermanItem bitPos)
    {
        int bitPosition = (int)bitPos;
        if (bitPosition < 0 || bitPosition > 63){
            return ;
        }
            //throw new ArgumentException("位位置必须在0到63之间", nameof(bitPosition));

            
        value &= ~(1UL << bitPosition);
    }

    // 判断指定位是否为1
    public static bool IsBitSet(ulong value, PommermanItem bitPos)
    {
        int bitPosition = (int)bitPos;
        if (bitPosition < 0 || bitPosition > 63)
        {
            return false;
        }
        // throw new ArgumentException("位位置必须在0到63之间", nameof(bitPosition));
            
        return (value & (1UL << bitPosition)) != 0;
    }
    // 设置指定位为1
    public static void SetBit(ref ulong value, PommermanItem bitPos)
    {
        int bitPosition = (int)bitPos;
        if (bitPosition < 0 || bitPosition > 63)
        {
            return;
        }
        value |= 1UL << bitPosition; // 使用复合赋值运算符直接修改引用值
    }
    // 冲突的几个直接互相干掉
    public static void SetCollisionBit(ref ulong value, PommermanItem bitPos)
    {
        //set anyway!
        SetBit(ref value, bitPos);
        //rigid passage wood will collised.
        if(bitPos == PommermanItem.Rigid){
            SetBit(ref value, bitPos);
            ClearBit(ref value, PommermanItem.Passage);
            ClearBit(ref value, PommermanItem.Wood);
        }
        if(bitPos == PommermanItem.Passage){
            SetBit(ref value, bitPos);
            ClearBit(ref value, PommermanItem.Rigid);
            ClearBit(ref value, PommermanItem.Wood);
        }
        if(bitPos == PommermanItem.Wood){
            SetBit(ref value, bitPos);
            ClearBit(ref value, PommermanItem.Passage);
            ClearBit(ref value, PommermanItem.Rigid);
        }
        //bonus type will collised.
        if(bitPos == PommermanItem.ExtraBomb){
            SetBit(ref value, bitPos);
            ClearBit(ref value, PommermanItem.IncrRange);
            ClearBit(ref value, PommermanItem.Kick);
            ClearBit(ref value, PommermanItem.SpeedUp);
        }        
        if(bitPos == PommermanItem.IncrRange){
            SetBit(ref value, bitPos);
            ClearBit(ref value, PommermanItem.ExtraBomb);
            ClearBit(ref value, PommermanItem.Kick);
            ClearBit(ref value, PommermanItem.SpeedUp);
        }     
        if(bitPos == PommermanItem.Kick){
            SetBit(ref value, bitPos);
            ClearBit(ref value, PommermanItem.IncrRange);
            ClearBit(ref value, PommermanItem.ExtraBomb);
            ClearBit(ref value, PommermanItem.SpeedUp);
        }     
        if(bitPos == PommermanItem.SpeedUp){
            SetBit(ref value, bitPos);
            ClearBit(ref value, PommermanItem.IncrRange);
            ClearBit(ref value, PommermanItem.ExtraBomb);
            ClearBit(ref value, PommermanItem.Kick);
        }

    }
    public static void SetCollisionBit(int x, int z, PommermanItem item){
        SetCollisionBit(ref board[x, z], item);
    }
    public static bool IsBitSet(int x, int z, PommermanItem bitPos){
        ulong value = board[x, z];
        return IsBitSet(value, bitPos);
    }
}
