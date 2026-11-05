using UnityEngine;

public class PulsanteFinestre : MonoBehaviour
{
    public Transform vetroFinestra;

    public Vector3 posizioneChiusa;
    public Vector3 posizioneAperta;
    public float velocitaMovimento = 5f;

    private AudioSource suonoVento;
    private bool finestraAperta = false;
    public void Start()
    {
        suonoVento = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 destinazione = finestraAperta ? posizioneAperta : posizioneChiusa;
        vetroFinestra.localPosition = Vector3.Lerp(vetroFinestra.localPosition, destinazione, velocitaMovimento);
    }

    public void AzionaFinestra()
    {
        finestraAperta = !finestraAperta;
        if (finestraAperta)
        {
            suonoVento.Play();
        }
        else
        {
            suonoVento.Stop();
        }
    }
}
