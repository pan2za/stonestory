using System;
using System.Collections;
using System.Collections.Generic;
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
    }

    public void Update()
    {
        animator.SetBool("Walking", false);

        int agentPos = 0;
        if(transform.position.x < 3 && transform.position.z < 3)
        {
            agentPos = 1; // left down
        }else if(transform.position.x < 3 && transform.position.z > 18)
        {
            agentPos = 2; // right down
        }else if(transform.position. x > 18 && transform.position.z < 3)
        {
            agentPos = 3; // left up
        }else if(transform.position.x > 18 && transform.position. z > 18)
        {
            agentPos = 4; // right up
        }

        if (!CanPlaceBomb())
        {
            // move to any bomb-free path
            //no bomb can be placed.
            detections = FindNearCollisions(transform.position);

            if (HasTag(detections, "Bomb"))
            {
                NextDodgeLocation(detections);
                MovePlayer(next_dir, next_pos);
                return;
            }
            else if (HasTag(detections, "PowerUp"))
            {
                int count = detections.Count;

                for (int k = 0; k < count; k++)
                {
                    if (string.Compare(detections[k].tag, "PowerUp") == 0)
                    {
                        path.Clear();
                        path.Add(transform.position + detections[k].dir);
                        MovePath();
                        break;
                    }
                }
            }
            else
            {
                MovePath();
                if (bombs.Count > 0 && !HasBombsActive())
                    bombs.Clear();
            }
            return;
        }else{
            //2 drop a bomb
            //FIXME: should drop a bomb
            //DropBomb();
            //3 run away to dodge the bomb.
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

    private List<Vector3> FollowLocation(Vector3 startpos, Vector3 goal)
    {
        bool done = false;
        List<Vector3> result = new List<Vector3>();
        Vector3 currentpos = startpos;

        var temp = FindObjectOfType<LoadMap>();
        Vector3 old_pos = startpos;

        while (!done)
        {
            if (currentpos == goal)
            {
                done = true;
                continue;
            }
            old_pos = currentpos;
            foreach (Distance d in FindNearCollisions(currentpos))
            {
                Vector3 temp_pos = Round(currentpos + d.dir);
                if (MyCustomMap.CanWalk(startpos, temp_pos))
                {
                    if (Math.Abs(goal.x - temp_pos.x) < Math.Abs(goal.x - currentpos.x) ||
                    Math.Abs(goal.z - temp_pos.z) < Math.Abs(goal.z - currentpos.z))
                    {

                        result.Add(currentpos);
                        currentpos = temp_pos;
                        break;
                    }
                }
            }

            if (currentpos == old_pos)
                done = true;
        }

        return result;
    }

    private Vector3 FindRandomWalk()
    {
        var m = FindObjectOfType<LoadMap>();
        Vector3 res = Vector3.zero;
        bool found = false;

        while (!found)
        {
            Vector3 test = new Vector3(UnityEngine.Random.Range(1, m.size - 1), 0, UnityEngine.Random.Range(1, m.size - 1));

            if (MyCustomMap.CanWalk(transform.position, test))
            {
                found = true;
                res = test;
            }
        }

        return res;
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
        //if the destination has not been reached.
        // the player should go to that destination.
        
        // destination is 1 mesh
        //       ------
        //       |    |  <----- a mesh
        //       ------
        //    a mesh = 60 frames
        // while a MovePlayer only move 1 frame

        if(hasDestinationPos[player.PlayerId - 1]){
            if(Vector3.Distance(destinationPos[player.PlayerId - 1] , transform.position) > 0.1f){
                Vector3 direction = (destinationPos[player.PlayerId - 1] - transform.position).normalized;
                MovePlayer(direction, destinationPos[player.PlayerId - 1]);
                return;
            }else{
                //so near, update..
            }
        }else{
            // the first time.
        }
        if(transform.position.x < 5 && transform.position.z < 5){
            Debug.Log(string.Format(" towards>> X:{0:F2},  Z:{1:F2}", 
                destinationPos[player.PlayerId - 1].x, destinationPos[player.PlayerId - 1].z));
        }
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
        Vector3[] locations= new Vector3[4]; 
        float maxDelta3 = 1;// (player.moveSpeed / 2) * Time.deltaTime ;
        //right
        locations[0].x = transform.position.x + maxDelta3;
        locations[0].y = transform.position.y ;
        locations[0].z = transform.position.z ;
        //left
        locations[1].x = transform.position.x - maxDelta3;
        locations[1].y = transform.position.y ;
        locations[1].z = transform.position.z ;
        //up
        locations[2].x = transform.position.x ;
        locations[2].y = transform.position.y ;
        locations[2].z = transform.position.z + maxDelta3;
        //down
        locations[3].x = transform.position.x ;
        locations[3].y = transform.position.y ;
        locations[3].z = transform.position.z - maxDelta3;
       
        // 上下左右
        // 如果有一个可以走
        // if MUST move backward, I will move backward.
        // if I can move to another way, I will not move backward.
        int count = 0;
        for(int i = 0; i < 4; i++)
        {
            Vector3 nextLocation = locations[i];
            Vector3 direction = (nextLocation - transform.position).normalized;

            if(MyCustomMap.CanWalk(transform.position, nextLocation))
            {
                count++;
            }
        }

        if(count == 0)
        {
            // Can't Move....
            // Stuck here.
        }else if(count == 1){
            // move this way
            for(int i = 0; i < 4; i++)
            {
                Vector3 nextLocation = locations[i];
                Vector3 direction = (nextLocation - transform.position).normalized;

                if(MyCustomMap.CanWalk(transform.position, nextLocation))
                {
                    prevMeshPos[player.PlayerId - 1] = destinationPos[player.PlayerId - 1];
                    destinationPos[player.PlayerId - 1] = locations[i];
                    hasDestinationPos[player.PlayerId - 1] = true;
                    MovePlayer(direction, nextLocation);
                    break;
                }
            }            
        }else{
            int randNum = UnityEngine.Random.Range(0, count);
            // move forward, will not move backward..
            for(int i = 0; i < 4; i++)
            {
                Vector3 nextLocation = locations[i];
                Vector3 direction = (nextLocation - transform.position).normalized;

                if(MyCustomMap.CanWalk(transform.position, nextLocation))
                {
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
                    if(Vector3.Distance(prevMeshPos[player.PlayerId - 1], locations[i]) > 0.1f)
                    {
                        prevMeshPos[player.PlayerId - 1] = destinationPos[player.PlayerId - 1];
                        destinationPos[player.PlayerId - 1] = locations[i];
                        hasDestinationPos[player.PlayerId - 1] = true;
                        MovePlayer(direction, nextLocation);
                        break;
                    }
                }
            }
        }
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

    private bool CanPlaceBomb()
    {
        if(player.avalibleBomb == 0){
            return false;
        }
        detections = FindNearCollisions(transform.position);

        for (int k = 0; k < detections.Count; k++)
        {
            Distance d = detections[k];

            if (d.tag == "Breakable" || (d.tag == "Player" && drop_bomb_on_player))
            {
                if (d.dist < player.explosion_power)
                {
                    ArrayList temp = FindEscapePath(transform.position, transform.position);
                    if (temp.Count >= 2) return true;
                }
            }
        }

        return false;
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
            var obj = Instantiate(bombPrefab, new Vector3(Mathf.RoundToInt(transform.position.x),
             bombPrefab.transform.position.y, Mathf.RoundToInt(transform.position.z)),
             bombPrefab.transform.rotation);

            player.avalibleBomb--;
            var temp = obj.GetComponent<Bomb>();
            temp.PlayerId = player.PlayerId;
            bombs.Add(temp);

            obj.GetComponent<Bomb>().explode_size = player.explosion_power;
            obj.GetComponent<Bomb>().player = player;
        }
    }
}
