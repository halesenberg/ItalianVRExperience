using UnityEngine;
using UnityEngine.XR.Hands;
using System.Collections.Generic;

public class HandTrackingLinker : MonoBehaviour
{
    public Handedness manoDaSeguire;
    public Transform targetIK;
    private XRHandSubsystem m_HandSubsystem;

    void Update()
    {
        if (targetIK == null) return;

        if (m_HandSubsystem == null)
        {
            var subsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            if (subsystems.Count > 0) m_HandSubsystem = subsystems[0];
            return;
        }

        var hand = (manoDaSeguire == Handedness.Left) ? m_HandSubsystem.leftHand : m_HandSubsystem.rightHand;

        if (hand.isTracked)
        {
            if (hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out Pose wPose))
            {
                // POSIZIONE MONDIALE PURA
                targetIK.position = wPose.position;
                targetIK.rotation = wPose.rotation;
            }
        }
    }
}