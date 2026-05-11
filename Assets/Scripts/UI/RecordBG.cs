using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecordBG : MonoBehaviour
{
    public static RecordBG Instance;

    private void Awake()
    {
        Instance = this;
    }
}
