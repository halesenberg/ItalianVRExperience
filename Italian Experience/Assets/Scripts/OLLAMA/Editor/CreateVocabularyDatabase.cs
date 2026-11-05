#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Bellavalle.Data;
using System.Collections.Generic;

namespace Bellavalle.Editor
{
    public class CreateVocabularyDatabase
    {
        [MenuItem("Assets/Create/Bellavalle/Vocabulary Database")]
        public static void CreateDatabase()
        {
            CreateAndPopulate();
        }

        [MenuItem("Assets/Populate Vocabulary Database")]
        public static void PopulateExisting()
        {
            CreateAndPopulate();
        }

        static void CreateAndPopulate()
        {
            string path = "Assets/Game/Databases/VocabularyDatabase.asset";
            System.IO.Directory.CreateDirectory("Assets/Game/Databases");

            VocabularyDatabase database = AssetDatabase.LoadAssetAtPath<VocabularyDatabase>(path);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<VocabularyDatabase>();
                AssetDatabase.CreateAsset(database, path);
            }

            var entries = new List<VocabularyEntry>
            {
                // Stazione (Controllore)
                new VocabularyEntry { word = "benvenuto", translation_EN = "welcome", partOfSpeech = "adj", definition_EN = "greeted as a guest with pleasure" },
                new VocabularyEntry { word = "città", translation_EN = "city", partOfSpeech = "noun", definition_EN = "a large town" },
                new VocabularyEntry { word = "destra", translation_EN = "right", partOfSpeech = "noun", definition_EN = "the right side or direction" },

                // Quartiere (Lina)
                new VocabularyEntry { word = "nuovo", translation_EN = "new", partOfSpeech = "adj", definition_EN = "not old or previously existing" },
                new VocabularyEntry { word = "vicina", translation_EN = "neighbour (female)", partOfSpeech = "noun", definition_EN = "a female person living nearby" },
                new VocabularyEntry { word = "giro", translation_EN = "tour / walk", partOfSpeech = "noun", definition_EN = "a trip around a place" },
                new VocabularyEntry { word = "casa", translation_EN = "house", partOfSpeech = "noun", definition_EN = "a building where people live" },
                new VocabularyEntry { word = "domani", translation_EN = "tomorrow", partOfSpeech = "adv", definition_EN = "the day after today" },

                // Chitarrista
                new VocabularyEntry { word = "suono", translation_EN = "I play", partOfSpeech = "verb", definition_EN = "to make music with an instrument" },
                new VocabularyEntry { word = "chitarra", translation_EN = "guitar", partOfSpeech = "noun", definition_EN = "a stringed musical instrument" },
                new VocabularyEntry { word = "contare", translation_EN = "to count", partOfSpeech = "verb", definition_EN = "to enumerate numbers" },
                new VocabularyEntry { word = "monete", translation_EN = "coins", partOfSpeech = "noun", definition_EN = "round pieces of metal used as money" },
                new VocabularyEntry { word = "amico", translation_EN = "friend", partOfSpeech = "noun", definition_EN = "a person with whom one has a bond of mutual affection" },

                // Bar (Barista)
                new VocabularyEntry { word = "euro", translation_EN = "euro", partOfSpeech = "noun", definition_EN = "the currency used in most EU countries" },
                new VocabularyEntry { word = "centesimi", translation_EN = "cents", partOfSpeech = "noun", definition_EN = "one hundredth of a euro" },
                new VocabularyEntry { word = "buona", translation_EN = "good (f)", partOfSpeech = "adj", definition_EN = "of high quality" },
                new VocabularyEntry { word = "giornata", translation_EN = "day", partOfSpeech = "noun", definition_EN = "a 24-hour period" },

                // Bar & Servizio
                new VocabularyEntry { word = "caffè", translation_EN = "coffee", partOfSpeech = "noun", definition_EN = "a hot drink made from roasted coffee beans" },
                new VocabularyEntry { word = "cappuccino", translation_EN = "cappuccino", partOfSpeech = "noun", definition_EN = "coffee with steamed milk and foam" },
                new VocabularyEntry { word = "cornetto", translation_EN = "croissant", partOfSpeech = "noun", definition_EN = "a sweet pastry, often eaten for breakfast" },
                new VocabularyEntry { word = "tazza", translation_EN = "cup", partOfSpeech = "noun", definition_EN = "a drinking vessel" },
                new VocabularyEntry { word = "acqua", translation_EN = "water", partOfSpeech = "noun", definition_EN = "clear liquid essential for life" },
                new VocabularyEntry { word = "biglietto", translation_EN = "ticket", partOfSpeech = "noun", definition_EN = "a document giving permission to travel" },
                new VocabularyEntry { word = "uscita", translation_EN = "exit", partOfSpeech = "noun", definition_EN = "a way out of a place" },

                // Casa
                new VocabularyEntry { word = "divano", translation_EN = "sofa", partOfSpeech = "noun", definition_EN = "a long seat for multiple people" },
                new VocabularyEntry { word = "sedia", translation_EN = "chair", partOfSpeech = "noun", definition_EN = "a seat with a backrest for one person" },
                new VocabularyEntry { word = "sgabello", translation_EN = "stool", partOfSpeech = "noun", definition_EN = "a seat without a backrest" },
                new VocabularyEntry { word = "panca", translation_EN = "bench", partOfSpeech = "noun", definition_EN = "a long seat for multiple people" },
                new VocabularyEntry { word = "letto", translation_EN = "bed", partOfSpeech = "noun", definition_EN = "furniture for sleeping" },
                new VocabularyEntry { word = "tavolo", translation_EN = "table", partOfSpeech = "noun", definition_EN = "a flat surface with legs for placing objects" },
                new VocabularyEntry { word = "frigorifero", translation_EN = "fridge", partOfSpeech = "noun", definition_EN = "an appliance for keeping food cold" },
                new VocabularyEntry { word = "cucina", translation_EN = "kitchen", partOfSpeech = "noun", definition_EN = "a room where food is cooked" },
                new VocabularyEntry { word = "pianta", translation_EN = "plant", partOfSpeech = "noun", definition_EN = "a living organism that grows from the ground" },

                // Formule sociali
                new VocabularyEntry { word = "buongiorno", translation_EN = "good morning / hello", partOfSpeech = "phrase", definition_EN = "a polite greeting during the day" },
                new VocabularyEntry { word = "buonasera", translation_EN = "good evening", partOfSpeech = "phrase", definition_EN = "a polite greeting in the evening" },
                new VocabularyEntry { word = "buonanotte", translation_EN = "good night", partOfSpeech = "phrase", definition_EN = "a polite farewell before sleep" },
                new VocabularyEntry { word = "arrivederci", translation_EN = "goodbye", partOfSpeech = "phrase", definition_EN = "a formal farewell" },
                new VocabularyEntry { word = "ciao", translation_EN = "bye / hello", partOfSpeech = "phrase", definition_EN = "an informal greeting or farewell" },
                new VocabularyEntry { word = "grazie", translation_EN = "thank you", partOfSpeech = "phrase", definition_EN = "an expression of gratitude" },
                new VocabularyEntry { word = "prego", translation_EN = "you're welcome", partOfSpeech = "phrase", definition_EN = "a polite response to thanks" },
                new VocabularyEntry { word = "per favore", translation_EN = "please", partOfSpeech = "phrase", definition_EN = "a polite request marker" },
                new VocabularyEntry { word = "scusa", translation_EN = "excuse me / sorry", partOfSpeech = "phrase", definition_EN = "an apology or attention-getter" },
                new VocabularyEntry { word = "piacere", translation_EN = "pleased / nice to meet you", partOfSpeech = "phrase", definition_EN = "a response after introductions" },
                new VocabularyEntry { word = "nessun problema", translation_EN = "no problem", partOfSpeech = "phrase", definition_EN = "a reassuring response" },
                new VocabularyEntry { word = "va bene", translation_EN = "okay / alright", partOfSpeech = "phrase", definition_EN = "an expression of agreement" },

                // Numerali
                new VocabularyEntry { word = "uno", translation_EN = "one", partOfSpeech = "numeral", definition_EN = "the number 1" },
                new VocabularyEntry { word = "due", translation_EN = "two", partOfSpeech = "numeral", definition_EN = "the number 2" },
                new VocabularyEntry { word = "tre", translation_EN = "three", partOfSpeech = "numeral", definition_EN = "the number 3" },
                new VocabularyEntry { word = "quattro", translation_EN = "four", partOfSpeech = "numeral", definition_EN = "the number 4" },
                new VocabularyEntry { word = "cinque", translation_EN = "five", partOfSpeech = "numeral", definition_EN = "the number 5" },
                new VocabularyEntry { word = "sei", translation_EN = "six", partOfSpeech = "numeral", definition_EN = "the number 6" },
                new VocabularyEntry { word = "sette", translation_EN = "seven", partOfSpeech = "numeral", definition_EN = "the number 7" },
                new VocabularyEntry { word = "otto", translation_EN = "eight", partOfSpeech = "numeral", definition_EN = "the number 8" },
                new VocabularyEntry { word = "nove", translation_EN = "nine", partOfSpeech = "numeral", definition_EN = "the number 9" },
                new VocabularyEntry { word = "dieci", translation_EN = "ten", partOfSpeech = "numeral", definition_EN = "the number 10" },

                // Parole mancanti dai dialoghi (Controllore, Lina, chitarrista, Barista)
                new VocabularyEntry { word = "dove", translation_EN = "where", partOfSpeech = "adv", definition_EN = "asking about a place" },
                new VocabularyEntry { word = "ecco", translation_EN = "here it is / there", partOfSpeech = "interjection", definition_EN = "used when presenting or pointing something out" },
                new VocabularyEntry { word = "scusi", translation_EN = "excuse me (formal)", partOfSpeech = "phrase", definition_EN = "polite attention-getter or apology, formal register" },
                new VocabularyEntry { word = "attenta", translation_EN = "careful (f)", partOfSpeech = "adj", definition_EN = "warning someone (female) to be careful" },
                new VocabularyEntry { word = "tranquilla", translation_EN = "don't worry / calm (f)", partOfSpeech = "adj", definition_EN = "reassuring someone (female) that everything is fine" },
                new VocabularyEntry { word = "qui", translation_EN = "here", partOfSpeech = "adv", definition_EN = "indicates this place" },
                new VocabularyEntry { word = "sì", translation_EN = "yes", partOfSpeech = "adv", definition_EN = "affirmative answer" },
                new VocabularyEntry { word = "appena", translation_EN = "just / barely", partOfSpeech = "adv", definition_EN = "very recently or barely" },
                new VocabularyEntry { word = "arrivato", translation_EN = "arrived", partOfSpeech = "adj", definition_EN = "having just reached a place" },
                new VocabularyEntry { word = "come", translation_EN = "how", partOfSpeech = "adv", definition_EN = "asking about manner, e.g. 'come ti chiami?' = what's your name?" },
                new VocabularyEntry { word = "chiami", translation_EN = "you are called", partOfSpeech = "verb", definition_EN = "from 'chiamarsi', used to ask someone's name" },
                new VocabularyEntry { word = "bello", translation_EN = "beautiful / nice", partOfSpeech = "adj", definition_EN = "describes something pleasant or attractive" },
                new VocabularyEntry { word = "bella", translation_EN = "beautiful / nice (f)", partOfSpeech = "adj", definition_EN = "feminine form of 'bello'" },
                new VocabularyEntry { word = "nome", translation_EN = "name", partOfSpeech = "noun", definition_EN = "what a person or thing is called" },
                new VocabularyEntry { word = "bar", translation_EN = "bar / café", partOfSpeech = "noun", definition_EN = "a place serving coffee and drinks" },
                new VocabularyEntry { word = "tua", translation_EN = "your (f)", partOfSpeech = "adj", definition_EN = "possessive, feminine singular" },
                new VocabularyEntry { word = "tuoi", translation_EN = "your (pl)", partOfSpeech = "adj", definition_EN = "possessive, masculine plural" },
                new VocabularyEntry { word = "porto", translation_EN = "I bring / I carry", partOfSpeech = "verb", definition_EN = "from 'portare', to bring or carry" },
                new VocabularyEntry { word = "mille", translation_EN = "a thousand / a lot", partOfSpeech = "numeral", definition_EN = "used in 'grazie mille' = thanks a lot" },
                new VocabularyEntry { word = "perfetto", translation_EN = "perfect", partOfSpeech = "adj", definition_EN = "expresses agreement or approval" },
                new VocabularyEntry { word = "verso", translation_EN = "towards", partOfSpeech = "prep", definition_EN = "indicates direction towards something" },
                new VocabularyEntry { word = "porta", translation_EN = "door", partOfSpeech = "noun", definition_EN = "an entrance to a room or building" },
                new VocabularyEntry { word = "entrare", translation_EN = "to enter", partOfSpeech = "verb", definition_EN = "to go into a place" },
                new VocabularyEntry { word = "bene", translation_EN = "well / good", partOfSpeech = "adv", definition_EN = "expresses that something is good or fine" },
                new VocabularyEntry { word = "soldi", translation_EN = "money", partOfSpeech = "noun", definition_EN = "currency, cash" },
                new VocabularyEntry { word = "certo", translation_EN = "certainly / sure", partOfSpeech = "adv", definition_EN = "expresses confident agreement" },
                new VocabularyEntry { word = "volentieri", translation_EN = "gladly", partOfSpeech = "adv", definition_EN = "willingly, with pleasure" },
                new VocabularyEntry { word = "capito", translation_EN = "understood", partOfSpeech = "verb", definition_EN = "past participle of 'capire', to understand" },
                new VocabularyEntry { word = "aiuto", translation_EN = "help", partOfSpeech = "noun", definition_EN = "assistance given to someone" },
                new VocabularyEntry { word = "vediamo", translation_EN = "we see / let's see", partOfSpeech = "verb", definition_EN = "from 'vedere', used in 'ci vediamo' = see you" },
                new VocabularyEntry { word = "italiani", translation_EN = "Italians", partOfSpeech = "noun", definition_EN = "people from Italy" },
                new VocabularyEntry { word = "qualcuno", translation_EN = "someone", partOfSpeech = "pron", definition_EN = "an unspecified person" },
                new VocabularyEntry { word = "ricevuto", translation_EN = "received", partOfSpeech = "verb", definition_EN = "past participle of 'ricevere', to receive" },
                new VocabularyEntry { word = "favore", translation_EN = "favour", partOfSpeech = "noun", definition_EN = "a kind or helpful act" },
                new VocabularyEntry { word = "tutto", translation_EN = "everything / all", partOfSpeech = "pron", definition_EN = "the whole amount or all of something" },
                new VocabularyEntry { word = "altro", translation_EN = "other / something else", partOfSpeech = "pron", definition_EN = "something in addition or different" },
                new VocabularyEntry { word = "no", translation_EN = "no", partOfSpeech = "adv", definition_EN = "negative answer" },
                new VocabularyEntry { word = "qualcosa", translation_EN = "something", partOfSpeech = "pron", definition_EN = "an unspecified thing" },
                new VocabularyEntry { word = "lei", translation_EN = "she / you (formal)", partOfSpeech = "pron", definition_EN = "third person singular, also used as formal 'you'" },
                new VocabularyEntry { word = "bisogno", translation_EN = "need", partOfSpeech = "noun", definition_EN = "used in 'non c'è bisogno' = there's no need" },

                // Pronomi personali (soggetto) — insegnati anche nella canzone "Acqua azzurra, acqua chiara" al giradischi
                new VocabularyEntry { word = "io", translation_EN = "I", partOfSpeech = "pronoun", definition_EN = "first person singular subject pronoun" },
                new VocabularyEntry { word = "tu", translation_EN = "you (singular)", partOfSpeech = "pronoun", definition_EN = "second person singular subject pronoun, informal" },
                new VocabularyEntry { word = "lui", translation_EN = "he", partOfSpeech = "pronoun", definition_EN = "third person singular subject pronoun, masculine" },
                new VocabularyEntry { word = "noi", translation_EN = "we", partOfSpeech = "pronoun", definition_EN = "first person plural subject pronoun" },
                new VocabularyEntry { word = "voi", translation_EN = "you (plural)", partOfSpeech = "pronoun", definition_EN = "second person plural subject pronoun" },
                new VocabularyEntry { word = "loro", translation_EN = "they / their", partOfSpeech = "pronoun", definition_EN = "third person plural subject pronoun; also used as possessive 'their' (invariable)" },

                // Pronomi possessivi
                new VocabularyEntry { word = "mio", translation_EN = "my / mine (m)", partOfSpeech = "pronoun", definition_EN = "possessive, masculine singular" },
                new VocabularyEntry { word = "mia", translation_EN = "my / mine (f)", partOfSpeech = "pronoun", definition_EN = "possessive, feminine singular" },
                new VocabularyEntry { word = "miei", translation_EN = "my / mine (m pl)", partOfSpeech = "pronoun", definition_EN = "possessive, masculine plural" },
                new VocabularyEntry { word = "mie", translation_EN = "my / mine (f pl)", partOfSpeech = "pronoun", definition_EN = "possessive, feminine plural" },
                new VocabularyEntry { word = "tuo", translation_EN = "your / yours (m)", partOfSpeech = "pronoun", definition_EN = "possessive, masculine singular, informal" },
                new VocabularyEntry { word = "tue", translation_EN = "your / yours (f pl)", partOfSpeech = "pronoun", definition_EN = "possessive, feminine plural, informal" },
                new VocabularyEntry { word = "suo", translation_EN = "his / her / its (m)", partOfSpeech = "pronoun", definition_EN = "possessive, masculine singular" },
                new VocabularyEntry { word = "sua", translation_EN = "his / her / its (f)", partOfSpeech = "pronoun", definition_EN = "possessive, feminine singular" },
                new VocabularyEntry { word = "suoi", translation_EN = "his / her / its (m pl)", partOfSpeech = "pronoun", definition_EN = "possessive, masculine plural" },
                new VocabularyEntry { word = "sue", translation_EN = "his / her / its (f pl)", partOfSpeech = "pronoun", definition_EN = "possessive, feminine plural" },
                new VocabularyEntry { word = "nostro", translation_EN = "our / ours (m)", partOfSpeech = "pronoun", definition_EN = "possessive, masculine singular" },
                new VocabularyEntry { word = "nostra", translation_EN = "our / ours (f)", partOfSpeech = "pronoun", definition_EN = "possessive, feminine singular" },
                new VocabularyEntry { word = "nostri", translation_EN = "our / ours (m pl)", partOfSpeech = "pronoun", definition_EN = "possessive, masculine plural" },
                new VocabularyEntry { word = "nostre", translation_EN = "our / ours (f pl)", partOfSpeech = "pronoun", definition_EN = "possessive, feminine plural" },
                new VocabularyEntry { word = "vostro", translation_EN = "your / yours (m, pl. owner)", partOfSpeech = "pronoun", definition_EN = "possessive, masculine singular, formal/plural owner" },
                new VocabularyEntry { word = "vostra", translation_EN = "your / yours (f, pl. owner)", partOfSpeech = "pronoun", definition_EN = "possessive, feminine singular, formal/plural owner" },
                new VocabularyEntry { word = "vostri", translation_EN = "your / yours (m pl)", partOfSpeech = "pronoun", definition_EN = "possessive, masculine plural" },
                new VocabularyEntry { word = "vostre", translation_EN = "your / yours (f pl)", partOfSpeech = "pronoun", definition_EN = "possessive, feminine plural" },
            };

            database.entries = entries.ToArray();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CreateVocabularyDatabase] Asset creato: {path}");
            EditorUtility.RevealInFinder(path);
        }
    }
}
#endif
