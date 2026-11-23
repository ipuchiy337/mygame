using UnityEngine;
using UnityEngine.UI;

public class endMenu : MonoBehaviour
{
    public GameObject endPanel;

    public GameObject player;

    private void Update()
    {
        if (player == null)
        {
            endPanel.SetActive(true);
        }
    }
}

