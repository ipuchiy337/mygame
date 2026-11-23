using UnityEngine;
using UnityEngine.UI;

public class Kills : MonoBehaviour
{
    public int _killed;

    public Text killz;

    private void Update()
    {
        killz.text = _killed.ToString();
    }
}
