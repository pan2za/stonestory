using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ShowPanel : MonoBehaviour
{
    public Text round;
    private float elapsedTime;
    private bool canShow;

    public void Start()
    {
        int id = MyPlayerPrefs.GetLevel();
        if (id == 0) id = 1;
        round.text = $"第{id}回合";
        elapsedTime = 1f;
        canShow = true;
    }

    public void Update()
    {
        if (canShow)
        {
            if (elapsedTime > 0f)
                elapsedTime -= Time.deltaTime;
            else
            {
                elapsedTime = 0;
                canShow = false;
            }
        }
        else
            gameObject.SetActive(false);
    }
}
