using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdsInitializer : MonoBehaviour
{
    public static AdsInitializer instance;
    private void Awake()
    {

        if (!instance)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Advertisements.Instance.Initialize();
    }
}
