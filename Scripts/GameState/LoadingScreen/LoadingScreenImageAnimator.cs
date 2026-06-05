using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingScreenImageAnimator : MonoBehaviour
{
    private Animator _anim;
    private SpriteRenderer _renderer;

    public int state;

    void Awake()
    {
        _anim = GetComponent<Animator>();
        _renderer = GetComponent<SpriteRenderer>();

        //Debug.Log("state fade in : " + (int)fadeIn + " state fadeOut: " + (int)fadeOut);
    }
    

    // Update is called once per frame
    void FixedUpdate()
    {
        if (state == _currentState) return;
        _anim.CrossFade(state, 0, 0);
        _currentState = state;
        //Debug.Log("new state: " +  _currentState);
    }

    public void FadeOut(bool beforeFirstFrame = true)
    {
	    state = fadeOut;

        if (!beforeFirstFrame) return;
        _anim.CrossFade(state, 0, 0);
        _currentState = state;
        //Debug.Log("1. Fade Out:" + _currentState);
    }

    public void FadeIn(bool beforeFirstFrame = true)
    {
        state = fadeIn;

        if (!beforeFirstFrame) return;
        _anim.CrossFade(state, 0, 0);
        _currentState = state;
        //Debug.Log("2. Fade In" + _currentState);
    }


    #region Cached Properties

    private int _currentState;
	
    private static readonly int fadeIn = Animator.StringToHash("FadeIn");
    private static readonly int fadeOut = Animator.StringToHash("FadeOut");
    #endregion
}
