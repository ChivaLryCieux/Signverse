using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillUI : MonoBehaviour
{
    public GameObject highLight;
    public float pickScale = 1.1f;


    void OnMouseExit()
    {
        highLight.SetActive(false);
        gameObject.transform.localScale = new Vector3(1f,1f,1f);
    }
    void OnEnable()
    {
        highLight.SetActive(false);
    }
    void OnMouseEnter()
    {
        highLight.SetActive(true);
    }
    void OnMouseDown()
    {
        gameObject.transform.localScale *= pickScale;
    }
}
