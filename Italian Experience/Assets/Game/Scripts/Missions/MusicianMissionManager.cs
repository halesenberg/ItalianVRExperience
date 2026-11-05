using UnityEngine;

namespace Bellavalle.Missions
{
    /// <summary>
    /// Orchestratore della missione "Il musicista distratto" — versione
    /// semplificata (senza albero di dialogo, per adesso).
    ///
    /// Flusso:
    ///  1. Il player raccoglie la banconota (LostBanknote / DeliverableItem,
    ///     itemId "banconota_10") e la posiziona nel cappello.
    ///  2. Sul cappello c'è un GiveToNPCZone (Is Trigger = true) che accetta
    ///     "banconota_10": quando la banconota entra, GiveToNPCZone invoca
    ///     OnItemDelivered e (se destroyOnDelivery = true) la fa sparire.
    ///  3. Questo manager riceve OnItemDelivered e riproduce la clip audio
    ///     in cui il chitarrista chiede aiuto a contare, usando un
    ///     AudioSource SEPARATO da quello della musica, così le due tracce
    ///     possono sovrapporsi senza che l'una interrompa l'altra. Insieme
    ///     alla clip, se assegnato, mostra anche il canvas con trascrizione
    ///     e sottotitolo (TimedCaptionPanel), per la durata della clip.
    ///  4. Il conteggio vero e proprio (le 10 monete da 1€) è gestito da
    ///     CoinCountingZone + CoinNumberNarrator sulla custodia: quando
    ///     tutte le monete sono state spostate, è QUEL componente a
    ///     completare la missione (non questo script).
    ///
    /// Setup:
    ///  1. Metti questo script su un GameObject in scena (es. vicino al
    ///     chitarrista, o sullo stesso oggetto di MusicianMissionManager
    ///     se esisteva già).
    ///  2. giveToNPCZone: il componente GiveToNPCZone che hai messo sul
    ///     CAPPELLO (non serve più metterlo sull'NPC).
    ///  3. voiceSource: un secondo AudioSource sul chitarrista (o vicino),
    ///     DIVERSO da quello che riproduce la musica della chitarra.
    ///  4. helpRequestClip: la clip audio "aiutami a contare le monete".
    ///  5. helpRequestCaption (opzionale): il TimedCaptionPanel del canvas
    ///     sottotitoli/trascrizione, se lo vuoi.
    /// </summary>
    public class MusicianMissionManager : MonoBehaviour
    {
        [Header("Identificazione missione")]
        [SerializeField] string missionId = "musicista_distratto";

        [Header("Consegna banconota (zona = il cappello)")]
        [SerializeField] GiveToNPCZone giveToNPCZone;
        [SerializeField] string expectedItemId = "banconota_10";

        [Header("Richiesta d'aiuto del chitarrista")]
        [Tooltip("AudioSource dedicato alla voce, separato da quello della musica.")]
        [SerializeField] AudioSource voiceSource;
        [SerializeField] AudioClip helpRequestClip;

        [Header("Sottotitoli / trascrizione (opzionale)")]
        [Tooltip("Canvas con trascrizione IT + sottotitolo, mostrato per la durata di helpRequestClip.")]
        [SerializeField] TimedCaptionPanel helpRequestCaption;

        bool _helpRequestPlayed;

        void OnEnable()
        {
            if (giveToNPCZone != null)
                giveToNPCZone.OnItemDelivered += OnItemDelivered;
        }

        void OnDisable()
        {
            if (giveToNPCZone != null)
                giveToNPCZone.OnItemDelivered -= OnItemDelivered;
        }

        void OnItemDelivered(string itemId)
        {
            if (itemId != expectedItemId) return;
            if (GameManager.Instance.IsMissionDone(missionId)) return;
            if (_helpRequestPlayed) return;

            _helpRequestPlayed = true;
            PlayHelpRequest();
        }

        void PlayHelpRequest()
        {
            if (voiceSource != null && helpRequestClip != null)
                voiceSource.PlayOneShot(helpRequestClip);

            // Mostra il testo anche senza clip audio assegnata: senza questo, se
            // helpRequestClip non è ancora stato registrato/importato, il player
            // non riceve alcun indizio dopo aver consegnato la banconota.
            if (helpRequestCaption != null)
                helpRequestCaption.Show(helpRequestClip != null ? helpRequestClip.length : -1f);
        }
    }
}