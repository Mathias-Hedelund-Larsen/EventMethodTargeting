using UnityEngine;
using UnityEngine.Events;
using FoldergeistAssets.UnityEventMethodTargeting;
using System;

[Serializable]
public class FloatUnityEvent : UnityEvent<float> { }

public class ButtonCallMethod : MonoBehaviour
{
    [SerializeField, EventMethodTarget]
    private FloatUnityEvent _event;

    private void Awake()
    {
        _event.Invoke(2.345f);
    }

    private void OnClick()
    {
        Debug.Log("Clicked");
    }

    private void TakeFloat(float val)
    {
        Debug.Log(val);
    }
}
