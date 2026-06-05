using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIFadeIn : MonoBehaviour
{
    private void Awake()
    {
        GameSceneManager.instance.LoadingScreenEnd();
    }
}
