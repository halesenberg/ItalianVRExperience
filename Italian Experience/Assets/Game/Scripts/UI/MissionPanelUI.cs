using System.Collections;
using UnityEngine;
using TMPro;

namespace Bellavalle.UI
{
    /// <summary>
    /// Pannello missione semplice — appare con un testo, poi sparisce da solo
    /// dopo qualche secondo (o quando richiamato HidePanel()).
    ///
    /// Setup:
    ///  1. Crea un Canvas World Space vicino alla banconota/cappello
    ///  2. Aggiungi un TMP_Text per il testo missione
    ///  3. Aggiungi questo script sul Canvas, assegna missionText
    ///  4. Collega LostBanknote.missionPanel a questo GameObject
    ///  5. Il Canvas parte DISATTIVATO in scena (LostBanknote lo attiva lui)
    /// </summary>
    public class MissionPanelUI : MonoBehaviour
    {
        [SerializeField] TMP_Text missionText;
        [SerializeField] float autoHideAfterSeconds = 6f;

        [TextArea]
        [SerializeField]
        string defaultMessage =
            "Seems like the musician lost something...";

        void OnEnable()
        {
            if (missionText != null)
                missionText.text = defaultMessage;

            if (autoHideAfterSeconds > 0)
                StartCoroutine(AutoHide());
        }

        IEnumerator AutoHide()
        {
            yield return new WaitForSeconds(autoHideAfterSeconds);
            HidePanel();
        }

        public void HidePanel()
        {
            gameObject.SetActive(false);
        }
    }
}