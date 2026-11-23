using UnityEngine;

public class OpenStats : MonoBehaviour
{
    public GameObject UpStatspanel;


    public void Open()
    {
        UpStatspanel.SetActive(true);
    }
    public void Close()
    {
        UpStatspanel.SetActive(false);
    }
}
