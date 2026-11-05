using UnityEngine;
using UnityEngine.XR.Hands;
using System.Collections.Generic;

public class HandFingerLinker : MonoBehaviour
{
    public Handedness manoDaSeguire;

    [Header("Assegna le ossa del tuo Avatar")]
    public Transform thumb1, thumb2, thumb3;
    public Transform index1, index2, index3;
    public Transform middle1, middle2, middle3;
    public Transform ring1, ring2, ring3;
    public Transform pinky1, pinky2, pinky3;

    private XRHandSubsystem handSubsystem;

    void Update()
    {
        if (handSubsystem == null)
        {
            var subsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            if (subsystems.Count > 0) handSubsystem = subsystems[0];
            return;
        }

        var hand = (manoDaSeguire == Handedness.Left) ? handSubsystem.leftHand : handSubsystem.rightHand;

        if (hand.isTracked)
        {
            // Passiamo la "hand" alla funzione MapJoint
            MapJoint(hand, XRHandJointID.ThumbMetacarpal, thumb1);
            MapJoint(hand, XRHandJointID.ThumbProximal, thumb2);
            MapJoint(hand, XRHandJointID.ThumbDistal, thumb3);

            MapJoint(hand, XRHandJointID.IndexProximal, index1);
            MapJoint(hand, XRHandJointID.IndexIntermediate, index2);
            MapJoint(hand, XRHandJointID.IndexDistal, index3);

            MapJoint(hand, XRHandJointID.MiddleProximal, middle1);
            MapJoint(hand, XRHandJointID.MiddleIntermediate, middle2);
            MapJoint(hand, XRHandJointID.MiddleDistal, middle3);

            MapJoint(hand, XRHandJointID.RingProximal, ring1);
            MapJoint(hand, XRHandJointID.RingIntermediate, ring2);
            MapJoint(hand, XRHandJointID.RingDistal, ring3);

            MapJoint(hand, XRHandJointID.LittleProximal, pinky1);
            MapJoint(hand, XRHandJointID.LittleIntermediate, pinky2);
            MapJoint(hand, XRHandJointID.LittleDistal, pinky3);
        }
    }

    void MapJoint(XRHand hand, XRHandJointID jointID, Transform boneTransform)
    {
        // Qui usiamo hand.GetJoint per ottenere la posa specifica di ogni dito
        if (boneTransform != null && hand.GetJoint(jointID).TryGetPose(out Pose jointPose))
        {
            // CORREZIONE 180 GRADI: 
            // Se le dita sparano all'esterno, usiamo questo offset.
            // Se non basta 180 su Y, proveremo (180, 0, 0) o (0, 0, 180)
            Quaternion correction = Quaternion.Euler(0, 180, 0);

            boneTransform.localRotation = jointPose.rotation * correction;
        }
    }
}