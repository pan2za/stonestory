using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public GameObject bombPrefab;
    private PlayerUnit player;
    private Rigidbody rigidBody;
    private Animator animator;
    private List<Bomb> bombs;

    private List<Distance> detections;
    private Status state = Status.Idle;
    private bool dodge;

    public bool drop_bomb_on_player;
    public PlayerType mode;
    private bool on_bomb = false;

    private bool bomb;
    private bool powerup;
    private bool follow;

    private Vector3 next_pos;
    private Vector3 next_dir;
    private List<Vector3> path = new List<Vector3>();
    // previous mesh position
    // 1 mesh = 60 frames.
    // 1 mesh is a lattice of the bomberman.
    private Vector3[] prevMeshPos = new Vector3[4];
    //destination position of each agent.
    private Vector3[] destinationPos = new Vector3[4];
    private bool[] hasDestinationPos = new bool[4];
 private string logFilePath;
     private bool isLogInitialized = false;
    private struct Distance
    {
        public Vector3 dir;
        public int dist;

        public string tag;
        public int weight;
    }

    public void Start()
    {
        player = GetComponent<PlayerUnit>();
        bombs = FindObjectOfType<LoadMap>().BombList;
        
        rigidBody = GetComponent<Rigidbody>();
        animator = transform.Find("PlayerModel").GetComponent<Animator>();

        bomb = true;
        follow = true;
        powerup = true;
        dodge = true;
        for(int i = 0; i < 4; i++){
            hasDestinationPos[i] = false;
        }
        LogInitialize();
    }
    // 或者提供公共方法手动初始化
    public void LogInitialize()
    {
        if (isLogInitialized) return;
        
        // 设置日志文件路径，这里保存在应用程序数据目录下的Logs文件夹中
        string logFolderPath = Path.Combine(Application.dataPath, "Logs");
        if (!Directory.Exists(logFolderPath))
        {
            Directory.CreateDirectory(logFolderPath);
        }

        logFilePath = Path.Combine(logFolderPath, $"Log_{System.DateTime.Now:yyyy-MM-dd}.txt");
        Application.logMessageReceived += HandleLog;
        isLogInitialized = true;
    }
    public void Update()
    {
        animator.SetBool("Walking", false);

        if(player.isDead()){
            return;
        }
        if(player.takecare && !player.isWaiting){
            StartCoroutine(WaitAndResetTakecare());
        }
        if (!CanPlaceBomb(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z)))
        {
            // move to any bomb-free path
            //no bomb can be placed.
            //FIXME: if eat bonus PowerUp, I should clear and update the Path.
            MovePath();
            if (bombs.Count > 0 && !HasBombsActive())
                bombs.Clear();
            return;
        }else{
            //2 drop a bomb
            DropBomb();
            // 如果已经计算过路线，按照计算的路线前进
            player.pathList.Clear();
            MovePath();
            return; 
        }
    }


    private bool HasBombsActive()
    {
        int len = bombs.Count;

        for (int i = 0; i < len; i++)
            if (bombs[i] != null) return true;

        return false;
    }

    private bool FindLastCollide(Vector3 pos_test)
    {
        int len = bombs.Count;

        if (len > 0)
        {
            for (int i = 0; i < len; i++)
            {
                if ((bombs[i] != null) && (bombs[i].transform.position.x == pos_test.x || bombs[i].transform.position.z == pos_test.z))
                    return true;
            }
        }

        return false;
    }

    private List<Vector3> GetClosestBreakable(Vector3 position, List<Distance> det)
    {
        List<Vector3> p = new List<Vector3>();
        Distance next = new Distance();
        bool found = false;

        List<Distance> t = det;
        int len = t.Count;

        for(int k = 0; k < len; k++)
        {
            if (string.Compare(t[k].tag, "Breakable") == 0)
            {
                if (t[k].dist < next.dist)
                {
                    next = t[k];
                    found = true;
                }
            }
        }

        if (found)
            p.Add(position + Round(next.dir));
        else
        {
            for(int j = 0; j < len; j++)
            {
                if (t[j].dist >= 1)
                {
                    next = t[j];
                    found = true;
                }
            }

            p.Add(position + Round(next.dir));
        }

        return p;
    }



    private List<Vector3> GetSafePosition(Vector3 pos)
    {
        List<Vector3> result = new List<Vector3>();
        Vector3 location = pos;

        List<Vector3> list = new List<Vector3>();
        bool safePos = false;

        while (!safePos)
        {
            var buffer = FindNearCollisions(location);

            if (GetWays(buffer) == 4)
            {
                safePos = true;
                break;
            }

            bool found = false;

            foreach (Distance d in buffer)
            {
                if (d.dist >= 1)
                {
                    if (!list.Contains(location + d.dir))
                    {
                        location += d.dir;
                        list.Add(location);
                        result.Add(location);

                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                if (result.Count > 1)
                {
                    result.RemoveAt(result.Count - 1);
                    location = result[result.Count - 1];
                }

                if(result.Count <= 1) break;
            }
        }

        return result;
    }

    private void MovePath()
    {
        if(player.PlayerId == 4){
            //FIXME: ONLY DEBUG right down player.
            int x = Mathf.RoundToInt(transform.position.x);
            int z = Mathf.RoundToInt(transform.position.z);
            // 查看他的下一步动作。
        }
        // 如果已经计算过路线，按照计算的路线前进
        if (player.pathList != null && player.pathList.Count > 0)
        {
            // 获取下一个目标点
            Vector3 nextLocation = new Vector3(player.pathList[0].x, transform.position.y, player.pathList[0].y);
            Vector3 currLocation = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            
            // 计算移动方向
            Vector3 direction = (nextLocation - currLocation).normalized;
            
            // 移动玩家
            MovePlayer(direction, nextLocation);
            if(player.PlayerId == 4){
                Debug.Log($"player 4 : {transform.position} moved to {nextLocation}.");
            }
            // 检查是否到达目标点
            if (Vector3.Distance(transform.position, nextLocation) < 0.01f)
            {
                // 移除已到达的点
                player.pathList.RemoveAt(0);
            }
        }
        else
        {
            // 路线用完了，重新计算路线
            DestFinder(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));
            if(player.PlayerId == 4){
                Debug.Log("player 4 : Calculated Path.");
                foreach(var point in player.pathList){
                    Debug.Log($"-> ({point.x}, {point.y})");
                }
            }
        }
        return;
        //if the destination has not been reached.
        // the player should go to that destination.
        
        // destination is 1 mesh
        //       ------
        //       |    |  <----- a mesh
        //       ------
        //    a mesh = 60 frames
        // while a MovePlayer only move 1 frame

        //else:

        //1 I will go 4 directions
        //2 select 1 direction
        //3 calculate the next position
        //4 Can the next position walk?
        //5 no?yes, if yes, step6
        //6 Can the next position a dodge position?
        //7 no?yes, if yes, go

        //FIXME: IS THERE A BOMB NEARBY?
        //IF THERE IS A BOMB
        //DODGE IT
        //
        //detections = FindNearCollisions(transform.position);
        //NextDodgeLocation(detections);
        //MovePlayer(next_dir, next_pos);
        // 随机选一个方向
                    //the move back logic :
                    // from source lattice to destination lattice
                    // NOT from source frame to destination frame.
                    // 
                    //  ------>---------->|
                    //                    |
                    //          X<--------|
                    //   IT'S NOT ALLOWED TO MOVE BACKWARDS
                    // you should turn to other way
                    //  for example, MOVE upwards
                    //
                    //                   V
                    //                   ^|
                    //                   ||
                    //                   ||
                    //  ------>---------->|
                    //                    |
                    //                    |
                    //
                    //
        // 走过去
        // 如果没有就呆住
        
    }

    private void TestBomb()
    {
        if (dodge)
        {
            detections = FindNearCollisions(transform.position);

            if (HasTag(detections, "Bomb") || on_bomb)
            {
                on_bomb = false;
                NextDodgeLocation(detections);
                state = Status.Dodge;
            }
        }
    }

    private ArrayList FindEscapePath(Vector3 dest, Vector3 src)
    {
        ArrayList result = new ArrayList();
        Vector3 location = src;
        ArrayList all = new ArrayList();
        bool safePos = false;

        while (!safePos)
        {
            if (dest.x != location.x && dest.z != location.z)
            {
                safePos = true;
                break;
            }

            var distances = FindNearCollisions(location);
            bool found = false;

            for(int j = 0; j < distances.Count; j++)
            {
                Distance d = distances[j];

                if (d.dist >= 1 && !FindLastCollide(location + d.dir) && !all.Contains(location + d.dir))
                {
                    location += d.dir;
                    all.Add(location);
                    result.Add(location);

                    found = true;
                    break;
                }
            }

            if (!found)
            {
                if (result.Count > 1)
                {
                    result.RemoveAt(result.Count - 1);
                    location = (Vector3)result[result.Count - 1];
                }
                if (result.Count <= 1) break;
            }
        }

        return result;
    }


    private int GetWays(List<Distance> detections)
    {
        int i = 0;

        for(int k = 0; k < detections.Count; k++)
        {
            Distance d = detections[k];
            if (d.dist > 0) i++;
        }

        return i;
    }

    private void NextDodgeLocation(List<Distance> buffer)
    {
        Distance temp = new Distance();
        bool found = false;
        int counter = 0;

        while (!found)
        {
            if (counter > 4)
            {
                found = true;
                continue;
            }

            counter++;


            for(int j = 0; j < buffer.Count; j++)
            {
                Distance d = buffer[j];
                if (d.weight > temp.weight) temp = d;
            }

            next_pos = transform.position + temp.dir;
            next_dir = temp.dir;

            var array = FindNearCollisions(next_pos);

            if (HasTag(array, "Bomb"))
            {
                List<Distance> list = new List<Distance>();
                Distance t = new Distance();

                for(int i = 0; i < buffer.Count; i++)
                {
                    Distance d = buffer[i];

                    if (d.dir == next_dir)
                    {
                        t = d;
                        t.weight = 1 / 2;
                    }
                    else t = d;

                    list.Add(t);
                }

                buffer = list;
            }
            else
                found = true;
        }
    }

    private void MovePlayer(Vector3 direction, Vector3 position)
    {
        float maxDelta = (player.moveSpeed / 2) * Time.deltaTime;
        Vector3 movePosition = Vector3.MoveTowards(transform.position, position, maxDelta);
        // 使用格式化字符串打印位置信息
        // only debug the left top agent.
        //if(transform.position.x < 5 && transform.position.z < 5){
        //    Debug.Log(string.Format(" towards>> X:{0:F2}, Y:{1:F2}, Z:{2:F2}", 
        //        transform.position.x, transform.position.y, transform.position.z));
        //}

        Vector3 prevPos = transform.position;        
        transform.position = movePosition;
        //inform the custom map to update agent position
        //FIXME: Collision will be triggered.
        //change the board there, not here.        
        int x = Mathf.RoundToInt(transform.position.x) - Mathf.RoundToInt(prevPos.x);
        int z = Mathf.RoundToInt(transform.position.z) - Mathf.RoundToInt(prevPos.z);
        if(x == 0 && z == 0)
        {
            // stay in its circle.
            // Do nothing.
        }else{
            // do everything......
            // change prevPosition to passage
            // change currPosition to Agent0

            //FIXME: when should I check collision?
            MyCustomMap.UpdateAgent(prevPos, movePosition);
            MyCustomMap.SetBoard(prevPos, PommermanItem.Passage);
        }
        if (direction == Vector3.forward)
            transform.rotation = Quaternion.Euler(0, 0, 0);
        else if (direction == Vector3.back)
            transform.rotation = Quaternion.Euler(0, 180, 0);
        else if (direction == Vector3.left)
            transform.rotation = Quaternion.Euler(0, 270, 0);
        else if (direction == Vector3.right)
            transform.rotation = Quaternion.Euler(0, 90, 0);

        animator.SetBool("Walking", true);

        if (Vector3.Distance(transform.position, position) == 0)
            state = Status.Idle;
    }

    private bool HasTag(List<Distance> list, string tag)
    {
        int len = list.Count;

        for (int k = 0; k < len; k++)
            if (string.Compare(list[k].tag, tag) == 0)
                return true;

        return false;
    }

    //
    //                |-------O---------|
    //                |       |         |
    //                O-----(pos)-------O 
    //                |       |         |
    //                |-------O---------| 
    //
    // where pos==current location, O== current collision
    private List<Distance> FindNearCollisions(Vector3 pos)
    {
        var temp = new List<Distance>();
        temp.Add(DetectCollision(Vector3.forward, pos));
        temp.Add(DetectCollision(Vector3.back, pos));

        temp.Add(DetectCollision(Vector3.left, pos));
        temp.Add(DetectCollision(Vector3.right, pos));
        return temp;
    }

    private Distance DetectCollision(Vector3 direction, Vector3 position)
    {
        Distance result = new Distance();


        const int maxDistance = 10; // 设置最大检测距离
        RaycastHit hit;

        for (int i = 1; i <= maxDistance; i++)
        {
            Physics.Raycast(position, direction, out hit, i);

            if (hit.collider)
            {

                result.dir = direction;
                result.tag = hit.collider.tag;

                result.dist = i - 1;
                result.weight = result.dist;

                if (string.Compare(result.tag, "Bomb") == 0)
                {
                    if (result.dist > 0)
                    {
                        result.weight = 1 / 2;

                        var obj = hit.collider.gameObject.GetComponent<Bomb>();
                        if (obj != null && !bombs.Contains(obj))
                        {
                            bombs.Add(obj);
                        }
                    }
                }
                break; // 找到碰撞后退出循环
            }
        }

        return result;
    }

    private Vector3 Round(Vector3 pos)
    {
        int xp = Mathf.RoundToInt(pos.x);
        int zp = Mathf.RoundToInt(pos.z);
        var res = new Vector3(xp, .5f, Mathf.RoundToInt(pos.z));
        return res;
    }

    private void DropBomb()
    {
        if (player.avalibleBomb > 0 && bombPrefab)
        {
            var obj = Instantiate(bombPrefab, new Vector3(transform.position.x,
             bombPrefab.transform.position.y, transform.position.z),
             bombPrefab.transform.rotation);

            player.avalibleBomb--;
            var temp = obj.GetComponent<Bomb>();
            temp.PlayerId = player.PlayerId;
            bombs.Add(temp);

            obj.GetComponent<Bomb>().explode_size = player.explosion_power;
            obj.GetComponent<Bomb>().player = player;
            if(player.PlayerId == 4){
                //FIXME: ONLY DEBUG right down player.
                int x = Mathf.RoundToInt(transform.position.x);
                int z = Mathf.RoundToInt(transform.position.z);
                // 查看他的下一步动作。
                Debug.Log($"player 4 drop bomb at {transform.position}.");
            }
            MyCustomMap.SetCollisionBit(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z), PommermanItem.Bomb);
            //3 run away to dodge the bomb.
            player.pathList.Clear();
            // 路线用完了，重新计算路线
            DestFinder(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));
        }
    }

    public void DestFinder(int x, int z) {
        // 遍历棋盘 size=11X11
            // 找到所有不在炸弹射程的Passage点 SavePoint
                // 找寻到SavePoint的路径
                    // 遍历路径
                    // 路径通则设置为终点站
                        // 多条路径，是否会跑到炸弹射程里面，如果会，则放弃这条路径。如果不会，就是安全路径。 Security Path
                        // 向终点站进发。
        // 遍历棋盘
        // 找到空点
        // 找到到空点的路径
        // 每点满足条件：是空点，不在炸弹爆炸时刻的射程内。
        // GO!
        int size = MyCustomMap.GetSize();
        var start = (x, z);
        for(int i = 0; i < size; i++){
            for(int j = 0; j < size; j++){
                if(MyCustomMap.IsBitSet(i, j, PommermanItem.Passage)){
                    // 找到到空点的所有路径
                    ChessboardPathFinder finder = new ChessboardPathFinder(size, size);

                    var end = (i, j);
                    if(MyCustomMap.IsNextToBomb(i, j)){
                        continue;
                    }                    
                    var paths = finder.FindAllPathsWithoutBombs(start, end);
                    foreach (var path in paths)
                    {
                        bool bad = false;
                        //Console.WriteLine("路径:");
                        for(int k = 0; k < path.Count; k++)
                        {
                            var point = path[k];
                            //Console.Write($"({point.x}, {point.y}) -> ");
                            //堵上了，这条路不通
                            if(!CanWalkAwayFromBomb(point.x, point.y, k)){
                                bad = true;
                                break;
                            }
                        }
                        if(!bad){
                            //通路
                            //是否在炸弹爆炸时刻的射程内
                            // go this way.
                            player.pathList = path;
                            return;
                        }
                        //Console.WriteLine();
                    }                   
                }
            }
        }
        
    }
    private float GetBombRange(int x, int z)
    {
        int len = bombs.Count;

        if (len > 0)
        {
            for (int i = 0; i < len; i++)
            {
                if ((bombs[i] != null) && (Mathf.RoundToInt(bombs[i].transform.position.x) == x && Mathf.RoundToInt(bombs[i].transform.position.z) == z))
                    return bombs[i].GetBombRange();
            }
        }

        return 0;
    }
    private float GetBombTimeout(int x, int z)
    {
        int len = bombs.Count;

        if (len > 0)
        {
            for (int i = 0; i < len; i++)
            {
                if ((bombs[i] != null) && (Mathf.RoundToInt(bombs[i].transform.position.x) == x && Mathf.RoundToInt(bombs[i].transform.position.z) == z))
                    return bombs[i].GetBombTimeout();
            }
        }

        return 0;
    }    

    //选择的路径是否碰巧在炸弹爆炸射程里面？
    // x, z is location point of the Path
    // positionAtPath is the point sequence of the Path.
    public bool CanWalkAwayFromBomb(int x, int z, int positionAtPath)
    {
        //可能是刚刚放了炸弹
        if(positionAtPath == 0){
            return true;
        }
        // 已知该点 x，z
        // player跑到该点的时间为t秒（t秒取决于路径）
        float playReachTime = 0.5f*positionAtPath;//FIXME: 3秒后经过,取决于该路径的点的位置，例如第k个点。
        // 附近有炸弹，炸弹爆炸会不会波及该点？
        // 炸弹爆炸不会波及该点，那就可以走。
        // 假设炸弹不会波及该点，怎么判断？
        // 附近有炸弹，炸弹威力是多大？会不会波及？

        // 1 列出所有判断的可能炸弹点。possibleBombPoints
        // x direction
        List<Vector2> possibleBombPoints = new List<Vector2>();
        for(int i = 0; i < MyCustomMap.GetSize(); i++){
                if(MyCustomMap.IsBitSet(x, i, PommermanItem.Bomb)){
                    possibleBombPoints.Add(new Vector2(x, i));
                }
        }
        // z direction
        for(int i = 0; i < MyCustomMap.GetSize(); i++){
                if(MyCustomMap.IsBitSet(i, z, PommermanItem.Bomb)){
                    possibleBombPoints.Add(new Vector2(i, z));
                }
        }      

        // 3 没有发现炸弹，true
        if(possibleBombPoints==null || possibleBombPoints.Count == 0){
            return true;
        }
        // 2 遍历possibleBombPoints
        // 4 检查该炸弹位置 bx, bz
        for(int i = 0; i < possibleBombPoints.Count; i++){
            Vector2 pos = possibleBombPoints[i];
            int bx = Mathf.RoundToInt(pos.x);
            int bz = Mathf.RoundToInt(pos.y);
            //len = 炸弹和空闲点的距离
            float len = Mathf.Abs(bx - x) + Mathf.Abs(bz - z);
            // 炸弹射程
            float range = GetBombRange(bx, bz);
            if(range >= len){
                float timeout = GetBombTimeout(bx, bz);                
                if(Mathf.Abs(playReachTime - timeout) < 0.3){
                    return false;
                }
            }
            //炸弹距离太大，炸不到

            //炸弹炸得太早或太晚，也炸不到。
        }
        // 5 is this bomb have effect on the way(x, z)?
        // 5.1 炸弹长度(bx-x) or (bz-z)小于max则可以安全通过。
        // 5.2 获取bomb爆炸时间，例如2秒后
        // 5.3 获取player经过的时间，例如3秒后。
        // 5.4 如果abs(playpassbytime - explosiontime) < 0.3s,则认为正好撞上爆炸。
        //在炸弹射程内。同x或者同z，则会被炸弹炸死，也是不行的。
        //有没有要爆炸的炸弹？有，则不行。
        //                     |
        //                     |
        //                     |
        //           ----B-----X------B----->x     <------AgentN or Enemy
        //                     |
        //                     |
        //                     |
        //                     B  <-------BooM!

        return true;

    }
   public bool CanPlaceBomb(int x, int z) {
        // 遍历棋盘 size=11X11
            // 找到所有不在炸弹射程的Passage点 SavePoint
                // 找寻到SavePoint的路径
                    // 遍历路径
                    // 路径通则设置为终点站
                        // 多条路径，是否会跑到炸弹射程里面，如果会，则放弃这条路径。如果不会，就是安全路径。 Security Path
                        // 向终点站进发。
        // 遍历棋盘
        // 找到空点
        // 找到到空点的路径
        // 每点满足条件：是空点，不在炸弹爆炸时刻的射程内。
        // GO!
        int size = MyCustomMap.GetSize();
        var start = (x, z);
        for(int i = 0; i < size; i++){
            for(int j = 0; j < size; j++){
                if(MyCustomMap.IsBitSet(i, j, PommermanItem.Passage)){
                    // 找到到空点的所有路径
                    ChessboardPathFinder finder = new ChessboardPathFinder(size, size);

                    var end = (i, j);
                    
                    //目的地上下左右任意3个不是Passage，最后一个方向是FutureBomb。这个也不行
                    if(IsDeadEnd(i, j, x, z)){
                        continue;
                    }                    
                    var paths = finder.FindAllPathsWithoutBombs(start, end);
                    foreach (var path in paths)
                    {
                        bool bad = false;

                        //Console.WriteLine("路径:");
                        for(int k = 0; k < path.Count; k++)
                        {
                            var point = path[k];
                            //Console.Write($"({point.x}, {point.y}) -> ");
                            //堵上了，这条路不通
                            if(!CanWalkAwayFromBomb(point.x, point.y, k)){
                                bad = true;
                                break;
                            }
                            //FIXME:考虑炸弹放下后的行为，炸弹会炸死自己吗？else if(!CanWalkAwayFromFutureBomb(point.x, point.y, k, x, z, 3f)){
                            //    bad = true;
                            //    break;
                            //}
                        }
                        if(!bad){
                            //通路
                            //是否在炸弹爆炸时刻的射程内
                            // go this way.                            
                            return true;
                        }
                        //Console.WriteLine();
                    }                   
                }
            }
        }
        return false;
    }
    bool IsDeadEnd(int dest_x, int dest_z, int futurebomb_x, int futurebomb_z)
    {
        //原地不动作为目的地。放炸弹，会炸死自己。
        if(dest_x == futurebomb_x && dest_z == futurebomb_z){
            return true;
        }
        //确定是挨着的关系。
        if(Math.Abs(dest_x - futurebomb_x) + Math.Abs(dest_z - futurebomb_z) == 1 ){
            //目的周边至少2块空地
            bool isLeftPassable = MyCustomMap.IsBitSet(dest_x-1, dest_z, PommermanItem.Passage);
            bool isRightPassable = MyCustomMap.IsBitSet(dest_x+1, dest_z, PommermanItem.Passage);
            bool isUpPassable = MyCustomMap.IsBitSet(dest_x, dest_z+1, PommermanItem.Passage);
            bool isDownPassable = MyCustomMap.IsBitSet(dest_x, dest_z-1, PommermanItem.Passage);
            int i = 0;
            if(isLeftPassable ){
                i++;
            }
            if(isRightPassable){
                i++;
            }
            if(isUpPassable){
                i++;
            }
            if(isDownPassable){
                i++;
            }
            if(i>=2){
                return false;
            }
        }
        return true;
    }

    private IEnumerator WaitAndResetTakecare()
    {
        player.isWaiting = true; // 标记为正在等待
        
        yield return new WaitForSeconds(3f); // 等待0.5秒
        
        if(player != null) // 确保player引用仍然有效
        {
            Debug.Log($"user {player.PlayerId} add bomb");
            player.avalibleBomb++;
            player.takecare = false;
        }
        
        player.isWaiting = false; // 标记等待结束
    }
    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        string logEntry = $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{type}] {logString}";
        if (type == LogType.Exception || type == LogType.Error)
        {
            logEntry += $"\nStackTrace: {stackTrace}";
        }
        logEntry += "\n";

        try
        {
            File.AppendAllText(logFilePath, logEntry);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to write log: {e.Message}");
        }
    }
}
