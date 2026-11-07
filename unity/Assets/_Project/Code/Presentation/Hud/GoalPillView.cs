using System.Collections;
using Meowtosis.Gameplay;
using TMPro;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Viên gỗ mục tiêu dưới thanh sao: icon khối gỗ + số ô đã lấp, và icon hộp + số hộp đúng màu nếu màn có hộp.
    // Không có chữ, nên nhìn là hiểu. Mỗi lần tiến độ tăng thì viên gỗ nảy lên một nhịp.
    public sealed class GoalPillView : MonoBehaviour
    {
        [SerializeField] private TMP_Text cellsText;
        [Tooltip("Icon hộp và số hộp, ẩn khi màn không có hộp.")]
        [SerializeField] private GameObject boxGroup;
        [SerializeField] private TMP_Text boxesText;
        [Tooltip("Chữ đổi sang màu này khi mục tiêu đó đã xong.")]
        [SerializeField] private Color doneColor = new Color(0.62f, 1f, 0.45f);
        [SerializeField] private float punchScale = 1.15f;
        [SerializeField] private float punchSeconds = 0.25f;

        private Color normalColor;
        private int shownCells = -1;
        private int shownBoxes = -1;

        public void Show(GoalProgress progress, bool instantly)
        {
            cellsText.text = GameTexts.Count(progress.FilledCells, progress.CellsToFill);
            cellsText.color = progress.FilledCells >= progress.CellsToFill ? doneColor : normalColor;

            var hasBoxes = progress.BoxCount > 0;
            boxGroup.SetActive(hasBoxes);
            boxesText.gameObject.SetActive(hasBoxes);
            if (hasBoxes)
            {
                boxesText.text = GameTexts.Count(progress.BoxesDone, progress.BoxCount);
                boxesText.color = progress.BoxesDone >= progress.BoxCount ? doneColor : normalColor;
            }

            var grew = progress.FilledCells > shownCells || progress.BoxesDone > shownBoxes;
            if (!instantly && grew && shownCells >= 0 && isActiveAndEnabled)
            {
                StopAllCoroutines();
                StartCoroutine(Punch());
            }
            shownCells = progress.FilledCells;
            shownBoxes = progress.BoxesDone;
        }

        private void Awake() => normalColor = cellsText.color;

        private IEnumerator Punch()
        {
            for (var time = 0f; time < punchSeconds; time += Time.unscaledDeltaTime)
            {
                var wave = Mathf.Sin(time / punchSeconds * Mathf.PI);
                transform.localScale = Vector3.one * Mathf.Lerp(1f, punchScale, wave);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }

        private void OnDisable() => transform.localScale = Vector3.one;
    }
}
