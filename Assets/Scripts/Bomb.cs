using UnityEngine;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

// https://gitee.com/yuanzhi0515/playground/blob/master/pommerman/characters.py
// reference to class Bomb(object):
public class Bomb : MonoBehaviour
{
    public AudioClip explosionSound;
    public GameObject explosionPrefab;

    public LayerMask levelMask;
    private bool exploded = false;
    public int PlayerId;

    public int explode_size = 2;
    public PlayerUnit player;
    private AudioSource audioSource;

    private PlayerController playerController;

    public Vector2 position; // 显式定义position字段

    private float bombSpawnTime; // 记录炸弹释放的时间点

    public float GetBombRange()
    {
        return (float)explode_size;
    }

    public float GetBombTimeout()
    {
        //获取当前时间和释放炸弹的时间差。
        // 获取当前时间和释放炸弹的时间差
        float elapsedTime = Time.time - bombSpawnTime;
        
        // 计算剩余时间（总时间3秒减去已过去的时间）
        float remainingTime = 3f - elapsedTime;
        
        // 确保剩余时间不会小于0
        return Mathf.Max(0f, remainingTime);
    }

    public void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = MyPlayerPrefs.GetVolume();
        position = new Vector2(transform.position.x, transform.position.z); // 初始化position为当前x和z坐标
    }

    public void Start()
    {
            // 记录炸弹释放时间
        bombSpawnTime = Time.time;
        // 获取PlayerController实例
        playerController = FindObjectOfType<PlayerController>();
        Invoke("Explode", 3f);
    }

    private void Explode()
    {
        float volume = MyPlayerPrefs.GetVolume();
        AudioSource.PlayClipAtPoint(explosionSound, transform.position, volume);
        Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        StartCoroutine(CreateExplosions(Vector3.forward));
        StartCoroutine(CreateExplosions(Vector3.right));

        StartCoroutine(CreateExplosions(Vector3.back));
        StartCoroutine(CreateExplosions(Vector3.left));


        exploded = true;
        GetComponent<MeshRenderer>().enabled = false;

        transform.Find("Collider").gameObject.SetActive(false);
        Destroy(gameObject, .3f);

        // 在增加可用炸弹前检查player引用是否有效
        if (player != null)
        {
            //player.avalibleBomb++;
            player.takecare = true;
        }
        else
        {
            Debug.LogWarning("炸弹爆炸后player引用为空，无法增加可用炸弹数量");
        }
        // 通知playerController炸弹已爆炸并移除其位置
        playerController.OnBombExploded(transform.position);
        //reveal bonus in the board after bombing
        MyCustomMap.ClearBit(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z), PommermanItem.Bomb);

    }



    public void OnTriggerEnter(Collider other)
    {
        if (!exploded && other.CompareTag("Explosion"))
        {
            CancelInvoke("Explode");
            Explode();
        }
    }

    private IEnumerator CreateExplosions(Vector3 direction)
    {
        var list = new List<Vector3>();

        for (int i = 1; i < explode_size; i++)
        {
            RaycastHit hit;
            Physics.Raycast(transform.position + new Vector3(0, .5f, 0), direction, out hit, i, levelMask);

            if (!hit.collider) list.Add(transform.position + (i * direction));
            else
            {
                if (hit.collider.CompareTag("Breakable"))
                {
                    hit.collider.GetComponent<Brick>().Collide = true;
                    hit.collider.gameObject.SetActive(false);
                    // 更新地图中的数据
                    MyCustomMap.RevealBonus(transform.position + (i * direction));
                }
                else if (hit.collider.CompareTag("Player") || hit.collider.CompareTag("PowerUp") || hit.collider.CompareTag("Bomb"))
                {
                    list.Add(transform.position + (i * direction));
                    //FIXME:改为passage，表示被清除了。
                    continue;
                }

                break;
            }
        }

        foreach (Vector3 position in list)
        {
            var obj = Instantiate(explosionPrefab, position, explosionPrefab.transform.rotation);
            var script = obj.GetComponent<DestroySelf>();
            script.EnemyId = PlayerId;
            yield return new WaitForSeconds(.05f);
        }
    }
}
