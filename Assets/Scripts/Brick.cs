using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Brick : MonoBehaviour
{
    public GameObject[] list;
    public bool Collide;

    public void OnBecameInvisible()
    {
        if (Collide)
        {
            var obj = Instantiate(list[3], transform.position, Quaternion.identity);
            obj.GetComponent<BoxCollider>().isTrigger = false;

            int item = MyCustomMap.GetBonusType(transform.position);

            if (item >= 0)
            {
                Instantiate(list[item - 1], transform.position, Quaternion.identity);
            }else{
                //board from brick to passage.
                MyCustomMap.SetBoard(transform.position, PommermanItem.Passage);
            }


            Destroy(gameObject);
        }
    }
}
