using HephaestusForge.UnityEventMethodTargeting;
using UnityEngine;
using UnityEngine.Events;

public class Demo : MonoBehaviour
{
    [SerializeField, EventMethodTarget]
    private UnityEvent _event;

    private void TakeInt(int val)
    {
        Debug.Log(val);
    }

    private void OnClick()
    {
        Debug.Log("Clicked");
    }

    private void TakeFloat(float val)
    {
        Debug.Log(val);
    }
    private void TakeString(string val)
    {
        Test(2);
        Debug.Log(val);
    }

    private void Test<T>(T val) where T : new()
    {

    }
}
