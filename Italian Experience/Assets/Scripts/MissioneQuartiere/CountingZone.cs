using UnityEngine;
using UnityEngine.Events;

namespace Bellavalle.Missions
{
    public class CountingZone : MonoBehaviour
    {
        [Header("Impostazioni")]
        [SerializeField] private string coinTag = "Coin";

        [Header("Eventi")]
        public UnityEvent<int> OnCountChanged; // Invia il nuovo totale (es. 1, 2, 3...)

        private int _currentCoinCount = 0;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(coinTag))
            {
                _currentCoinCount++;
                OnCountChanged?.Invoke(_currentCoinCount);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(coinTag))
            {
                _currentCoinCount = Mathf.Max(0, _currentCoinCount - 1);
                OnCountChanged?.Invoke(_currentCoinCount);
            }
        }

        public int GetCount() => _currentCoinCount;
    }
}