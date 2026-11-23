using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
public enum NameSkill
{
    regen,
    magnit,
    minemit,
    addsprint,
    apteka,
    mina,
    shit
}
public class DroppedSkill : MonoBehaviour
{
    public PassivkiManager manager;
    public NameSkill skill; // какую активку даёт этот объект

    public GameObject regen;
    public GameObject magnit;
    public GameObject minemit;
    public GameObject addsprint;
    public GameObject apteka;
    public GameObject mina;
    public GameObject shit;

    public void Init(NameSkill newSkill)
    {
        skill = newSkill;
        UpdateVisual();
    }
    private void UpdateVisual()
    {
        regen.SetActive(false);
        magnit.SetActive(false);
        minemit.SetActive(false);
        addsprint.SetActive(false);
        apteka.SetActive(false);
        mina.SetActive(false);
        shit.SetActive(false);
        switch (skill)
        {
            case NameSkill.regen:
                regen.SetActive(true);
                break;
            case NameSkill.magnit:
                magnit.SetActive(true);
                break;
            case NameSkill.minemit:
                minemit.SetActive(true);
                break;
            case NameSkill.addsprint:
                addsprint.SetActive(true);
                break;
            case NameSkill.apteka:
                apteka.SetActive(true);
                break;
            case NameSkill.mina:
                mina.SetActive(true);
                break;
            case NameSkill.shit:
                shit.SetActive(true);
                break;
        }

    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Через switch донесём менеджеру, какую переменную +1
        switch (skill)
        {
            case NameSkill.regen:
                manager.regen++;
                break;
            case NameSkill.magnit:
                manager.magnit++;
                break;
            case NameSkill.minemit:
                manager.raketamet++;
                break;
            case NameSkill.addsprint:
                manager.sprint++;
                break;
            case NameSkill.apteka:
                manager.apteka++;
                break;
            case NameSkill.mina:
                manager.mina++;
                break;
            case NameSkill.shit:
                manager.shit++;
                break;
        }

        Destroy(gameObject); // убираем объект
    }
}
