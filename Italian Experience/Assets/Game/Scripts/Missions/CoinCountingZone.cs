using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Bellavalle.Missions
{
    /// <summary>
    /// Zona di conteggio nella custodia della chitarra.
    ///
    /// Ogni moneta (tag "Coin") che entra nel trigger viene contata UNA SOLA
    /// VOLTA, in ordine di arrivo (1, 2, 3... fino a totalCoins). A differenza
    /// di una CountingZone "classica", qui il conteggio NON diminuisce mai:
    /// una volta contata, una moneta resta contata anche se rimbalza o si
    /// sposta leggermente dentro alla custodia. Questo evita che la fisica in
    /// VR (piccoli urti, jitter) faccia "saltare" il numero avanti e indietro.
    ///
    /// Setup:
    ///  1. Metti questo script sullo stesso GameObject del Box Collider che
    ///     hai già creato dentro la custodia (quello pensato per accogliere
    ///     le monete).
    ///  2. Su quel Box Collider spunta "Is Trigger" = true (è la causa più
    ///     comune per cui "non succede niente": se non è un trigger,
    ///     OnTriggerEnter non parte mai).
    ///  3. Ogni moneta deve avere: Tag = "Coin" (creala in Edit > Project
    ///     Settings > Tags and Layers se non esiste ancora), un Collider
    ///     (va bene anche piccolo) e un Rigidbody. Serve ENTRAMBI perché
    ///     Unity generi eventi trigger: basta che uno dei due oggetti
    ///     coinvolti (moneta o custodia) abbia un Rigidbody — in questo
    ///     caso lo mettiamo sulla moneta, che tra l'altro ti serve comunque
    ///     per l'XR Grab Interactable.
    ///  4. Collega gli eventi qui sotto (OnCountChanged / OnAllCoinsCounted)
    ///     a CoinNumberNarrator nell'Inspector.
    /// </summary>
    public class CoinCountingZone : MonoBehaviour
    {
        [Header("Impostazioni")]
        [SerializeField] private string coinTag = "Coin";
        [SerializeField] private int totalCoins = 10;

        [Header("Eventi")]
        [Tooltip("Invocato ogni volta che una NUOVA moneta viene contata. Passa il totale aggiornato (1, 2, 3...).")]
        public UnityEvent<int> OnCountChanged;

        [Tooltip("Invocato una sola volta, quando si raggiunge totalCoins.")]
        public UnityEvent OnAllCoinsCounted;

        readonly HashSet<Collider> _counted = new HashSet<Collider>();
        int _currentCount;
        bool _completed;

        void OnTriggerEnter(Collider other)
        {
            if (_completed) return;
            if (!other.CompareTag(coinTag)) return;
            if (_counted.Contains(other)) return;

            _counted.Add(other);
            _currentCount++;
            OnCountChanged?.Invoke(_currentCount);

            if (_currentCount >= totalCoins)
            {
                _completed = true;
                OnAllCoinsCounted?.Invoke();
            }
        }

        public int GetCount() => _currentCount;

        /// <summary>Utile se vuoi permettere di rifare la missione (debug o reset di scena).</summary>
        public void ResetCount()
        {
            _counted.Clear();
            _currentCount = 0;
            _completed = false;
        }
    }
}
