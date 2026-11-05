#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Bellavalle.Data;
using System.Collections.Generic;

namespace Bellavalle.Editor
{
    public class CreateVerbConjugationDatabase
    {
        [MenuItem("Assets/Create/Bellavalle/Verb Conjugation Database")]
        public static void CreateDatabase()
        {
            CreateAndPopulate();
        }

        [MenuItem("Assets/Populate Verb Conjugation Database")]
        public static void PopulateExisting()
        {
            CreateAndPopulate();
        }

        static void CreateAndPopulate()
        {
            string path = "Assets/Game/Databases/VerbConjugationDatabase.asset";
            System.IO.Directory.CreateDirectory("Assets/Game/Databases");

            VerbConjugationDatabase database = AssetDatabase.LoadAssetAtPath<VerbConjugationDatabase>(path);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<VerbConjugationDatabase>();
                AssetDatabase.CreateAsset(database, path);
            }

            var entries = new List<VerbEntry>
            {
                // Verbi comuni (presenti nei dialoghi)
                new VerbEntry
                {
                    infinitive = "essere",
                    infinitiveEN = "to be",
                    io = "sono",
                    tu = "sei",
                    luiLei = "è",
                    noi = "siamo",
                    voi = "siete",
                    loro = "sono"
                },
                new VerbEntry
                {
                    infinitive = "avere",
                    infinitiveEN = "to have",
                    io = "ho",
                    tu = "hai",
                    luiLei = "ha",
                    noi = "abbiamo",
                    voi = "avete",
                    loro = "hanno"
                },
                new VerbEntry
                {
                    infinitive = "andare",
                    infinitiveEN = "to go",
                    io = "vado",
                    tu = "vai",
                    luiLei = "va",
                    noi = "andiamo",
                    voi = "andate",
                    loro = "vanno"
                },
                new VerbEntry
                {
                    infinitive = "volere",
                    infinitiveEN = "to want",
                    io = "voglio",
                    tu = "vuoi",
                    luiLei = "vuole",
                    noi = "vogliamo",
                    voi = "volete",
                    loro = "vogliono"
                },
                new VerbEntry
                {
                    infinitive = "venire",
                    infinitiveEN = "to come",
                    io = "vengo",
                    tu = "vieni",
                    luiLei = "viene",
                    noi = "veniamo",
                    voi = "venite",
                    loro = "vengono"
                },
                new VerbEntry
                {
                    infinitive = "potere",
                    infinitiveEN = "can / to be able",
                    io = "posso",
                    tu = "puoi",
                    luiLei = "può",
                    noi = "possiamo",
                    voi = "potete",
                    loro = "possono"
                },
                new VerbEntry
                {
                    infinitive = "capire",
                    infinitiveEN = "to understand",
                    io = "capisco",
                    tu = "capisci",
                    luiLei = "capisce",
                    noi = "capiamo",
                    voi = "capite",
                    loro = "capiscono"
                },
                new VerbEntry
                {
                    infinitive = "suonare",
                    infinitiveEN = "to play (instrument)",
                    io = "suono",
                    tu = "suoni",
                    luiLei = "suona",
                    noi = "suoniamo",
                    voi = "suonate",
                    loro = "suonano"
                },
                new VerbEntry
                {
                    infinitive = "contare",
                    infinitiveEN = "to count",
                    io = "conto",
                    tu = "conti",
                    luiLei = "conta",
                    noi = "contiamo",
                    voi = "contate",
                    loro = "contano"
                },
                new VerbEntry
                {
                    infinitive = "aiutare",
                    infinitiveEN = "to help",
                    io = "aiuto",
                    tu = "aiuti",
                    luiLei = "aiuta",
                    noi = "aiutiamo",
                    voi = "aiutate",
                    loro = "aiutano"
                },
                new VerbEntry
                {
                    infinitive = "camminare",
                    infinitiveEN = "to walk",
                    io = "cammino",
                    tu = "cammini",
                    luiLei = "cammina",
                    noi = "camminiamo",
                    voi = "camminate",
                    loro = "camminano"
                },
                new VerbEntry
                {
                    infinitive = "considerare",
                    infinitiveEN = "to consider",
                    io = "considero",
                    tu = "consideri",
                    luiLei = "considera",
                    noi = "consideriamo",
                    voi = "considerate",
                    loro = "considerano"
                },
                new VerbEntry
                {
                    infinitive = "scusare",
                    infinitiveEN = "to excuse",
                    io = "scuso",
                    tu = "scusi",
                    luiLei = "scusa",
                    noi = "scusiamo",
                    voi = "scusate",
                    loro = "scusano"
                },
                new VerbEntry
                {
                    infinitive = "chiamarsi",
                    infinitiveEN = "to be called",
                    io = "mi chiamo",
                    tu = "ti chiami",
                    luiLei = "si chiama",
                    noi = "ci chiamiamo",
                    voi = "vi chiamate",
                    loro = "si chiamano"
                },
                new VerbEntry
                {
                    infinitive = "arrivare",
                    infinitiveEN = "to arrive",
                    io = "arrivo",
                    tu = "arrivi",
                    luiLei = "arriva",
                    noi = "arriviamo",
                    voi = "arrivate",
                    loro = "arrivano"
                },
                new VerbEntry
                {
                    infinitive = "portare",
                    infinitiveEN = "to bring / to carry",
                    io = "porto",
                    tu = "porti",
                    luiLei = "porta",
                    noi = "portiamo",
                    voi = "portate",
                    loro = "portano"
                },
                new VerbEntry
                {
                    infinitive = "entrare",
                    infinitiveEN = "to enter",
                    io = "entro",
                    tu = "entri",
                    luiLei = "entra",
                    noi = "entriamo",
                    voi = "entrate",
                    loro = "entrano"
                },
                new VerbEntry
                {
                    infinitive = "vedere",
                    infinitiveEN = "to see",
                    io = "vedo",
                    tu = "vedi",
                    luiLei = "vede",
                    noi = "vediamo",
                    voi = "vedete",
                    loro = "vedono"
                },
                new VerbEntry
                {
                    infinitive = "ricevere",
                    infinitiveEN = "to receive",
                    io = "ricevo",
                    tu = "ricevi",
                    luiLei = "riceve",
                    noi = "riceviamo",
                    voi = "ricevete",
                    loro = "ricevono"
                },
                new VerbEntry
                {
                    infinitive = "desiderare",
                    infinitiveEN = "to wish / to desire",
                    io = "desidero",
                    tu = "desideri",
                    luiLei = "desidera",
                    noi = "desideriamo",
                    voi = "desiderate",
                    loro = "desiderano"
                },
                new VerbEntry
                {
                    infinitive = "ordinare",
                    infinitiveEN = "to order",
                    io = "ordino",
                    tu = "ordini",
                    luiLei = "ordina",
                    noi = "ordiniamo",
                    voi = "ordinate",
                    loro = "ordinano"
                },
                new VerbEntry
                {
                    infinitive = "spendere",
                    infinitiveEN = "to spend",
                    io = "spendo",
                    tu = "spendi",
                    luiLei = "spende",
                    noi = "spendiamo",
                    voi = "spendete",
                    loro = "spendono"
                },
            };

            database.entries = entries.ToArray();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CreateVerbConjugationDatabase] Asset popolato: {path} con {entries.Count} verbi");
            EditorUtility.RevealInFinder(path);
        }
    }
}
#endif
