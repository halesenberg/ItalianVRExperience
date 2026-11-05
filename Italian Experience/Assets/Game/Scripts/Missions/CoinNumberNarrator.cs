using UnityEngine;
using TMPro;

namespace Bellavalle.Missions
{
    /// <summary>
    /// Ascolta CoinCountingZone e, ad ogni moneta contata:
    ///  - riproduce la clip audio del numero corrispondente (1..10)
    ///  - stampa il numero a schermo su un TMP_Text
    /// Quando tutte le monete sono state contate, completa la missione.
    ///
    /// Setup:
    ///  1. Metti questo script su un GameObject in scena (va benissimo lo
    ///     stesso oggetto della custodia/CoinCountingZone).
    ///  2. audioSource: un AudioSource dedicato (Spatial Blend 3D consigliato,
    ///     così il suono del numero viene "dalla custodia" e non copre in
    ///     modo innaturale la musica/voce del chitarrista).
    ///  3. numberClips: trascina le 10 clip nell'ordine 1 → 10
    ///     (indice 0 = clip del numero "uno" ... indice 9 = clip "dieci").
    ///  4. countText: un TMP_Text su un Canvas World Space vicino alla
    ///     custodia, che mostra solo la cifra corrente.
    ///  5. Nell'Inspector di CoinCountingZone:
    ///       On Count Changed (Int32)  -> questo oggetto -> OnCoinCounted(int)
    ///       On All Coins Counted ()   -> questo oggetto -> OnAllCoinsCounted()
    /// </summary>
    public class CoinNumberNarrator : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] AudioSource audioSource;
        [Tooltip("10 clip in ordine: indice 0 = \"uno\" ... indice 9 = \"dieci\"")]
        [SerializeField] AudioClip[] numberClips;

        [Header("UI")]
        [SerializeField] TMP_Text countText;

        [Header("Missione")]
        [SerializeField] string missionId = "musicista_distratto";
        [SerializeField] bool completeMissionOnFinish = true;

        /// <summary>Collegato a CoinCountingZone.OnCountChanged(int).</summary>
        public void OnCoinCounted(int count)
        {
            if (countText != null)
                countText.text = count.ToString();

            int index = count - 1;
            if (audioSource != null && numberClips != null &&
                index >= 0 && index < numberClips.Length && numberClips[index] != null)
            {
                audioSource.PlayOneShot(numberClips[index]);
            }
        }

        /// <summary>Collegato a CoinCountingZone.OnAllCoinsCounted().</summary>
        public void OnAllCoinsCounted()
        {
            if (!completeMissionOnFinish) return;
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.IsMissionDone(missionId)) return;

            GameManager.Instance.CompleteMission(missionId);
            GameManager.Instance.NpcRemember(GameManager.NPC.Chitarrista, "il_player_lo_ha_aiutato_a_contare");
        }
    }
}
