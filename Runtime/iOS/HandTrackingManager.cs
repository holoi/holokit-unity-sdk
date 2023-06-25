// SPDX-FileCopyrightText: Copyright 2023 Holo Interactive <dev@holoi.com>
// SPDX-FileContributor: Yuchen Zhang <yuchen@holoi.com>
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace HoloInteractive.XR.HoloKit.iOS
{
    [RequireComponent(typeof(AppleVisionHandPoseManager))]
    public class HandTrackingManager : MonoBehaviour
    {
        AppleVisionHandPoseManager m_HandPoseManager;

        AROcclusionManager m_AROcclusionManager;

        Camera m_MainCamera;

        List<Dictionary<JointName, GameObject>> m_Hands = new();

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            if (transform.childCount > 0)
                return;

            gameObject.name = "Hand Tracking Manager";

            for (int i = 0; i < 2; i++)
            {
                GameObject hand = new($"Hand {i}");
                hand.transform.SetParent(transform);
                for (int j = 0; j < 21; j++)
                {
                    GameObject joint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    joint.name = ((JointName)j).ToString();
                    joint.transform.localScale = new(0.01f, 0.01f, 0.01f);
                    joint.transform.SetParent(hand.transform);
                }
            }
        }

        private void Start()
        {
            m_AROcclusionManager = FindObjectOfType<AROcclusionManager>();
            if (m_AROcclusionManager == null)
            {
                Debug.LogWarning("Failed to find AROcclusionManager. You cannot use hand tracking without AROcclusionManager.");
                return;
            }

            m_MainCamera = m_AROcclusionManager.gameObject.GetComponent<Camera>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var hand = transform.GetChild(i);
                Dictionary<JointName, GameObject> dict = new();
                m_Hands.Add(dict);
                for (int j = 0; j < hand.childCount; j++)
                {
                    var joint = hand.GetChild(j);
                    m_Hands[i].Add((JointName)j, joint.gameObject);
                }
            }

            m_HandPoseManager = GetComponent<AppleVisionHandPoseManager>();
            m_HandPoseManager.OnHandPoseUpdated += OnHandPoseUpdated;
        }

        private void OnHandPoseUpdated()
        {
            if (m_HandPoseManager.HandCount > 0)
            {
                using (EnvironmentDepthImage depthImage = new(m_AROcclusionManager))
                {
                    if (depthImage == null)
                        return;

                    for (int i = 0; i < m_HandPoseManager.HandCount; i++)
                    {
                        var hand = m_Hands[i];
                        for (int j = 0; j < hand.Count; j++)
                        {
                            JointName jointName = (JointName)j;
                            Vector2 location = m_HandPoseManager.GetHandJointLocation(i, jointName);
                            float depth = depthImage.GetDepth(location);
                            //float depth = 0.3f;
                            Vector3 worldPos = m_HandPoseManager.UnprojectScreenPoint(location, depth);
                            hand[jointName].transform.position = worldPos;
                        }
                    }
                }
            }
        }
    }
}
