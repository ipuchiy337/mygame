using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    public bool IsPaused { get; private set; } = false;

    // События для подписки других скриптов
    public event Action OnPause;
    public event Action OnResume;

    public GameObject pauseMenu;

    public bool objPaused = false;
    public bool allStop = false;  //for xp

    [Header("типа скрипты")]
    public UpgradeGun UpgradeGun;
    public ShopUI ShopUI;
    public itemSell itemSell;

    [Header("Пауза меню кнопки")]
    public GameObject resumeBg;   // подсветка кнопки "Продолжить"
    public GameObject restartBg;  // подсветка кнопки "Рестарт"

    private int pauseMenuIndex = 0; // 0=Resume, 1=Restart
    private bool pauseMenuActive = false;


    public void Awake()
    {
        Instance = this;
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Update()
    {
        if (ShopCameraManager.Instance != null && ShopCameraManager.Instance.isOpen) return;

        if (InputManager.Instance.Pause)
        {
            if (objPaused == false) TogglePause();
            if (objPaused == true) ToggleObjPause();
        }

        if (IsPaused)
        {
            if (InputManager.Instance.UIUp)
            {
                pauseMenuIndex = (pauseMenuIndex - 1 + 2) % 2;
                UpdatePauseHighlight();
            }
            if (InputManager.Instance.UIDown)
            {
                pauseMenuIndex = (pauseMenuIndex + 1) % 2;
                UpdatePauseHighlight();
            }
            if (InputManager.Instance.UIConfirm)
            {
                if (pauseMenuIndex == 0) ResumeGame();
                else RestartScene();
            }
        }
    }
    public void RestartScene()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;
      
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void UpdatePauseHighlight()
    {
        if (resumeBg) resumeBg.SetActive(pauseMenuIndex == 0);
        if (restartBg) restartBg.SetActive(pauseMenuIndex == 1);
    }

    void ClearPauseHighlight()
    {
        if (resumeBg) resumeBg.SetActive(false);
        if (restartBg) restartBg.SetActive(false);
    }
    public void TogglePause()
    {
        if (IsPaused)
            ResumeGame();
        else
            PauseGame();
    }
    public void ToggleObjPause()
    {
        if (objPaused)
            Rezume();
        else
            Pauz();
    }
    // Поставить на паузу
    public void PauseGame()
    {
        if (IsPaused) return;
        if (allStop == false)
        {
            Time.timeScale = 0f;
            AudioListener.pause = true;
            IsPaused = true;
            OnPause?.Invoke();
            pauseMenu.SetActive(true);

            // Инициализируем индекс и подсветку
            pauseMenuIndex = 0;
            UpdatePauseHighlight();
        }
    }

    public void ResumeGame()
    {
        if (!IsPaused) return;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;
        OnResume?.Invoke();
        pauseMenu.SetActive(false);
        ClearPauseHighlight();
    }


    // пауза/старт для обж
    public void Pauz()
    {
        if(objPaused) return;

        Time.timeScale = 0f;           // останавливаем физику и все Time.deltaTime
        AudioListener.pause = true;     // останавливаем звук
       // Debug.Log("Pauz: timeScale = " + Time.timeScale + ", objPaused = " + objPaused);
        objPaused = true;
        OnPause?.Invoke();
    }
    public void Rezume()
    {
      //  Debug.Log("Rezume вызван! objPaused = " + objPaused);
        //Debug.Log(System.Environment.StackTrace); // покажет кто вызвал
        if (!objPaused) return;
 
        Time.timeScale = 1f;
        AudioListener.pause = false;
       
        objPaused = false;
        OnResume?.Invoke();


        // obj resume
   
        ShopUI.ClosePanel();

        
    }
    public void Pauzed()
    {
       

        Time.timeScale = 0f;           // останавливаем физику и все Time.deltaTime
        AudioListener.pause = true;     // останавливаем звук
        allStop = true;                                // Debug.Log("Pauz: timeScale = " + Time.timeScale + ", objPaused = " + objPaused);
       
        OnPause?.Invoke();
    }
    public void Resumed()
    {


        Time.timeScale = 1f;           // останавливаем физику и все Time.deltaTime
        AudioListener.pause = false;     // останавливаем звук
        allStop = false;                                // Debug.Log("Pauz: timeScale = " + Time.timeScale + ", objPaused = " + objPaused);

        OnPause?.Invoke();
    }


}
