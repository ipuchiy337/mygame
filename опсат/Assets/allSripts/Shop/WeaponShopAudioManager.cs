using UnityEngine;

public class WeaponShopAudioManager : MonoBehaviour
{

    [Header("Close&Buy")]
    public AudioSource BoomObiem;
    public AudioSource Tidish;
    public AudioSource Babah;
    public AudioSource Sirena;
    public AudioSource Bip;
    public AudioSource Close;
    public AudioSource BrokenGlass;
    public AudioSource Punch;
    public AudioSource prosoBah;
    public AudioSource megaBah;
    public AudioSource kick;
    public AudioSource tovar;
    public AudioSource bipGreen;
    [Header("idle")]
    public AudioSource hrezzer;
    public AudioSource lightBroken;
    public void BoomObiemm()
    {
        BoomObiem.Play();
    }

    public void Tidishh()
    {
        Tidish.Play();
    }
    public void Babahh()
    {
        Babah.Play();
    }
    public void Sirenaa()
    {
        Sirena.Play();
    }
    public void SirenaaStop()
    {
        Sirena.Stop();
    }
    public void Bipp()
    {
        Bip.Play();
    }
    public void BippG()
    {
        bipGreen.Play();
    }
    public void Closee()
    {
        Close.Play();
    }
    public void BrokenGlazz()
    {
        BrokenGlass.Play();
    }
    public void Punchh()
    {
        Punch.Play();
    }
    public void ProstBah()
    {
        prosoBah.Play();
    }
    public void megabux()
    {
        megaBah.Play();
    }
    public void kikc()
    {
        kick.Play();
    }
    public void ShelestTovara()
    {
        tovar.Play();
    }





    public void frezerzs()
    {
        hrezzer.Play();
    }
    public void lightB()
    {
        lightBroken.Play();
    }
}
