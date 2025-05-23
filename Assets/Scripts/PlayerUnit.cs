using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerUnit : MonoBehaviour
{
    [Range(1, 9)]
    public int PlayerId;
    public float moveSpeed = 5f;
    public int avalibleBomb = 1; // default bomb is 1, not 2.

    public bool canKick = false;
    public int explosion_power = 2;
    public Material[] Materials;

    public bool dead = false;
    public bool respawning = false;

    public int isFrozing = 0; // 0: not freezing, 1: freezing, 2: frozen.;

    public Gameplay gameplay;

    // 从起点到终点的路径，形成一串坐标。
    public List<(int x, int y)> pathList = new List<(int x, int y)>();

    public bool isDead(){
        return dead;
    }

    public void Start()
    {
        gameplay = FindObjectOfType<Gameplay>();
        var mesh = GetComponentInChildren<SkinnedMeshRenderer>();
        mesh.material = Materials[PlayerId - 1];
    }

    private int Manhattan(Vector3 left, Vector3 right)
    {
        int lx = Mathf.RoundToInt(left.x);
        int ly = Mathf.RoundToInt(left.z);

        int rx = Mathf.RoundToInt(right.x);
        int ry = Mathf.RoundToInt(right.z);
        return Math.Abs(lx - rx) + Math.Abs(ly - ry);
    }

    private PlayerUnit GetNearestEnemy(int playerId)
    {
        PlayerUnit[] enemies = FindObjectsOfType<PlayerUnit>();

        var player = FindObjectsOfType<PlayerUnit>()
            .FirstOrDefault(u => u.PlayerId == playerId);

        int min = 100000;
        PlayerUnit playerUnit = null;

        for(int k = 0; k < enemies.Length; k++)
        {
            if(playerId != enemies[k].PlayerId && MyPlayerPrefs.IsAlive(enemies[k].PlayerId))
            {
                int dist = Manhattan(player.transform.position, enemies[k].transform.position);

                if(min > dist)
                {
                    playerUnit = enemies[k];
                    min = dist;
                }
            }
        }

        return playerUnit;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!dead && other.CompareTag("Explosion"))
        {
            dead = true;
            gameplay.KillAgent(PlayerId);

            int playerId = MyPlayerPrefs.GetPlayerId();
            var self = other.gameObject.GetComponent<DestroySelf>();

            Debug.Log($"Player {PlayerId} is dead at {transform.position} and Collider position is {other.transform.position}");

            if(!MyPlayerPrefs.IsAlive(playerId))
            {
                if (self.EnemyId != playerId && self.EnemyId > 0 && MyPlayerPrefs.IsAlive(self.EnemyId))
                    MyPlayerPrefs.SetEnemyId(self.EnemyId);
                else
                {
                    int enemyId = MyPlayerPrefs.GetEnemyId();
                    var enemyUnit = GetNearestEnemy(enemyId);

                    if (enemyUnit != null && !MyPlayerPrefs.IsAlive(enemyId))
                        MyPlayerPrefs.SetEnemyId(enemyUnit.PlayerId);
                }
            }

            Destroy(gameObject);
        }
    }
}
