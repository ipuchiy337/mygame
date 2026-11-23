using UnityEngine;
using UnityEngine.UI;

public class levelTimer : MonoBehaviour
{
    public Text lvlTimer;
    public float startTime = 0f; 

    private float timeLeft;
    private bool timerEnded;

   

    private void Start()
    {
        timeLeft = startTime;
        UpdateText();
    }

    private void Update()
    {
        if (timerEnded) return;

        timeLeft += Time.deltaTime;

        

        UpdateText();
    }

    private void UpdateText()
    {
        int minutes = Mathf.FloorToInt(timeLeft / 60f);
        int seconds = Mathf.FloorToInt(timeLeft % 60f);

        lvlTimer.text = $"{minutes:00}:{seconds:00}";
    }

    
}
