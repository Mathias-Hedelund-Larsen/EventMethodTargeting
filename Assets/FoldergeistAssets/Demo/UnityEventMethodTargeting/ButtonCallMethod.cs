using UnityEngine;

public class ButtonCallMethod : MonoBehaviour
{
    private void OnClick()
    {
        Debug.Log("Clicked");
    }

    private void TakeFloat(float val)
    {
        Debug.Log(val);
    }
}
