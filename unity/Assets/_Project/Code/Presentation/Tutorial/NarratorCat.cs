using System.Collections;
using Spine.Unity;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Mèo cam dẫn chuyện cạnh bong bóng hướng dẫn (Spine IP_ingame, cùng con với avatar ở thanh trên).
    // Mỗi câu mới diễn một động tác theo tâm trạng rồi về dáng chờ; hiện và ẩn cùng bong bóng.
    public sealed class NarratorCat : MonoBehaviour
    {
        [SerializeField] private SkeletonGraphic skeleton;
        [SerializeField] private string idleAnimation = "ip_idle";
        [SerializeField] private string explainAnimation = "ip_thinking";
        [SerializeField] private string warnStartAnimation = "ip_worried_start";
        [SerializeField] private string warnLoopAnimation = "ip_worried_loop";
        [SerializeField] private string praiseAnimation = "ip_cheer";
        [SerializeField] private float popSeconds = 0.3f;

        private string line;
        private Vector3 home = Vector3.zero;

        // `text` để biết đã sang câu mới: câu mới thì diễn lại động tác, cùng câu thì giữ nguyên.
        public void Say(string text, NarratorMood mood)
        {
            if (home == Vector3.zero) home = transform.localScale;
            var appearing = !gameObject.activeSelf;
            gameObject.SetActive(true);
            if (!appearing && line == text) return;
            line = text;
            var state = skeleton.AnimationState;
            if (state == null) return;
            switch (mood)
            {
                case NarratorMood.Warn:
                    state.SetAnimation(0, warnStartAnimation, false);
                    state.AddAnimation(0, warnLoopAnimation, true, 0f);
                    break;
                case NarratorMood.Praise:
                    state.SetAnimation(0, praiseAnimation, false);
                    state.AddAnimation(0, idleAnimation, true, 0f);
                    break;
                default:
                    state.SetAnimation(0, explainAnimation, false);
                    state.AddAnimation(0, idleAnimation, true, 0f);
                    break;
            }
            if (appearing) StartCoroutine(Pop());
        }

        public void Hide()
        {
            line = null;
            gameObject.SetActive(false);
        }

        // Bật lên từ dưới: dãn theo chiều dọc rồi lắng lại.
        private IEnumerator Pop()
        {
            for (var time = 0f; time < popSeconds; time += Time.unscaledDeltaTime)
            {
                var t = time / popSeconds;
                var bounce = t < 0.6f ? Mathf.Lerp(0f, 1.12f, t / 0.6f) : Mathf.Lerp(1.12f, 1f, (t - 0.6f) / 0.4f);
                transform.localScale = new Vector3(home.x, home.y * bounce, home.z);
                yield return null;
            }
            transform.localScale = home;
        }

        private void OnDisable()
        {
            if (home != Vector3.zero) transform.localScale = home;
        }
    }
}
