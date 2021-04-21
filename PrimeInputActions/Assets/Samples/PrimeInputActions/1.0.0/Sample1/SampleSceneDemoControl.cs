using System;
using System.Collections;
using System.Collections.Generic;
using PrimeInputActions;
using UnityEngine;
using UniRx;

public class SampleSceneDemoControl : MonoBehaviour
{
    public GameObject dummyDialog;
    
    private void Start()
    {
        PrepareConflict1();
        PrepareConflict2();
    }

    private void PrepareConflict1()
    {
        // Conflict: Different behaviour is bound to the same InputAction
        // -> Example: "close dialog" vs "exit application" (both triggered with Escape key)
        // -> Solution: Set a priority to define which InputAction is handled first and cancel the other as needed.
        InputManager.GetInputAction("ui/cancel").PerformedAsObservable(10 /* higher priority is handled first */)
            .Where(_ => dummyDialog.activeSelf)
            .Subscribe(callbackContext =>
            {
                CloseDummyDialog();
                Debug.Log("Canceling further notification of event");
                InputManager.GetInputAction("ui/cancel").CancelNotifyForThisFrame();
            });
        
        // Note that you can use generated constants to reference InputActions.
        InputManager.GetInputAction(R.InputActions.ui_cancel).PerformedAsObservable(0 /* lower priority is handled later */)
            .Subscribe(callbackContext => QuitOrStopPlayMode());
    }
    
    private void PrepareConflict2()
    {
        // Conflict: one InputAction is a part of another
        // -> Example: "save" vs "save as" (Ctrl+S vs Shift+Ctrl+S)
        // -> Solution: Check that the more complex InputAction is not triggered when listening for the simpler InputAction
        InputManager.GetInputAction(R.InputActions.shortcuts_save).PerformedAsObservable()
            .Where(_ => InputManager.GetInputAction(R.InputActions.shortcuts_saveAs).ReadValue<float>() == 0)
            .Subscribe(callbackContext => Debug.Log("dummy 'save' triggered"));
        
        // Note that this is an InputAction of type "PassThrough".
        // For such an InputAction, the performed-event triggers also even though ReadValue<float>() == 0.
        // Thus, ReadValue<float>() == 1 is checked here explicitly in the Where-clause.
        InputManager.GetInputAction(R.InputActions.shortcuts_saveAs).PerformedAsObservable()
            .Where(_ => InputManager.GetInputAction(R.InputActions.shortcuts_saveAs).ReadValue<float>() == 1)
            .Subscribe(callbackContext => Debug.Log("dummy 'save as' triggered"));
    }
    
    private void CloseDummyDialog()
    {
        Debug.Log("Closing dummy dialog");
        dummyDialog.SetActive(false);
    }
    
    private static void QuitOrStopPlayMode()
    {
#if UNITY_EDITOR
        Debug.Log("Stopping play-mode");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Debug.Log("Quitting application");
        Application.Quit();
#endif
    }
}
