using UnityEngine;
using System.Collections.Generic;

public class EnvironmentManager : MonoBehaviour
{
    [Header("Riferimenti Scenari")]
    public GameObject scenarioDefault;
    public GameObject scenarioDormire;
    public GameObject scenarioSciare;
    public GameObject scenarioNuotare;
    public GameObject scenarioAutostop;
    public GameObject scenarioSalutare;

    private GameObject scenarioAttuale;

    void Start()
    {
        // All'inizio, attiviamo solo lo scenario di base
        DisattivaTutti();
        scenarioDefault.SetActive(true);
        scenarioAttuale = scenarioDefault;
    }

    public void CambiaScenario(string nomeVerbo)
    {
        // Spegniamo quello che c'era prima
        if (scenarioAttuale != null) scenarioAttuale.SetActive(false);

        // Accendiamo quello nuovo in base alla parola
        switch (nomeVerbo.ToLower())
        {
            case "dormire":
                scenarioAttuale = scenarioDormire;
                break;
            case "sciare":
                scenarioAttuale = scenarioSciare;
                break;
            case "nuotare":
                scenarioAttuale = scenarioNuotare;
                break;
            case "salutare":
                scenarioAttuale = scenarioSalutare;
                break;
            case "autostop":
                scenarioAttuale = scenarioAutostop;
                break;
            default:
                scenarioAttuale = scenarioDefault;
                break;
        }

        scenarioAttuale.SetActive(true);
        Debug.Log("Mondo cambiato in: " + nomeVerbo);
    }

    private void DisattivaTutti()
    {
        scenarioDefault.SetActive(false);
        scenarioDormire.SetActive(false);
        scenarioSciare.SetActive(false);
        scenarioNuotare.SetActive(false);
        scenarioSalutare.SetActive(false);
        scenarioAutostop.SetActive(false);
    }
}