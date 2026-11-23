using UnityEngine;
using UnityEngine.SceneManagement;


public class Death : MonoBehaviour
{
    public GameObject Panel;
    public GameObject Player;

   
    void Update()
    {
        if (Player == null)
        {
            Cursor.visible = true;
            Panel.SetActive(true);
        }
    }
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
