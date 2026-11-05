using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class FistDynamicMoveProvider : DynamicMoveProvider
{
    [Range(0f, 1f)]
    public float fistThreshold = 0.7f;

    private XRHandSubsystem _handSubsystem;
    private readonly List<XRHandSubsystem> _subsystemsCache = new List<XRHandSubsystem>();

    protected override void Awake()
    {
        base.Awake();
        TryAcquireHandSubsystem();
    }

    // Il sottosistema mani può non essere ancora registrato quando Awake() gira
    // (capita spesso su headset VR): a differenza della versione precedente, che
    // lo cercava una sola volta e restava con un riferimento null per sempre se
    // non lo trovava subito, qui ritentiamo finché non è disponibile — stesso
    // approccio già usato da HandMap/HandFingerLinker/HandTrackingLinker.
    void TryAcquireHandSubsystem()
    {
        if (_handSubsystem != null && _handSubsystem.running) return;

        SubsystemManager.GetSubsystems(_subsystemsCache);
        _handSubsystem = _subsystemsCache.Count > 0 ? _subsystemsCache[0] : null;
    }

    protected override Vector3 ComputeDesiredMove(Vector2 input)
    {
        TryAcquireHandSubsystem();

        Vector2 fistInput = Vector2.zero;
        if (_handSubsystem != null)
        {
            bool leftFist = IsPalmUp(_handSubsystem.leftHand);
            bool rightFist = IsPalmUp(_handSubsystem.rightHand);
            if (leftFist || rightFist)
                fistInput = Vector2.up;
        }

        // Passa al DynamicMoveProvider il nostro input invece di quello dei controller
        return base.ComputeDesiredMove(fistInput);
    }

    bool IsPalmUp(XRHand hand)
    {
        if (!hand.isTracked) return false;

        try
        {
            // Controlla che il palmo sia rivolto verso l'alto
            // usando il joint del palmo
            var palmJoint = hand.GetJoint(XRHandJointID.Palm);
            if (!palmJoint.TryGetPose(out Pose palmPose)) return false;

            // Il palmo � rivolto verso l'alto se il suo "up" punta verso il cielo
            float dot = Vector3.Dot(palmPose.rotation * Vector3.down, Vector3.up);
            return dot > 0.7f; // circa 45 gradi di tolleranza
        }
        catch
        {
            return false;
        }
    }
}