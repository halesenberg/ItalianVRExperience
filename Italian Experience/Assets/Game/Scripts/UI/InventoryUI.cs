using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Bellavalle.Core;

namespace Bellavalle.UI
{
    /// <summary>
    /// Canvas World Space per l'inventario delle frasi apprese (lo "zaino").
    /// Si apre/chiude con il tasto A del controller (o l'equivalente a gesti).
    /// Si riposiziona davanti al player ogni volta che viene aperto.
    ///
    /// AGGIORNAMENTO: la terza tab non è più "Vocabolario" (non è mai stata
    /// popolata: nessun codice chiamava LearnPhrase con quella categoria) —
    /// ora è la tab "AI", che mostra il pannello di AIHelperPanelUI invece
    /// della lista di frasi. Le tab Domande/Risposte restano identiche a prima.
    ///
    /// Ho scritto questo file basandomi sulla versione di InventoryUI.cs che
    /// avevo nel backup, che mi avevi detto essere diversa da quella reale.
    /// Prima di sostituire il tuo file, fai un confronto veloce (diff) con
    /// l'originale nel progetto, per non perdere eventuali differenze che
    /// avevi aggiunto lì nel frattempo.
    ///
    /// Gerarchia consigliata (Panel):
    ///   Panel
    ///   ├── TabBar (3 bottoni: Domande / Risposte / AI, invariati)
    ///   ├── Scroll View          ← assegnalo a "phraseListRoot"
    ///   │    └── Content         ← invariato, è "content"
    ///   ├── EmptyStateText       ← invariato
    ///   ├── clippy               ← invariato, NON gestito da questo script:
    ///   │                          resta visibile su tutte le tab, come già è
    ///   └── AIPanel (NUOVO)      ← crealo: un GameObject vuoto con dentro
    ///        └── AnswerText          un TMP_Text, assegna AIHelperPanelUI
    ///
    /// Setup (oltre a quello che avevi già):
    ///  1. Crea un GameObject "AIPanel" dentro Panel, con un TMP_Text figlio
    ///     (es. "AnswerText"). Metti AIHelperPanelUI.cs su "AIPanel" e
    ///     assegna i suoi campi (aiGrammarHelper, answerText, ecc. — vedi
    ///     i commenti in AIHelperPanelUI.cs).
    ///  2. Su questo InventoryUI, assegna:
    ///     - phraseListRoot = il GameObject "Scroll View" che avevi già
    ///     - aiPanelRoot = il nuovo GameObject "AIPanel"
    ///     - aiHelperPanel = il componente AIHelperPanelUI su "AIPanel"
    ///  3. I 3 bottoni della TabBar NON vanno ritoccati: chiamano ancora
    ///     ShowCategoryByIndex(int), che ora smista internamente verso la
    ///     lista frasi (tab 0/1) o verso il pannello AI (tab 2) — stesso
    ///     nome di metodo di prima, stesso collegamento nell'Inspector.
    ///  4. defaultTab di default è Domande, non più la vecchia tab morta —
    ///     cambialo in Inspector se preferisci che lo zaino si apra su AI.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        public enum InventoryTab { Domande = 0, Risposte = 1, AI = 2 }

        [Header("Canvas")]
        [SerializeField] GameObject canvasRoot;     // FIGLIO con la grafica — non questo GameObject

        [Header("Posizionamento davanti al player")]
        [SerializeField] Transform headTransform;    // Main Camera / XR Camera
        [SerializeField] float distanceFromHead = 0.6f;
        [SerializeField] float heightOffset = -0.1f;

        [Header("Contenuto — lista frasi (tab Domande/Risposte)")]
        [SerializeField] Transform content;          // dentro la ScrollView
        [SerializeField] GameObject phraseRowPrefab; // prefab di una riga (vedi PhraseRowUI)
        [SerializeField] GameObject emptyStateText;   // testo "vuoto" se nessuna frase
        [SerializeField] GameObject phraseListRoot;   // il GameObject "Scroll View" — nascosto sulla tab AI

        [Header("Pannello AI (tab AI, sostituisce Vocabolario)")]
        [SerializeField] AIHelperPanelUI aiHelperPanel;
        [SerializeField] GameObject aiPanelRoot;      // il GameObject "AIPanel" — nascosto sulle altre tab

        [Header("Movimento — bloccato mentre lo zaino è aperto")]
        [SerializeField] FistDynamicMoveProvider moveProvider;

        [Header("Tab attiva all'apertura")]
        [SerializeField] InventoryTab defaultTab = InventoryTab.Domande;

        readonly List<GameObject> _spawnedRows = new();
        bool _isOpen;

        public bool IsOpen => _isOpen;
        public InventoryTab CurrentTab { get; private set; }

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
            if (moveProvider != null) moveProvider.enabled = false;
            ShowTab(defaultTab);
        }

        public void CloseInventory()
        {
            _isOpen = false;
            if (canvasRoot != null) canvasRoot.SetActive(false);
            if (moveProvider != null) moveProvider.enabled = true;
            if (CurrentTab == InventoryTab.AI) aiHelperPanel?.Close();
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

        // ── Cambio tab — stesso metodo/firma di prima, collegato già ai 3 bottoni ──
        public void ShowCategoryByIndex(int index)
        {
            ShowTab((InventoryTab)index);
        }

        void ShowTab(InventoryTab tab)
        {
            // Se stavamo sulla tab AI e ce ne andiamo, chiudi quella modalità
            // (rimette Whisper in italiano, tra le altre cose).
            if (CurrentTab == InventoryTab.AI && tab != InventoryTab.AI)
                aiHelperPanel?.Close();

            CurrentTab = tab;

            if (tab == InventoryTab.AI)
            {
                if (phraseListRoot != null) phraseListRoot.SetActive(false);
                if (emptyStateText != null) emptyStateText.SetActive(false);
                if (aiPanelRoot != null) aiPanelRoot.SetActive(true);
                aiHelperPanel?.Open();
                return;
            }

            if (aiPanelRoot != null) aiPanelRoot.SetActive(false);
            if (phraseListRoot != null) phraseListRoot.SetActive(true);

            var category = tab == InventoryTab.Domande ? PhraseCategory.Domanda : PhraseCategory.Risposta;
            ShowCategory(category);
        }

        void ShowCategory(PhraseCategory category)
        {
            ClearRows();

            if (GameManager.Instance == null) return;
            var phrases = GameManager.Instance.State.GetPhrasesByCategory(category);

            if (emptyStateText != null)
                emptyStateText.SetActive(phrases.Count == 0);

            foreach (var phrase in phrases)
                SpawnRow(phrase);
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