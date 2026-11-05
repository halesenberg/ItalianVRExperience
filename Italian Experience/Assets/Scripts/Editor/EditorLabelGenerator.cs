#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
public class EditorLabelGenerator : EditorWindow
{
    private ObjectLabeler labelPrefab;
    private const string folderPath = "Assets/AudioLabels";
    private Dictionary<string, WordInfo> wordDatabase = new Dictionary<string, WordInfo>(System.StringComparer.OrdinalIgnoreCase)
    {
        { "Sedia",              new WordInfo("Noun | Feminine, Singular", "la",
            "sedersi (to sit down), mettere (to put), pulire (to clean)",
            "Io mi siedo sulla sedia.", "I sit on the chair.",
            "sulla sedia", "on the chair") },

        { "Tavolo",             new WordInfo("Noun | Masculine, Singular (also used as 'la tavola', feminine, in the dining-table sense)", "il",
            "apparecchiare (to set/lay the table), pulire (to clean), mettere (to put)",
            "Io apparecchio il tavolo.\nIo mangio a tavola.", "I set the table.\nI eat at the table.",
            "a tavola", "at the table (idiomatic — no article after 'a' here)") },

        { "Divano",             new WordInfo("Noun | Masculine, Singular", "il",
            "sedersi (to sit down), sdraiarsi (to lie down), riposare (to rest)",
            "Io mi sdraio sul divano.", "I lie down on the sofa.",
            "sul divano", "on the sofa") },

        { "Tavolino",           new WordInfo("Noun | Masculine, Singular", "il",
            "mettere (to put), pulire (to clean), prendere (to take)",
            "Io metto il libro sul tavolino.", "I put the book on the coffee table.",
            "sul tavolino", "on the coffee table") },

        { "Poltrona",           new WordInfo("Noun | Feminine, Singular", "la",
            "sedersi (to sit down), rilassarsi (to relax), sprofondare (to sink into)",
            "Io mi rilasso sulla poltrona.", "I relax in the armchair.",
            "sulla poltrona", "in the armchair") },

        { "Sgabello",           new WordInfo("Noun | Masculine, Singular", "lo",
            "sedersi (to sit down), mettere (to put), prendere (to take)",
            "Io prendo lo sgabello.", "I take the stool.",
            "sullo sgabello", "on the stool") },

        { "Sedia girevole",     new WordInfo("Noun | Feminine, Singular", "la",
            "girare (to spin), sedersi (to sit down), regolare (to adjust)",
            "Io giro la sedia girevole.", "I spin the swivel chair.",
            "sulla sedia girevole", "on the swivel chair") },

        { "TV",              new WordInfo("Noun | Feminine, Singular", "la",
            "accendere (to turn on), spegnere (to turn off), guardare (to watch)",
            "Io guardo la TV.", "I watch TV.",
            "davanti alla TV", "in front of the TV") },

        { "Computer",        new WordInfo("Noun | Masculine, Singular", "il",
            "accendere (to turn on), usare (to use), spegnere (to turn off)",
            "Io uso il computer.", "I use the computer.",
            "al computer", "at the computer") },

        { "Giradischi",      new WordInfo("Noun | Masculine, Singular", "il",
            "accendere (to turn on), ascoltare (to listen to), usare (to use)",
            "Io accendo il giradischi.", "I turn on the record player.",
            "sul giradischi", "on the record player") },

        { "Disco",           new WordInfo("Noun | Masculine, Singular", "il",
            "mettere (to put on), ascoltare (to listen to), cambiare (to change)",
            "Io metto il disco.", "I put the record on.",
            "sul disco", "on the record") },

        { "Vinile",          new WordInfo("Noun | Masculine, Singular", "il",
            "ascoltare (to listen to), pulire (to clean), collezionare (to collect)",
            "Io ascolto il vinile.", "I listen to the vinyl.",
            "del vinile", "of the vinyl") },

        { "Carte da gioco",  new WordInfo("Noun | Feminine, Plural", "le",
            "mescolare (to shuffle), dare (to deal), giocare (to play)",
            "Io mescolo le carte da gioco.", "I shuffle the playing cards.",
            "con le carte da gioco", "with the playing cards") },

        { "Carte",           new WordInfo("Noun | Feminine, Plural", "le",
            "giocare (to play), pescare (to draw), scartare (to discard)",
            "Io gioco a carte.", "I play cards.",
            "a carte", "cards (idiomatic — 'giocare a carte')") },

        { "Pianta",          new WordInfo("Noun | Feminine, Singular", "la",
            "annaffiare (to water), curare (to take care of), trapiantare (to transplant)",
            "Io annaffio la pianta.", "I water the plant.",
            "vicino alla pianta", "near the plant") },

        { "Pesce",           new WordInfo("Noun | Masculine, Singular", "il",
            "nutrire (to feed), osservare (to observe), cucinare (to cook)",
            "Io nutro il pesce.", "I feed the fish.",
            "vicino al pesce", "near the fish") },

        { "Letto",           new WordInfo("Noun | Masculine, Singular", "il",
            "dormire (to sleep), rifare (to make [the bed]), sdraiarsi (to lie down)",
            "Io dormo nel letto.", "I sleep in the bed.",
            "nel letto", "in the bed") },

        { "Comodino",        new WordInfo("Noun | Masculine, Singular", "il",
            "mettere (to put), aprire (to open), pulire (to clean)",
            "Io metto il libro sul comodino.", "I put the book on the nightstand.",
            "sul comodino", "on the nightstand") },

        { "Armadio",         new WordInfo("Noun | Masculine, Singular", "l'",
            "aprire (to open), chiudere (to close), riordinare (to tidy up)",
            "Io apro l'armadio.", "I open the wardrobe.",
            "nell'armadio", "in the wardrobe") },

        { "Libreria",        new WordInfo("Noun | Feminine, Singular", "la",
            "riordinare (to tidy up), spolverare (to dust), riempire (to fill)",
            "Io riordino la libreria.", "I tidy the bookshelf.",
            "nella libreria", "in the bookshelf") },

        { "Libri",           new WordInfo("Noun | Masculine, Plural", "i",
            "leggere (to read), sfogliare (to leaf through), riordinare (to tidy up)",
            "Io leggo i libri.", "I read the books.",
            "sui libri", "on the books") },

        { "Cuscino",         new WordInfo("Noun | Masculine, Singular", "il",
            "abbracciare (to hug), sprimacciare (to fluff), sistemare (to arrange)",
            "Io abbraccio il cuscino.", "I hug the pillow.",
            "sul cuscino", "on the pillow") },

        { "Coperta",         new WordInfo("Noun | Feminine, Singular", "la",
            "coprire (to cover), piegare (to fold), lavare (to wash)",
            "Io piego la coperta.", "I fold the blanket.",
            "sotto la coperta", "under the blanket") },

        { "Cucina",          new WordInfo("Noun | Feminine, Singular", "la",
            "cucinare (to cook), pulire (to clean), riordinare (to tidy up)",
            "Io cucino in cucina.", "I cook in the kitchen.",
            "in cucina", "in the kitchen (idiomatic — no article)") },

        { "Padella",         new WordInfo("Noun | Feminine, Singular", "la",
            "scaldare (to heat), friggere (to fry), lavare (to wash)",
            "Io scaldo la padella.", "I heat the pan.",
            "nella padella", "in the pan") },

        { "Tagliere",        new WordInfo("Noun | Masculine, Singular", "il",
            "tagliare (to cut), affettare (to slice), lavare (to wash)",
            "Io taglio sul tagliere.", "I cut on the cutting board.",
            "sul tagliere", "on the cutting board") },

        { "Piatto",          new WordInfo("Noun | Masculine, Singular", "il",
            "servire (to serve), lavare (to wash), riempire (to fill)",
            "Io lavo il piatto.", "I wash the plate.",
            "nel piatto", "on the plate") },

        { "Utensili",        new WordInfo("Noun | Masculine, Plural", "gli",
            "usare (to use), mescolare (to mix), lavare (to wash)",
            "Io uso gli utensili.", "I use the utensils.",
            "con gli utensili", "with the utensils") },

        { "Lavabo",          new WordInfo("Noun | Masculine, Singular", "il",
            "aprire (to turn on), lavare (to wash), pulire (to clean)",
            "Io apro il lavabo.", "I turn on the sink.",
            "nel lavabo", "in the sink") },

        { "Lavandino",       new WordInfo("Noun | Masculine, Singular", "il",
            "lavarsi (to wash oneself), aprire (to turn on), pulire (to clean)",
            "Io mi lavo al lavandino.", "I wash myself at the sink.",
            "al lavandino", "at the sink") },

        { "Rubinetto",       new WordInfo("Noun | Masculine, Singular", "il",
            "aprire (to turn on), chiudere (to turn off), chiudere bene (to close tightly)",
            "Io apro il rubinetto.", "I turn on the tap.",
            "dal rubinetto", "from the tap") },

        { "Doccia",          new WordInfo("Noun | Feminine, Singular", "la",
            "fare (to take [a shower]), lavarsi (to wash oneself), asciugare (to dry)",
            "Io faccio la doccia.", "I take a shower.",
            "sotto la doccia", "under the shower") },

        { "Gabinetto",       new WordInfo("Noun | Masculine, Singular", "il",
            "usare (to use), pulire (to clean), igienizzare (to sanitize)",
            "Io pulisco il gabinetto.", "I clean the toilet.",
            "nel gabinetto", "in the toilet") },

        { "Scarico",         new WordInfo("Noun | Masculine, Singular", "lo",
            "tirare (to flush/pull), usare (to use), controllare (to check)",
            "Io tiro lo scarico.", "I flush the drain.",
            "dallo scarico", "from the drain") },

        { "Sapone",          new WordInfo("Noun | Masculine, Singular", "il",
            "usare (to use), strofinare (to rub), sciacquare (to rinse)",
            "Io uso il sapone.", "I use the soap.",
            "con il sapone", "with the soap") },

        { "Carta Igienica",  new WordInfo("Noun | Feminine, Singular", "la",
            "usare (to use), strappare (to tear), rimpiazzare (to replace)",
            "Io strappo la carta igienica.", "I tear the toilet paper.",
            "con la carta igienica", "with the toilet paper") },

        { "Lavatrice",       new WordInfo("Noun | Feminine, Singular", "la",
            "caricare (to load), usare (to use), svuotare (to empty)",
            "Io carico la lavatrice.", "I load the washing machine.",
            "nella lavatrice", "in the washing machine") },

        { "Specchio",        new WordInfo("Noun | Masculine, Singular", "lo",
            "guardarsi (to look at oneself), riflettere (to reflect), pulire (to clean)",
            "Io mi guardo allo specchio.", "I look at myself in the mirror.",
            "allo specchio", "in the mirror") },

        { "Cestino",         new WordInfo("Noun | Masculine, Singular", "il",
            "buttare (to throw away), svuotare (to empty), pulire (to clean)",
            "Io butto la carta nel cestino.", "I throw the paper in the bin.",
            "nel cestino", "in the bin") },

        { "Ciabatte",        new WordInfo("Noun | Feminine, Plural", "le",
            "indossare (to wear), togliere (to take off), infilare (to slip on)",
            "Io indosso le ciabatte.", "I wear the slippers.",
            "con le ciabatte", "with the slippers") },

        { "Porta",           new WordInfo("Noun | Feminine, Singular", "la",
            "aprire (to open), chiudere (to close), bussare (to knock)",
            "Io apro la porta.", "I open the door.",
            "alla porta", "at the door") },

        { "Maniglia",        new WordInfo("Noun | Feminine, Singular", "la",
            "spingere (to push), abbassare (to lower/press down), afferrare (to grab)",
            "Io afferro la maniglia.", "I grab the handle.",
            "con la maniglia", "with the handle") },

        { "Finestra",        new WordInfo("Noun | Feminine, Singular", "la",
            "aprire (to open), chiudere (to close), spalancare (to open wide)",
            "Io apro la finestra.", "I open the window.",
            "dalla finestra", "from the window") }
    };
    [MenuItem("Tools/Genera Etichette VR (Offline)")]
    public static void ShowWindow()
    {
        GetWindow<EditorLabelGenerator>("Generatore Identikit VR");
    }
    private void OnGUI()
    {
        GUILayout.Label("Configurazione Identikit VR (QCER / Grammatica)", EditorStyles.boldLabel);
        labelPrefab = (ObjectLabeler)EditorGUILayout.ObjectField("Label Prefab", labelPrefab, typeof(ObjectLabeler), false);
        EditorGUILayout.Space(10);
        if (GUILayout.Button("1. Esporta Lista Parole (.txt)", GUILayout.Height(30)))
        {
            ExportWordList();
        }
        EditorGUILayout.Space(5);
        if (GUILayout.Button("2. Genera Identikit e Collega Audio Localmente", GUILayout.Height(30)))
        {
            if (labelPrefab == null)
            {
                EditorUtility.DisplayDialog("Errore", "Assegna il Prefab Etichetta nello slot!", "OK");
                return;
            }
            ProcessAllLabelsLocal();
        }
    }
    private void ProcessAllLabelsLocal()
    {
        GameObject[] targets = GameObject.FindGameObjectsWithTag("Labelable");
        if (targets.Length == 0)
        {
            Debug.LogWarning("Nessun oggetto trovato con il Tag 'Labelable'.");
            return;
        }
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }

