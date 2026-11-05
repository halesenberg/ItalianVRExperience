using UnityEngine;

/// <summary>
/// Ancora questo GameObject (il canvas di dialogo) a un NPC — di solito
/// Vicina — invece che alla camera: appare vicino a lei, non segue il player.
/// La ROTAZIONE però guarda sempre verso "facingReference" (di solito Main
/// Camera), così il testo resta leggibile anche quando lei si gira durante
/// la sequenza di camminata — se facingReference non è assegnato, usa il
/// vecchio comportamento (si orienta rispetto a "target" stesso).
/// </summary>
public class CanvasFollow : MonoBehaviour
{
    [Tooltip("Trascina qui Vicina (il GameObject radice, NON un bone animato tipo 'Merged'/'mixamorig:Hips').")]
    public Transform target;

    [Tooltip("Opzionale ma consigliato: Main Camera. Il canvas resta ancorato a 'target' " +
             "ma la rotazione guarda sempre verso questo, per restare leggibile.")]
    public Transform facingReference;

    [Tooltip("Offset rispetto a target, in coordinate LOCALI di target (es. 0,1.8,0 = sopra la testa).")]
    public Vector3 localOffset = new Vector3(0f, 1.8f, 0.3f);

    public float smoothSpeed = 5f;

    void Update()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + target.TransformDirection(localOffset);
        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * smoothSpeed);

        Transform lookTarget = facingReference != null ? facingReference : target;
        transform.LookAt(lookTarget.position);
        transform.Rotate(0, 180, 0);
    }
}