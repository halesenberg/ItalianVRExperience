using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Bellavalle.Core;

namespace Bellavalle.UI
{
    /// <summary>
    /// Canvas World Space per l'inventario delle frasi apprese (lo "zaino").
    /// Si apre/chiude con il tasto A del controller. Si riposiziona
    /// davanti al player ogni volta che viene aperto.
    ///
    /// Gerarchia consigliata:
    ///   InventoryCanvas (questo script + Canvas + InventoryXRInput — SEMPRE attivo)
    ///   └── Panel (assegnato a canvasRoot — questo si attiva/disattiva)
    ///        ├── TabButtons (Domande / Risposte / Vocabolario)
    ///        ├── ScrollView
    ///        │    └── Content  <- qui vengono instanziate le righe (PhraseRowPrefab)
    ///        └── EmptyStateText ("Non hai ancora imparato nulla qui")
    ///
    /// Setup:
    ///  1. Metti questo script sul GameObject InventoryCanvas (padre)
    ///  2. canvasRoot DEVE essere un figlio (es. "Panel"), NON lo stesso
    ///     GameObject di questo script — altrimenti SetActive(false) spegne
    ///     anche lo script che ascolta l'input.
    ///  3. Assegna content, phraseRowPrefab, emptyStateText (dentro Panel)
    ///  4. Assegna headTransform = Main Camera del player (per il reposizionamento)
    ///  5. Collega i 3 bottoni delle tab a ShowCategoryByIndex(int)
    ///  6. ToggleInventory() è già collegato da InventoryXRInput al tasto A
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] GameObject canvasRoot;     // FIGLIO con la grafica — non questo GameObject

        [Header("Posizionamento davanti al player")]
        [SerializeField] Transform headTransform;    // Main Camera / XR Camera
        [SerializeField] float distanceFromHead = 0.6f;
        [SerializeField] float heightOffset = -0.1f;

        [Header("Contenuto")]
        [SerializeField] Transform content;          // dentro la ScrollView
        [SerializeField] GameObject phraseRowPrefab; // prefab di una riga (vedi PhraseRowUI)
        [SerializeField] GameObject emptyStateText;   // testo "vuoto" se nessuna frase

        [Header("Tab attiva (per evidenziare il bottone)")]
        [SerializeField] PhraseCategory defaultCategory = PhraseCategory.Vocabolario;

        readonly List<GameObject> _spawnedRows = new();
        bool _isOpen;

        public bool IsOpen => _isOpen;
        public PhraseCategory CurrentCategory { get; private set; }

        void Start()
        {
            if (canvasRoot != null) canvasRoot.SetActive(false);
            _isOpen = false;
        }

        // ── API pubblica — collega al tasto A del controller ──────────
        public void ToggleInventory()
        {
            if (_isOpen) CloseInventory();
            else OpenInventory();
        }

        public void OpenInventory()
        {
            _isOpen = true;
            PositionInFrontOfPlayer();
            if (canvasRoot != null) canvasRoot.SetActive(true);
            ShowCategory(defaultCategory);
        }

        public void CloseInventory()
        {
            _isOpen = false;
            if (canvasRoot != null) canvasRoot.SetActive(false);
        }

        // ── Posizionamento ────────────────────────────────────────────
        void PositionInFrontOfPlayer()
        {
            if (headTransform == null || canvasRoot == null) return;

            Vector3 flatForward = headTransform.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < 0.0001f) flatForward = Vector3.forward;
            flatForward.Normalize();

            Vector3 targetPos = headTransform.position + flatForward * distanceFromHead;
            targetPos.y += heightOffset;

            canvasRoot.transform.position = targetPos;
            canvasRoot.transform.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
        }

        // ── Cambio categoria — collega ai 3 bottoni tab ─────────────────
        public void ShowCategory(PhraseCategory category)
        {
            CurrentCategory = category;
            ClearRows();

            if (GameManager.Instance == null) return;
            var phrases = GameManager.Instance.State.GetPhrasesByCategory(category);

            if (emptyStateText != null)
                emptyStateText.SetActive(phrases.Count == 0);

            foreach (var phrase in phrases)
                SpawnRow(phrase);
        }

        // Overload comodo per collegare i bottoni Unity Event con un int (0,1,2)
        public void ShowCategoryByIndex(int index)
        {
            ShowCategory((PhraseCategory)index);
        }

        // ── Spawn riga ────────────────────────────────────────────────────
        void SpawnRow(LearnedPhrase phrase)
        {
            if (phraseRowPrefab == null || content == null) return;

            var go = Instantiate(phraseRowPrefab, content);
            _spawnedRows.Add(go);

            var row = go.GetComponent<PhraseRowUI>();
            if (row != null)
                row.Setup(phrase);
        }

        void ClearRows()
        {
            foreach (var row in _spawnedRows)
                if (row != null) Destroy(row);
            _spawnedRows.Clear();
        }
    }
}