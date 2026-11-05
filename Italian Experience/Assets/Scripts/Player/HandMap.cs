using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.SubsystemsImplementation; // Necessario per alcuni riferimenti ai sottosistemi

public class HandMap : MonoBehaviour
{
    [Header("Configurazione Mano")]
    public Handedness handToTrack; // Left o Right

    [Header("Riferimenti Avatar")]
    public Transform avatarWrist;

    [Header("Mapping Dita")]
    public List<FingerMapping> fingerMaps = new List<FingerMapping>();

    private XRHandSubsystem m_Subsystem;

    [System.Serializable]
    public struct FingerMapping
    {
        public Transform avatarBone;
        public XRHandJointID xrJointId;
    }

    void Update()
    {
        // 1. Recupero il sottosistema se non è già memorizzato
        if (m_Subsystem == null || !m_Subsystem.running)
        {
            var handSubsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(handSubsystems);

            if (handSubsystems.Count > 0)
            {
                m_Subsystem = handSubsystems[0];
            }
            else return; // Nessun sistema di hand tracking trovato
        }

        // 2. Ottengo i dati della mano specifica
        var hand = (handToTrack == Handedness.Left) ? m_Subsystem.leftHand : m_Subsystem.rightHand;

        // 3. Se la mano è tracciata, applico i movimenti
        if (hand.isTracked)
        {
            // Aggiorno il polso (Posizione + Rotazione)
            UpdateJoint(hand, XRHandJointID.Wrist, avatarWrist, true);

            // Aggiorno le dita (Solo Rotazione per non deformare il modello)
            foreach (var map in fingerMaps)
            {
                UpdateJoint(hand, map.xrJointId, map.avatarBone, false);
            }
        }
    }

    void UpdateJoint(XRHand hand, XRHandJointID jointId, Transform avatarTransform, bool updatePosition)
    {
        if (avatarTransform == null) return;

        // RECUPERO SICURO DEL JOINT
        var joint = hand.GetJoint(jointId);

        // Controlliamo che il joint sia valido e tracciato PRIMA di chiedere la Pose
        // Usiamo l'operatore && per sicurezza
        if (joint.id != XRHandJointID.Invalid && joint.TryGetPose(out Pose pose))
        {
            avatarTransform.rotation = pose.rotation;

            if (updatePosition)
            {
                avatarTransform.position = pose.position;
            }
        }
    }
}