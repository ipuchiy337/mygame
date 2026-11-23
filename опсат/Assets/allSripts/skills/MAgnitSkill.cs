using UnityEngine;



public class MAgnitSkill : MonoBehaviour
{
    public UnPasivkiManager pasiv;
    public GameObject _magnit;

    public void AddMagnit()
    {
        pasiv.plusMagnit++;
    }
    void Update()
    {
        if (_magnit == null) return;

        // магнит включён пока пассивка прокачана
        _magnit.SetActive(pasiv.plusMagnit > 0);
    }
}