using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class ObjectLabeler : MonoBehaviour
{
    [Header("Riferimenti UI & Audio")]
    [SerializeField] private TMP_Text labelText; // stesso testo principale: ci scriviamo sopra anche Esempi/Spiegazione
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Vector3 offset = new Vector3(0, 0.4f, 0);

    [Header("Grafica & Pulsanti principali")]
    [SerializeField] private GameObject visualContainer;
    [SerializeField] private Button closeButton;   // Pulsante 'X'
    [SerializeField] private Button replayButton;  // Pulsante '🔊' (Riascolta)

    [Header("Pulsanti extra (NUOVO) — sovrascrivono labelText, non aprono pannelli a parte")]
    [Tooltip("Bottone 'E': mostra le frasi di esempio al posto del testo principale. Premuto di nuovo, torna al testo principale.")]
    [SerializeField] private Button examplesButton;
    [Tooltip("Bottone 'i': mostra la spiegazione grammaticale (articolo/preposizione) al posto del testo principale. Premuto di nuovo, torna al testo principale.")]
    [SerializeField] private Button explainButton;

    // FIX: prima non era [SerializeField] — il riferimento all'oggetto
    // "bersaglio" (es. il cappuccino) si perdeva ad ogni ricompilazione di
    // script o riapertura del progetto, e UpdatePosition() smetteva
    // silenziosamente di funzionare (nessun errore, la label restava ferma).
    [SerializeField] private Transform targetObject;
    private LabelData data;

    // Le tre versioni del testo, pre-costruite una volta in SetupIdentikit.
    private string _mainContent;
    private string _examplesContent;
    private string _explanationContent;

    private enum LabelView { Main, Examples, Explanation }
    private LabelView _currentView = LabelView.Main;

    // NUOVO: condivisa tra TUTTE le istanze di ObjectLabeler nella scena.
    // Se è già valorizzata con UN'ALTRA etichetta, questa non si apre affatto
    // — resta bloccata finché quella corrente non viene chiusa con la X.
    private static ObjectLabeler _currentlyOpen;

    // ── Spiegazioni generiche in inglese: valgono per qualunque parola, ─────
    // ── non serve scriverle a mano nel dizionario di EditorLabelGenerator. ──
    static readonly Dictionary<string, string> ArticleExplanations = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
    {
        { "il",  "IL = definite article \"the\", masculine singular (before most consonants)." },
        { "lo",  "LO = definite article \"the\", masculine singular (before s+consonant, z, gn, ps, x, y)." },
        { "l'",  "L' = definite article \"the\", singular, before a noun starting with a vowel." },
        { "la",  "LA = definite article \"the\", feminine singular (before consonants)." },
        { "i",   "I = definite article \"the\", masculine plural." },
        { "gli", "GLI = definite article \"the\", masculine plural (before vowels, s+consonant, z, gn, ps, x, y)." },
        { "le",  "LE = definite article \"the\", feminine plural." },
    };

    static readonly Dictionary<string, string> ArticulatedPrepositions = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
    {
        { "al","A + IL = AL" }, { "allo","A + LO = ALLO" }, { "alla","A + LA = ALLA" }, { "all'","A + L' = ALL'" }, { "ai","A + I = AI" }, { "agli","A + GLI = AGLI" }, { "alle","A + LE = ALLE" },
        { "del","DI + IL = DEL" }, { "dello","DI + LO = DELLO" }, { "della","DI + LA = DELLA" }, { "dell'","DI + L' = DELL'" }, { "dei","DI + I = DEI" }, { "degli","DI + GLI = DEGLI" }, { "delle","DI + LE = DELLE" },
        { "nel","IN + IL = NEL" }, { "nello","IN + LO = NELLO" }, { "nella","IN + LA = NELLA" }, { "nell'","IN + L' = NELL'" }, { "nei","IN + I = NEI" }, { "negli","IN + GLI = NEGLI" }, { "nelle","IN + LE = NELLE" },
        { "sul","SU + IL = SUL" }, { "sullo","SU + LO = SULLO" }, { "sulla","SU + LA = SULLA" }, { "sull'","SU + L' = SULL'" }, { "sui","SU + I = SUI" }, { "sugli","SU + GLI = SUGLI" }, { "sulle","SU + LE = SULLE" },
        { "dal","DA + IL = DAL" }, { "dallo","DA + LO = DALLO" }, { "dalla","DA + LA = DALLA" }, { "dall'","DA + L' = DALL'" }, { "dai","DA + I = DAI" }, { "dagli","DA + GLI = DAGLI" }, { "dalle","DA + LE = DALLE" },

        // CON: forme articolate esistono ma sono poco usate nell'italiano
        // standard moderno (spesso si preferisce "con il/la/i..." staccato).
        // "col" resta la più comune delle sei; le altre sono rare/letterarie
        // ma corrette — le includo per completezza del pannello di spiegazione.
        { "col","CON + IL = COL" }, { "collo","CON + LO = COLLO" }, { "colla","CON + LA = COLLA" }, { "coi","CON + I = COI" }, { "cogli","CON + GLI = COGLI" }, { "colle","CON + LE = COLLE" },

        // PER, TRA, FRA: NON hanno forme articolate — restano sempre staccate
        // ("per il", "tra la", "fra gli"...). Nessuna voce da aggiungere qui:
        // se PrepositionIT inizia con una di queste, il codice sotto non
        // troverà corrispondenza in questa tabella — vedi NonArticulatingPrepositions.
    };

    // Preposizioni che NON si fondono mai con l'articolo — usata solo per
    // mostrare una nota esplicita nel pannello "i" invece di restare mute.
    static readonly HashSet<string> NonArticulatingPrepositions = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        "per", "tra", "fra"
    };

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(CloseLabel);
        if (replayButton != null) replayButton.onClick.AddListener(PlayAudio);
        if (examplesButton != null) examplesButton.onClick.AddListener(ToggleExamples);
        if (explainButton != null) explainButton.onClick.AddListener(ToggleExplanation);
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseLabel);
        if (replayButton != null) replayButton.onClick.RemoveListener(PlayAudio);
        if (examplesButton != null) examplesButton.onClick.RemoveListener(ToggleExamples);
        if (explainButton != null) explainButton.onClick.RemoveListener(ToggleExplanation);

        // Se questa etichetta viene distrutta mentre teneva il lock, liberalo
        // — altrimenti nessun'altra etichetta potrebbe più aprirsi mai più.
        if (_currentlyOpen == this) _currentlyOpen = null;
    }

    // ── API pubblica — chiamata da EditorLabelGenerator ─────────────────
    // NB: la firma è cambiata rispetto a prima (accetta un LabelData invece
    // di 4 stringhe separate) — se in progetto reale c'è qualche altro punto
    // che chiama SetupIdentikit(...) oltre a EditorLabelGenerator, va aggiornato.
    public void SetupIdentikit(Transform target, LabelData labelData, AudioClip clip)
    {
        targetObject = target;
        data = labelData;

        if (audioSource != null && clip != null)
        {
            audioSource.clip = clip;
        }

        BuildMainContent();
        BuildExamplesContent();
        BuildExplanationContent();

        SetView(LabelView.Main);

        UpdatePosition();
        HideLabel();
    }

    // ── Costruzione dei 3 testi (fatta una volta sola, non ad ogni click) ──
    void BuildMainContent()
    {
        // La parola mostrata è sempre "Articolo + Nome" (es. "Il Tavolo"),
        // ricostruita qui — non basta più prendere data.Word da solo, quello
        // è ora il nome nudo (dal GameObject in scena), non la voce del
        // dizionario che potrebbe contenere l'articolo.
        string displayWord = BuildDisplayWord();

        _mainContent = $"<size=120%><b>{displayWord}</b></size>\n" +
                       $"<size=65%><color=#888888><i>[{data.Grammar}]</i></color></size>\n" +
                       $"<size=85%><color=#00AAFF><b>Verbs:</b> {data.Verbs}</color></size>";
    }

    // Ricostruisce "Articolo + Nome" gestendo la 'L apostrofata (niente spazio
    // dopo l'apostrofo, es. "L'Armadio" e non "L' Armadio").
    string BuildDisplayWord()
    {
        if (string.IsNullOrEmpty(data.Article)) return data.Word;

        string articleCap = char.ToUpperInvariant(data.Article[0]) + data.Article.Substring(1);
        string separator = data.Article.EndsWith("'") ? "" : " ";
        return $"{articleCap}{separator}{data.Word}";
    }

    void BuildExamplesContent()
    {
        string[] itLines = string.IsNullOrEmpty(data.ExampleIT) ? new string[0] : data.ExampleIT.Split('\n');
        string[] enLines = string.IsNullOrEmpty(data.ExampleEN) ? new string[0] : data.ExampleEN.Split('\n');

        var sb = new StringBuilder();
        for (int i = 0; i < itLines.Length; i++)
        {
            sb.Append("<b>").Append(itLines[i].Trim()).Append("</b>\n");
            if (i < enLines.Length)
                sb.Append("<i><color=#888888>").Append(enLines[i].Trim()).Append("</color></i>\n");
            sb.Append("\n");
        }

        _examplesContent = sb.ToString().TrimEnd();
    }

    void BuildExplanationContent()
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(data.Article) && ArticleExplanations.TryGetValue(data.Article, out string articleExp))
        {
            sb.Append("<b>").Append(data.Article.ToUpperInvariant()).Append("</b>\n")
              .Append(articleExp).Append("\n\n");
        }

        if (!string.IsNullOrEmpty(data.PrepositionIT))
        {
            // Se la preposizione dell'esempio è articolata (es. "sul", "nella"...)
            // spiega anche la fusione preposizione + articolo.
            string firstWord = data.PrepositionIT.Split(' ')[0].Trim();
            if (ArticulatedPrepositions.TryGetValue(firstWord, out string prepFusion))
            {
                sb.Append("<b>").Append(firstWord.ToUpperInvariant()).Append("</b>\n")
                  .Append(prepFusion).Append(" (articulated preposition)\n\n");
            }
            else if (NonArticulatingPrepositions.Contains(firstWord.ToLowerInvariant()))
            {
                // PER / TRA / FRA: nessuna fusione — lo spieghiamo comunque,
                // invece di restare silenziosi come per un semplice "a"/"in".
                sb.Append("<b>").Append(firstWord.ToUpperInvariant()).Append("</b>\n")
                  .Append(firstWord.ToUpperInvariant()).Append(" never combines with an article — it always stays a separate word.\n\n");
            }

            sb.Append("<b>\"").Append(data.PrepositionIT).Append("\"</b>\n")
              .Append(data.PrepositionEN);
        }

        _explanationContent = sb.ToString().TrimEnd();
    }

    // ── Cambio di vista: sovrascrive labelText, nessun pannello separato ──
    void SetView(LabelView view)
    {
        _currentView = view;
        if (labelText == null) return;

        switch (view)
        {
            case LabelView.Main:
                labelText.text = _mainContent;
                break;
            case LabelView.Examples:
                labelText.text = string.IsNullOrEmpty(_examplesContent) ? _mainContent : _examplesContent;
                break;
            case LabelView.Explanation:
                labelText.text = string.IsNullOrEmpty(_explanationContent) ? _mainContent : _explanationContent;
                break;
        }
    }

    private void LateUpdate()
    {
        if (targetObject != null && visualContainer != null && visualContainer.activeSelf)
        {
            UpdatePosition();
        }
    }

    private void UpdatePosition()
    {
        if (targetObject != null)
        {
            transform.position = targetObject.position + offset;
        }
    }

    // ── NUOVO: posizionamento a mano, visivo ────────────────────────────
    // Flusso consigliato:
    //  1. Genera le etichette col tool (Tools > Genera Etichette VR).
    //  2. Seleziona la label nella Hierarchy, nell'Inspector spunta ON il suo
    //     "Visual Container" (così la vedi in Scene view anche fuori da Play).
    //  3. Trascinala con lo strumento Move dove la vuoi — in Edit Mode nulla
    //     la ricalcola, quindi puoi spostarla liberamente.
    //  4. Click destro sull'header del componente ObjectLabeler (o sui tre
    //     puntini in alto a destra) → "Salva posizione attuale come Offset".
    //     Questo calcola e scrive il campo Offset per te, dalla posizione
    //     in cui l'hai appena trascinata.
    //  5. Rispunta OFF "Visual Container" (a runtime ci pensa LabelInteractable
    //     ad attivarla/disattivarla, il valore ON/OFF qui non conta ai fini
    //     del gameplay, ma tienilo com'era prima per pulizia).
    // Da questo momento l'Offset (non il Transform) è la fonte di verità
    // della posizione, quindi resta corretto anche a runtime e rigenerando.
    [ContextMenu("Salva posizione attuale come Offset")]
    void SaveCurrentPositionAsOffset()
    {
        if (targetObject == null)
        {
            Debug.LogWarning($"[{name}] Impossibile salvare l'offset: 'Target Object' non è assegnato su questa etichetta.");
            return;
        }

        offset = transform.position - targetObject.position;
        Debug.Log($"[{name}] Offset salvato: {offset}");
    }

    public void ShowLabelAndPlayAudio()
    {
        // BLOCCO: se un'ALTRA etichetta è già aperta, questa non si apre per
        // niente (niente pannello, niente audio) — resta bloccata finché
        // quella corrente non viene chiusa con la X. Se avvicini la mano di
        // nuovo dopo averla chiusa, a quel punto questa può aprirsi.
        if (_currentlyOpen != null && _currentlyOpen != this)
        {
            return;
        }
        _currentlyOpen = this;

        if (visualContainer != null)
        {
            visualContainer.SetActive(true);
        }

        // Ogni volta che riapri l'etichetta riparti dal testo principale,
        // non da Esempi/Spiegazione se erano rimasti aperti l'ultima volta.
        SetView(LabelView.Main);

        PlayAudio();
    }

    // Metodo pubblico per riprodurre l'audio a comando
    public void PlayAudio()
    {
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Stop(); // Ferma l'audio se stava già esaurendo per farlo ripartire subito da capo
            audioSource.Play();
        }
    }

    public void CloseLabel()
    {
        if (visualContainer != null)
        {
            visualContainer.SetActive(false);
        }

        _currentView = LabelView.Main; // pronto per la prossima apertura

        // Libera il lock: SOLO da qui in poi un'altra etichetta può aprirsi.
        if (_currentlyOpen == this) _currentlyOpen = null;
    }

    public void HideLabel()
    {
        if (visualContainer != null)
        {
            visualContainer.SetActive(false);
        }
    }

    // ── NUOVO: pulsante "E" (Esempi) ──────────────────────────────────
    public void ToggleExamples()
    {
        SetView(_currentView == LabelView.Examples ? LabelView.Main : LabelView.Examples);
    }

    // ── NUOVO: pulsante "i" (Spiegazione grammaticale) ────────────────
    public void ToggleExplanation()
    {
        SetView(_currentView == LabelView.Explanation ? LabelView.Main : LabelView.Explanation);
    }
}

// ── Dati passati da EditorLabelGenerator, utilizzabili anche a runtime ───
// (WordInfo invece resta editor-only, dentro il blocco #if UNITY_EDITOR)
[System.Serializable]
public struct LabelData
{
    public string Word;
    public string Article;          // "il", "lo", "la", "l'", "i", "gli", "le"
    public string Grammar;          // "Noun | Masculine, Singular"
    public string Verbs;            // "apparecchiare (to set the table), pulire (to clean)"
    public string ExampleIT;        // una o più frasi, separate da \n
    public string ExampleEN;        // traduzioni corrispondenti, stesso numero di righe
    public string PrepositionIT;    // es. "sul tavolo"
    public string PrepositionEN;    // es. "on the table"
}