using UnityEngine;
using UnityEngine.Animations.Rigging;

public class FingerSync : MonoBehaviour
{
    // Il nome esatto dell'osso della mano XR da seguire (es: "IndexProximal")
    public string targetBoneName;
    private Transform targetTransform;
    private MultiRotationConstraint constraint;

    void Start()
    {
        constraint = GetComponent<MultiRotationConstraint>();
        // Cerchiamo l'oggetto nella scena che ha il nome della mano XR
        GameObject handRoot = GameObject.Find("LeftHand(Clone)"); // O il nome del tuo visualizer

        if (handRoot != null)
        {
            // Cerca ricorsivamente l'osso con il nome indicato
            targetTransform = FindDeepChild(handRoot.transform, targetBoneName);

            if (targetTransform != null)
            {
                var sources = constraint.data.sourceObjects;
                sources.SetTransform(0, targetTransform);
                constraint.data.sourceObjects = sources;
            }
        }
    }

    // Funzione per cercare dentro le sottocartelle della gerarchia
    Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }
}