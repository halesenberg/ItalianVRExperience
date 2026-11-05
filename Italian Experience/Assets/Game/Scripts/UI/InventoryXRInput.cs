using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Hands;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Bellavalle.UI;

namespace Bellavalle.UI
{
    public class InventoryXRInput : MonoBehaviour
    {
        // Presa automaticamente a ogni scena — niente più da trascinare in Inspector.
        InventoryUI inventoryUI;

        [Header("Rilevamento — palmi in giù")]
        [Range(0f, 1f)]
        [SerializeField] float palmDownThreshold = 0.7f;

        [Header("Rilevamento — sovrapposizione polsi")]
        [SerializeField] float overlapDistance = 0.12f;

        [Header("Debug")]
        [SerializeField] bool logToConsole = true;

        XRHandSubsystem _handSubsystem;
        bool _gestureActive;

        void Awake()
        {
            var subsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            if (subsystems.Count > 0)
                _handSubsystem = subsystems[0];
            else
                Debug.LogWarning("[InventoryXRInput] XRHandSubsystem non trovato!");
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            RefreshSceneReferences();
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode) => RefreshSceneReferences();

        void RefreshSceneReferences()
        {
            inventoryUI = FindFirstObjectByType<InventoryUI>();
            if (logToConsole)
                Debug.Log($"[InventoryXRInput] inventoryUI di scena: {(inventoryUI != null ? "trovato" : "assente")}");
        }

        void Update()
        {
            if (_handSubsystem != null)
            {
                bool isGestureNow = IsWatchCheckGesture();

                if (isGestureNow && !_gestureActive)
                {
                    _gestureActive = true;
                    if (logToConsole) Debug.Log("[InventoryXRInput] Gesto rilevato → toggle zaino.");
                    inventoryUI?.ToggleInventory();
                }
                else if (!isGestureNow && _gestureActive)
                {
                    _gestureActive = false;
                }
            }

            if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
            {
                if (logToConsole) Debug.Log("[TEST] I premuto");
                inventoryUI?.ToggleInventory();
            }
        }

        bool IsWatchCheckGesture()
        {
            var left = _handSubsystem.leftHand;
            var right = _handSubsystem.rightHand;
            if (!left.isTracked || !right.isTracked) return false;

            try
            {
                if (!IsPalmDown(left) || !IsPalmDown(right)) return false;

                var leftWristJoint = left.GetJoint(XRHandJointID.Wrist);
                var rightWristJoint = right.GetJoint(XRHandJointID.Wrist);
                if (!leftWristJoint.TryGetPose(out Pose leftWristPose)) return false;
                if (!rightWristJoint.TryGetPose(out Pose rightWristPose)) return false;

                return Vector3.Distance(leftWristPose.position, rightWristPose.position) < overlapDistance;
            }
            catch { return false; }
        }

        bool IsPalmDown(XRHand hand)
        {
            var palmJoint = hand.GetJoint(XRHandJointID.Palm);
            if (!palmJoint.TryGetPose(out Pose palmPose)) return false;

            float dot = Vector3.Dot(palmPose.rotation * Vector3.down, Vector3.down);
            return dot > palmDownThreshold;
        }
    }
}