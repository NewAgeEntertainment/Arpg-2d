using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class Sex_StartingState : SexState
{
    public Sex_StartingState(SexyTimeLogic context) : base(context) 
    {
        
    }

    //public override void Enter()
    //{
    //    context.StartCoroutine(StartSexyTimeCoroutine());
    //}

    //private IEnumerator StartSexyTimeCoroutine()
    //{
    //    yield return new WaitForSeconds(0.75f);

    //    if (context.canvasBackground != null)
    //        context.canvasBackground.SetActive(true);

    //    if (context.goToPlayerPosition)
    //        context.transform.position = GameObject.FindGameObjectWithTag("Player").transform.position + context.offsetForPosition;
    //    else
    //        context.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, Camera.main.nearClipPlane)) + context.offsetForPosition;

    //    context.ChangeState(new Sex_StrokingState(context));
    //}
}