        int createdCount = 0;
        int updatedCount = 0;
        int skippedCount = 0;

        foreach (GameObject obj in targets)
        {
            if (obj == null) continue;

            // Un problema su UN oggetto non deve più far saltare tutti quelli
            // dopo di lui nella lista: lo isoliamo con un try/catch e continuiamo.
            try
            {
                string word = CleanObjectName(obj.name);

                if (string.IsNullOrWhiteSpace(word))
                {
                    Debug.LogWarning($"[SALTATO] L'oggetto '{obj.name}' ha un nome vuoto dopo la pulizia (CleanObjectName) — controllalo in scena, probabilmente è un oggetto organizzativo taggato 'Labelable' per errore, o un nome che inizia con '_'.", obj);
                    skippedCount++;
                    continue;
                }

                // Recupera dati dal dizionario
                string grammar = "Noun | Singular";
                string article = "il";
                string verbs = "usare, guardare";
                string exampleIT = "";
                string exampleEN = "";
                string prepIT = "";
                string prepEN = "";

                if (wordDatabase.TryGetValue(word, out WordInfo info))
                {
                    grammar = info.Grammar;
                    article = info.Article;
                    verbs = info.Verbs;
                    exampleIT = info.ExampleIT;
                    exampleEN = info.ExampleEN;
                    prepIT = info.PrepositionIT;
                    prepEN = info.PrepositionEN;
                }
                else
                {
                    Debug.LogWarning($"[DEFAULT] '{obj.name}' → parola pulita '{word}' non trovata nel dizionario, uso i valori generici di default.", obj);
                }

                AudioClip clip = FindLocalAudioClip(word);

                // 1. Assegna LabelInteractable
                LabelInteractable interactable = obj.GetComponent<LabelInteractable>();
                if (interactable == null)
                {
                    interactable = Undo.AddComponent<LabelInteractable>(obj);
                }

                if (interactable == null)
                {
                    Debug.LogError($"[ERRORE] Impossibile aggiungere/leggere 'LabelInteractable' su '{obj.name}'. Probabile causa: un problema di compilazione, o l'oggetto non permette l'aggiunta di componenti in questo momento (es. prefab con override bloccati).", obj);
                    skippedCount++;
                    continue;
                }

                // 2. Riusa l'etichetta già collegata, se esiste — NON creare una
                //    nuova istanza ogni volta: altrimenti si perdono posizione
                //    (offset), duplicati, e qualunque altra modifica fatta a mano
                //    sull'istanza esistente in scena.
                ObjectLabeler newLabel = interactable.AssociatedLabel;
                bool isNewLabel = (newLabel == null);

                if (isNewLabel)
                {
                    newLabel = (ObjectLabeler)PrefabUtility.InstantiatePrefab(labelPrefab);
                    if (newLabel == null)
                    {
                        Debug.LogError($"[ERRORE] Impossibile istanziare il Prefab dell'etichetta per l'oggetto '{obj.name}'. Assicurati che lo script 'ObjectLabeler' sia attaccato alla radice del Prefab!", obj);
                        skippedCount++;
                        continue;
                    }
                }

                // 3. Esegue il Setup dell'etichetta (dati raggruppati in LabelData, vedi ObjectLabeler.cs)
                //    SetupIdentikit aggiorna solo testo/audio, non tocca l'offset
                //    di posizione, quindi è sicuro richiamarlo anche su un'etichetta
                //    già posizionata a mano.
                LabelData data = new LabelData
                {
                    Word = word,
                    Article = article,
                    Grammar = grammar,
                    Verbs = verbs,
                    ExampleIT = exampleIT,
                    ExampleEN = exampleEN,
                    PrepositionIT = prepIT,
                    PrepositionEN = prepEN,
                };
                newLabel.SetupIdentikit(obj.transform, data, clip);

                // 4. Collega l'etichetta all'interactable (solo se è nuova — se era
                //    già associata non serve/non deve essere ri-registrata) e
                //    registra l'Undo solo per gli oggetti effettivamente creati ora.
                if (isNewLabel)
                {
                    interactable.RegisterLabel(newLabel);
                    Undo.RegisterCreatedObjectUndo(newLabel.gameObject, "Crea Identikit VR");
                    createdCount++;
                }
                else
                {
                    Undo.RecordObject(newLabel, "Aggiorna Identikit VR");
                    EditorUtility.SetDirty(newLabel);
                    updatedCount++;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ERRORE] Eccezione processando '{obj.name}': {ex.Message}\n{ex.StackTrace}", obj);
                skippedCount++;
            }
        }
        Debug.Log($"Generazione Identikit completata: {createdCount} create, {updatedCount} aggiornate, {skippedCount} saltate per errore (guarda i Warning/Error sopra per i dettagli).");
    }
    private void ExportWordList()
    {
        GameObject[] targets = GameObject.FindGameObjectsWithTag("Labelable");
        if (targets.Length == 0) return;
        HashSet<string> uniqueWords = new HashSet<string>();
        foreach (GameObject obj in targets)
        {
            if (obj != null) uniqueWords.Add(CleanObjectName(obj.name));
        }
        string outputPath = Path.Combine(Application.dataPath, "ListaParoleEtichette.txt");
        StringBuilder sb = new StringBuilder();
        foreach (string word in uniqueWords) sb.AppendLine(word);
        File.WriteAllText(outputPath, sb.ToString());
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Completato", $"Esportate {uniqueWords.Count} parole uniche in:\nAssets/ListaParoleEtichette.txt", "OK");
    }
    private AudioClip FindLocalAudioClip(string fileName)
    {
        string[] extensions = { ".mp3", ".wav", ".ogg" };
        foreach (string ext in extensions)
        {
            string fullPath = $"{folderPath}/{fileName}{ext}";
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(fullPath);
            if (clip != null) return clip;
        }
        return null;
    }
    private string CleanObjectName(string rawName)
    {
        string cleaned = rawName.Replace("(Clone)", "").Trim();
        int underscoreIdx = cleaned.IndexOf('_');
        if (underscoreIdx >= 0) cleaned = cleaned.Substring(0, underscoreIdx);

        // NUOVO: Unity rinomina in automatico i GameObject duplicati aggiungendo
        // " (1)", " (2)", ecc. — senza questo, "Vinile (1)" non troverebbe mai
        // corrispondenza nel dizionario (che ha solo la chiave "Vinile").
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s*\(\d+\)$", "");

        return cleaned.Trim();
    }
}
public class WordInfo
{
    public string Grammar;
    public string Article;          // "il", "lo", "la", "l'", "i", "gli", "le"
    public string Verbs;            // es. "apparecchiare (to set the table), pulire (to clean)"
    public string ExampleIT;        // una o più frasi separate da \n
    public string ExampleEN;        // traduzioni corrispondenti, stesso numero di righe
    public string PrepositionIT;    // es. "sul tavolo"
    public string PrepositionEN;    // es. "on the table"

    public WordInfo(string grammar, string article, string verbs, string exampleIT, string exampleEN, string prepositionIT, string prepositionEN)
    {
        Grammar = grammar;
        Article = article;
        Verbs = verbs;
        ExampleIT = exampleIT;
        ExampleEN = exampleEN;
        PrepositionIT = prepositionIT;
        PrepositionEN = prepositionEN;
    }
}
#endif