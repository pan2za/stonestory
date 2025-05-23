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
        // 记录炸弹释放时间
        bombSpawnTime = Time.time;
    }

    public void Start()
    {
        // 获取PlayerController实例
        playerController = FindObjectOfType<PlayerController>();
        Invoke("Explode", 3f);

    }
    Bounds CalculateTotalBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }
    private void Explode()
    {
        float volume = MyPlayerPrefs.GetVolume();
        AudioSource.PlayClipAtPoint(explosionSound, transform.position, volume);
        GameObject tempExplosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Debug.Log($"explosion at {transform.position}");
        // 计算所有Renderer的边界
        Bounds combinedBounds = CalculateTotalBounds(tempExplosion);

        //Debug.Log($"爆炸整体大小: {combinedBounds.size}");
        //Debug.Log($"爆炸最大范围: {Mathf.Max(combinedBounds.size.x, combinedBounds.size.y, combinedBounds.size.z)}");

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
            //StartCoroutine(WaitAndResetTakecare());
        }
        else
        {
            Debug.LogWarning("炸弹爆炸后player引用为空，无法增加可用炸弹数量");
        }
    }

    private IEnumerator WaitAndResetTakecare()
    {
        player.isWaiting = true; // 标记为正在等待

        yield return new WaitForSeconds(3f); // 等待0.5秒

        if (player != null) // 确保player引用仍然有效
        {
            Debug.Log($"user {player.PlayerId} add bomb");
            player.avalibleBomb++;
        }

        player.isWaiting = false; // 标记等待结束
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
            Debug.Log($"create explosion at {position}");
            var script = obj.GetComponent<DestroySelf>();
            script.EnemyId = PlayerId;
            yield return new WaitForSeconds(.05f);
        }
    }
    private void OnDestroy() {
        player.avalibleBomb++;
        // 通知playerController炸弹已爆炸并移除其位置
        playerController.OnBombExploded(transform.position);
        //reveal bonus in the board after bombing
        MyCustomMap.ClearBit(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z), PommermanItem.Bomb);
    }
}
