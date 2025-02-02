using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    public bool canDropBombs = true;
    public bool canMove = true;
    public PlayerUnit player;

    public GameObject bombPrefab;
    private Rigidbody rigidBody;
    private Transform myTransform;

    private DynamicJoystick joystick;
    private ActionJoystick action;

    private Animator animator;
    //https://gitee.com/yuanzhi0515/playground/blob/master/pommerman/forward_model.py
    // The starting bombs.
    private List<Bomb> bombs;

    // 存储所有炸弹位置的列表
    private List<Vector3> bombPositions = new List<Vector3>();

    public void Start()
    {
        player = GetComponent<PlayerUnit>();
        rigidBody = GetComponent<Rigidbody>();
        myTransform = transform;

        animator = myTransform.Find("PlayerModel").GetComponent<Animator>();
        bombs = FindObjectOfType<LoadMap>().BombList;
        action = FindObjectOfType<ActionJoystick>();
        joystick = FindObjectOfType<DynamicJoystick>();

        bombPositions = new List<Vector3>();
    }

    private bool GetKey(Joypad key)
    {
        if (joystick != null) return joystick.GetKey(key);
        else return false;
    }

    private bool GetActionKey()
    {
        if (action != null) return action.GetActionKey();
        else return false;
    }

    public void Update()
    {
        UpdatePlayer();
    }

    private void UpdatePlayer()
    {
        animator.SetBool("Walking", false);
        if (!canMove) return;
        UpdateMovement();
    }

    /// <summary>
    /// Updates Player's movement and facing rotation using the arrow keys and drops bombs.
    /// </summary>
    private void UpdateMovement()
    {
        if (Input.GetKey (KeyCode.UpArrow) || GetKey(Joypad.UpArrow))
        {
            rigidBody.velocity = new Vector3(rigidBody.velocity.x, rigidBody.velocity.y, player.moveSpeed);
            myTransform.rotation = Quaternion.Euler (0, 0, 0);
            animator.SetBool ("Walking", true);
        }

        if (Input.GetKey(KeyCode.LeftArrow) || GetKey(Joypad.LeftArrow))
        {
            rigidBody.velocity = new Vector3 (-player.moveSpeed, rigidBody.velocity.y, rigidBody.velocity.z);
            myTransform.rotation = Quaternion.Euler (0, 270, 0);
            animator.SetBool ("Walking", true);
        }

        if (Input.GetKey(KeyCode.DownArrow) || GetKey(Joypad.DownArrow))
        {
            rigidBody.velocity = new Vector3(rigidBody.velocity.x, rigidBody.velocity.y, -player.moveSpeed);
            myTransform.rotation = Quaternion.Euler(0, 180, 0);
            animator.SetBool("Walking", true);
        }

        if (Input.GetKey (KeyCode.RightArrow) || GetKey(Joypad.RightArrow))
        {
            rigidBody.velocity = new Vector3(player.moveSpeed, rigidBody.velocity.y, rigidBody.velocity.z);
            myTransform.rotation = Quaternion.Euler(0, 90, 0);
            animator.SetBool("Walking", true);
        }

        if (canDropBombs && (Input.GetKeyDown(KeyCode.Space) || GetActionKey()))
            DropBomb();
    }

    /// <summary>
    /// Drops a bomb beneath the player.
    /// </summary>
    public void DropBomb()
    {
        if (player.avalibleBomb > 0 && bombPrefab)
        {
            var newPosition = new Vector2(Mathf.RoundToInt(myTransform.position.x), Mathf.RoundToInt(myTransform.position.z));

            // 检查新位置是否已经有炸弹
            bool isPositionOccupied = IsPositionOccupied(newPosition);

            if (!isPositionOccupied)
            {
                player.avalibleBomb--;

                var obj = Instantiate (bombPrefab,
                    new Vector3 (Mathf.RoundToInt(myTransform.position.x), bombPrefab.transform.position.y, Mathf.RoundToInt(myTransform.position.z)),
                    bombPrefab.transform.rotation);

                obj.GetComponent<Bomb>().explode_size = player.explosion_power;
                obj.GetComponent<Bomb>().player = player;

                var bomb = obj.GetComponent<Bomb>();
                bomb.PlayerId = player.PlayerId;
                bomb.position = newPosition;
                bombs.Add(bomb);

                if(player.canKick) obj.GetComponent<Rigidbody>().isKinematic = false;
            }
        }
    }
    // 处理炸弹爆炸后的清理工作
    public void OnBombExploded(Vector3 bombPosition)
    {
        // 找到并移除炸弹的位置
        var newPosition = new Vector2(Mathf.RoundToInt(myTransform.position.x), Mathf.RoundToInt(myTransform.position.z));
        RemoveBombAtPosition(newPosition);
    }    
    
    // 检查新位置是否已经有炸弹的方法
    public bool IsPositionOccupied(Vector2 newPosition)
    {
        foreach (var bomb in bombs)
        {
            if (bomb.position == newPosition)
            {
                return true; // 如果找到相同的位置，则说明位置被占用了
            }
        }
        return false; // 如果遍历完所有炸弹都没有发现相同的位置，则说明位置未被占用
    }

    public void RemoveBombAtPosition(Vector2 position)
    {
        // 从最后一个元素开始向前遍历
        for (int i = bombs.Count - 1; i >= 0; i--)
        {
            if (bombs[i].position == position)
            {
                // 移除炸弹
                bombs.RemoveAt(i);
                break; // 如果只需要移除一个炸弹，则在此处跳出循环
            }
        }
    }

}
