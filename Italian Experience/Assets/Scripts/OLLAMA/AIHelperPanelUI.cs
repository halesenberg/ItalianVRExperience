using UnityEngine;
using TMPro;
using Bellavalle.AI;

namespace Bellavalle.UI
{
    /// <summary>
    /// Pannello minimo per mostrare le risposte di AIGrammarHelperService
    /// nella tab "AI" dello zaino (quella che sostituisce Vocabolario).
    ///
    /// NON è specifico per Clippy: espone solo un testo e un indicatore
    /// generico "sto pensando", così puoi agganciarci sopra qualunque
    /// presentazione tu voglia dare a Clippy (animazione, sprite, ecc.).
    /// Non ho visibilità sul componente Clippy reale nel tuo progetto (non
    /// è tra gli script di BackupScript che ho letto), quindi qui do solo
    /// l'aggancio ai dati — l'aspetto/animazione li colleghi tu, o mi dici
    /// come è fatto Clippy e ti aggiorno questo script di conseguenza.
    ///
    /// Setup:
    ///  1. Metti questo script sul GameObject del pannello "AI" dentro il tuo
    ///     InventoryCanvas (dove oggi sta il contenuto della tab Vocabolario,
    ///     quella mai popolata).
    ///  2. Assegna aiGrammarHelper = il componente AIGrammarHelperService
    ///     (quello sul GameObject persistente del WhisperManager).
    ///  3. Assegna answerText = un TMP_Text dentro il pannello, dove mostrare
    ///     la risposta (o il messaggio di aiuto, o l'errore).
    ///  4. thinkingIndicator è opzionale: un GameObject (es. Clippy mentre
    ///     "pensa") che si attiva da solo mentre aspetti la risposta di Ollama.
    ///  5. Collega Open() al bottone/tab "AI" del tuo InventoryUI (al posto
    ///     di ShowCategory(PhraseCategory.Vocabolario)), e Close() a quando il
    ///     player cambia tab o chiude lo zaino — gestiscono da soli anche
    ///     EnterEnglishMode()/ExitEnglishMode() su Whisper.
    /// </summary>
    public class AIHelperPanelUI : MonoBehaviour
    {
        [SerializeField] AIGrammarHelperService aiGrammarHelper;
        [SerializeField] TMP_Text answerText;
        [SerializeField] GameObject thinkingIndicator;

        void OnEnable()
        {
            if (aiGrammarHelper == null) return;
            aiGrammarHelper.OnAnswerReady += HandleMessage;
            aiGrammarHelper.OnHelpNeeded += HandleMessage;
            aiGrammarHelper.OnError += HandleMessage;
        }

        void OnDisable()
        {
            if (aiGrammarHelper == null) return;
            aiGrammarHelper.OnAnswerReady -= HandleMessage;
            aiGrammarHelper.OnHelpNeeded -= HandleMessage;
            aiGrammarHelper.OnError -= HandleMessage;
        }

        void Update()
        {
            if (thinkingIndicator != null && aiGrammarHelper != null)
                thinkingIndicator.SetActive(aiGrammarHelper.IsWaitingResponse);
        }

        // ── API pubblica — collega ai bottoni/tab del tuo zaino ──────────
        public void Open()
        {
            aiGrammarHelper?.EnterEnglishMode();
            if (answerText != null)
                answerText.text = "Press the mic and ask about a verb — e.g. \"how do you conjugate 'to have'?\"";
        }

        public void Close()
        {
            aiGrammarHelper?.ExitEnglishMode();
        }

        void HandleMessage(string text)
        {
            if (answerText != null) answerText.text = text;
        }
    }
}
