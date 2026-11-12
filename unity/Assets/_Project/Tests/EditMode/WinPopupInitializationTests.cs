#if UNITY_EDITOR
using System;
using System.Reflection;
using Meowtosis.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Tests
{
    public class WinPopupInitializationTests
    {
        [Test]
        public void InitializingBeforeAwakePreservesRewardPositionAndBindsButtonsOnce()
        {
            var root = new GameObject("Inactive win popup", typeof(RectTransform));
            root.SetActive(false);
            try
            {
                var popup = root.AddComponent<WinPopup>();
                var reward = Child(root, "Reward");
                reward.anchoredPosition = new Vector2(0f, -160f);
                var next = Child(root, "Continue").gameObject.AddComponent<Button>();
                var back = Child(root, "Back").gameObject.AddComponent<Button>();
                Set(popup, "reward", reward);
                Set(popup, "continueButton", next);
                Set(popup, "backButton", back);
                var clicks = 0;
                popup.BackClicked += () => clicks++;

                Call(popup, "InitializeOnce");
                reward.anchoredPosition = Vector2.zero;
                Call(popup, "Awake");
                back.onClick.Invoke();
                Assert.That(clicks, Is.Zero, "Navigation must wait until the reward is visible");
                Set(popup, "rewardReady", true);
                back.onClick.Invoke();
                Set(popup, "collecting", true);
                back.onClick.Invoke();

                Assert.That(Get<Vector2>(popup, "rewardHome"), Is.EqualTo(new Vector2(0f, -160f)),
                    "Awake must not capture an already reset reward position");
                Assert.That(clicks, Is.EqualTo(1), "Bind listeners once and block Back during collection");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static RectTransform Child(GameObject parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent.transform, false);
            return (RectTransform)child.transform;
        }

        private static void Set(WinPopup popup, string field, object value) =>
            typeof(WinPopup).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(popup, value);

        private static T Get<T>(WinPopup popup, string field) =>
            (T)typeof(WinPopup).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(popup);

        private static void Call(WinPopup popup, string method) =>
            typeof(WinPopup).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(popup, null);
    }
}
#endif
