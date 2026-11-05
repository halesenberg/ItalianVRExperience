using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Bellavalle.Core;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Bellavalle.Missions
{
    /// <summary>
    /// La banconota da 10€ persa dal chitarrista, sul terreno in 02_Quartiere.
    ///
    /// Comportamento:
    ///  - Quando il player ENTRA nel raggio del SphereCollider (trigger),
    ///    la banconota si illumina
    ///  - La PRIMA volta che si illumina, fa apparire il pannello missione
    ///  - E' raccoglibile (XRGrabInteractable) per essere lanciata al chitarrista
    ///
    /// Setup:
    ///  1. Aggiungi questo script all'oggetto banconota
    ///  2. Aggiungi un secondo Collider (SphereCollider, Is Trigger = true,
    ///     raggio ampio es. 1.5m) — SEPARATO dal collider fisico che serve
    ///     per afferrarla. Lo SphereCollider serve solo a rilevare il player.
    ///  3. Assegna missionId (es. "musicista_distratto")
    ///  4. Assegna missionPanel (il canvas del pannello missione in scena)
    ///  5. Assegna highlightRenderer + normalMaterial/highlightMaterial
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class LostBanknote : MonoBehaviour
    {
        [Header("Identificazione missione")]
        [SerializeField] string missionId = "musicista_distratto";

        [Header("Pannello missione (mostrato una sola volta)")]
        [SerializeField] GameObject missionPanel;

        [Header("Evidenziazione (prossimita')")]
        [SerializeField] Renderer highlightRenderer;
        [SerializeField] Material normalMaterial;
        [SerializeField] Material highlightMaterial;

        bool _missionPanelShown;
        bool _playerInRange;

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            _playerInRange = true;
            SetHighlight(true);

            if (!_missionPanelShown && !GameManager.Instance.IsMissionDone(missionId))
            {
                _missionPanelShown = true;
                ShowMissionPanel();
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            _playerInRange = false;
            SetHighlight(false);
        }

        void SetHighlight(bool highlighted)
        {
            if (highlightRenderer == null) return;
            highlightRenderer.material = highlighted ? highlightMaterial : normalMaterial;
        }

        void ShowMissionPanel()
        {
            if (missionPanel == null) return;
            missionPanel.SetActive(true);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            var sphereCollider = GetComponent<SphereCollider>();
            if (sphereCollider != null)
                Gizmos.DrawWireSphere(transform.position + sphereCollider.center, sphereCollider.radius);
        }
    }
}