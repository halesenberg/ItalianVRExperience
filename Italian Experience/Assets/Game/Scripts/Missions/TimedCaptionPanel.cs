using System.Collections;
using UnityEngine;

namespace Bellavalle.Missions
{
    /// <summary>
    /// Canvas di sottotitoli/trascrizione che compare insieme a una battuta
    /// audio e sparisce da solo dopo la durata indicata (tipicamente la
    /// durata della clip stessa).
    ///
    /// Questo script gestisce SOLO mostra/nascondi — il testo lo scrivi tu
    /// direttamente sui TMP_Text del canvas, in Inspector, non serve
    /// passarlo via codice.
    ///
    /// Setup:
    ///  1. Crea un Canvas World Space vicino al chitarrista, con dentro i
    ///     TMP_Text che vuoi (es. uno per la trascrizione italiana, uno per
    ///     il sottotitolo). Scrivi il testo direttamente su quei TMP_Text.
    ///  2. Aggiungi questo script sul Canvas. Il Canvas deve partire
    ///     DISATTIVATO in scena (Show() lo attiva lui quando serve).
    ///  3. Consigliato: aggiungi anche CanvasFollow (già nel progetto) sullo
    ///     stesso Canvas, target = Main Camera. Così il sottotitolo resta
    ///     sempre leggibile davanti al player, invece di essere ancorato
    ///     alla posizione del chitarrista (che il player potrebbe non star
    ///     guardando mentre l'audio parte).
    ///  4. Collega il riferimento a MusicianMissionManager.helpRequestCaption.
    /// </summary>
    public class TimedCaptionPanel : MonoBehaviour
    {
        [Tooltip("Usato solo se Show() viene chiamato senza una durata esplicita.")]
        [SerializeField] float defaultDuration = 4f;

        Coroutine _hideRoutine;

        /// <summary>Mostra il canvas. Se duration &lt;= 0 usa defaultDuration.</summary>
        public void Show(float duration = -1f)
        {
            gameObject.SetActive(true);

            if (_hideRoutine != null) StopCoroutine(_hideRoutine);
            _hideRoutine = StartCoroutine(HideAfter(duration > 0f ? duration : defaultDuration));
        }

        public void HidePanel()
        {
            if (_hideRoutine != null) { StopCoroutine(_hideRoutine); _hideRoutine = null; }
            gameObject.SetActive(false);
        }

        IEnumerator HideAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            HidePanel();
        }
    }
}
