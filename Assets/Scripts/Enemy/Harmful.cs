using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Harmful : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            //全局播送：玩家死了！   或者引用玩家，然后调用PlayerDeath的方法
        }
    }
}
