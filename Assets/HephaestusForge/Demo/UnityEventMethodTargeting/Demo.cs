using HephaestusForge.UnityEventMethodTargeting;
using UnityEngine;
using UnityEngine.Events;

public class Demo : MonoBehaviour
{
    [SerializeField]
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
        Debug.Log(val);
    }

    private void TakeBool(bool val) 
    {
        Debug.Log(val);
    }

    public void TakeObject(ScriptableObject obj)
    {
        Debug.Log(obj);
    }
}
