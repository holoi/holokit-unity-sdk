// SPDX-FileCopyrightText: Copyright 2023 Holo Interactive <dev@holoi.com>
// SPDX-FileContributor: Yuchen Zhang <yuchen@holoi.com>
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace HoloInteractive.XR.HoloKit.iOS
{
    [Serializable]
    public enum MaxHandCount
    {
        One = 1,
        Two = 2
    }

    public class AppleVisionHandPoseDetector : IDisposable
    {
        IntPtr m_Ptr;

        static Dictionary<IntPtr, AppleVisionHandPoseDetector> s_Detectors = new();

        int m_HandCount;

        List<Dictionary<JointName, Vector2>> m_HandPoses2D;

        List<Dictionary<JointName, Vector3>> m_HandPoses3D;

        public int HandCount => m_HandCount;

        public List<Dictionary<JointName, Vector2>> HandPoses2D => m_HandPoses2D;

        public List<Dictionary<JointName, Vector3>> HandPoses3D => m_HandPoses3D;

        public event Action OnHandPose2DUpdated;

        public event Action OnHandPose3DUpdated;

        public event Action OnHandPoseLost;

        public AppleVisionHandPoseDetector(MaxHandCount maxHandCount)
        {
            List<XRSessionSubsystem> xrSessionSubsystems = new();
            SubsystemManager.GetSubsystems(xrSessionSubsystems);
            if (xrSessionSubsystems.Count == 0)
            {
                Debug.LogWarning("Cannot find XRSessionSubsystem");
                return;
            }
            XRSessionSubsystem xrSessionSubsystem = xrSessionSubsystems[0];

            m_Ptr = InitWithARSession(xrSessionSubsystem.nativePtr, (int)maxHandCount);
            m_HandCount = 0;
            m_HandPoses2D = new();
            m_HandPoses3D = new();
            for (int i = 0; i < 2; i++)
            {
                m_HandPoses2D.Add(new Dictionary<JointName, Vector2>());
                m_HandPoses3D.Add(new Dictionary<JointName, Vector3>());
            }
            RegisterCallbacks(m_Ptr, OnHandPose2DUpdatedCallback, OnHandPose3DUpdatedCallback);

            s_Detectors[m_Ptr] = this;
        }

        public void ProcessCurrentFrame2D()
        {
            ProcessCurrentFrame2D(m_Ptr);
        }

        public void ProcessCurrentFrame3D()
        {
            ProcessCurrentFrame3D(m_Ptr);
        }

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                NativeApi.CFRelease(ref m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        [DllImport("__Internal", EntryPoint = "HoloInteractiveHoloKit_AppleVisionHandPoseDetector_initWithARSession")]
        static extern IntPtr InitWithARSession(IntPtr arSessionPtr, int maximumHandCount);

        [DllImport("__Internal", EntryPoint = "HoloInteractiveHoloKit_AppleVisionHandPoseDetector_registerCallbacks")]
        static extern IntPtr RegisterCallbacks(IntPtr self, Action<IntPtr, int, IntPtr> onHandPose2DUpdatedCallback, Action<IntPtr, int, IntPtr> onHandPose3DUpdatedCallback);

        [DllImport("__Internal", EntryPoint = "HoloInteractiveHoloKit_AppleVisionHandPoseDetector_processCurrentFrame2D")]
        static extern bool ProcessCurrentFrame2D(IntPtr self);

        [DllImport("__Internal", EntryPoint = "HoloInteractiveHoloKit_AppleVisionHandPoseDetector_processCurrentFrame3D")]
        static extern bool ProcessCurrentFrame3D(IntPtr self);

        [AOT.MonoPInvokeCallback(typeof(Action<IntPtr, int, IntPtr>))]
        static void OnHandPose2DUpdatedCallback(IntPtr detectorPtr, int handCount, IntPtr resultsPtr)
        {
            if (s_Detectors.TryGetValue(detectorPtr, out AppleVisionHandPoseDetector detector))
            {
                if (handCount == 0)
                {
                    if (detector.m_HandCount > 0)
                    {
                        detector.m_HandCount = handCount;
                        detector.OnHandPoseLost?.Invoke();
                    }
                    return;
                }
                detector.m_HandCount = handCount;

                int length = 2 * 21 * handCount;
                float[] results = new float[length];
                Marshal.Copy(resultsPtr, results, 0, length);
                Debug.Log("cici");
                for (int i = 0; i < handCount; i++)
                {
                    for (int j = 0; j < 21; j++)
                    {
                        detector.m_HandPoses2D[i][(JointName)j] = new Vector2(results[i * 2 * 21 + j * 2], results[i * 2 * 21 + j * 2 + 1]);
                    }
                }
                detector.OnHandPose2DUpdated?.Invoke();
            }
        }

        [AOT.MonoPInvokeCallback(typeof(Action<IntPtr, int, IntPtr>))]
        static void OnHandPose3DUpdatedCallback(IntPtr detectorPtr, int handCount, IntPtr resultsPtr)
        {
            if (s_Detectors.TryGetValue(detectorPtr, out AppleVisionHandPoseDetector detector))
            {
                if (handCount == 0)
                {
                    if (detector.m_HandCount > 0)
                    {
                        detector.m_HandCount = handCount;
                        detector.OnHandPoseLost?.Invoke();
                    }
                    return;
                }
                detector.m_HandCount = handCount;

                int length = 3 * 21 * handCount;
                float[] results = new float[length];
                Marshal.Copy(resultsPtr, results, 0, length);
                for (int i = 0; i < handCount; i++)
                {
                    for (int j = 0; j < 21; j++)
                    {
                        detector.m_HandPoses3D[i][(JointName)j] = new Vector3(results[i * 3 * 21 + j * 3], results[i * 3 * 21 + j * 3 + 1], results[i * 3 * 21 + j * 3 + 2]);
                    }
                }
                detector.OnHandPose3DUpdated?.Invoke();
            }
        }
    }
}
