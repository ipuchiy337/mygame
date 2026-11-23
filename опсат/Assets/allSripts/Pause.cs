using UnityEngine;

public class Pause : MonoBehaviour
{
    public GameObject pause;
    bool isActiv;

    private void Start()
    {
        pause.SetActive(false);
    }

    void Update()
    {
       
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                PauseManager.Instance.PauseGame();
                 Tog();
            }

    }
    public void Tog()
    {
        isActiv = !isActiv;           // меняем состояние
        pause.SetActive(isActiv); 
        // активируем/деактивируем панель
    }
     


}