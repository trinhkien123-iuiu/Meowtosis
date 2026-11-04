using UnityEngine;

namespace Meowtosis.Presentation
{
    // Mèo lúc rảnh: cứ mỗi khoảng thời gian có thể chọn một con đang thức làm một động tác (chớp mắt, ngó quanh,
    // quay đầu, ngáp, vươn vai, dụi con bên cạnh). Tỉ lệ và nhịp như prototype, chỉnh được trong Inspector.
    public sealed class BoardIdle : MonoBehaviour
    {
        [SerializeField] private BoardView board;
        [SerializeField] private PieceMotions motions;
        [SerializeField] private float intervalSeconds = 1.3f;
        [SerializeField, Range(0f, 1f)] private float skipChance = 0.3f;

        [Header("Tỉ lệ từng động tác")]
        [SerializeField] private float blinkWeight = 0.34f;
        [SerializeField] private float lookWeight = 0.22f;
        [SerializeField] private float turnWeight = 0.12f;
        [SerializeField] private float yawnWeight = 0.11f;
        [SerializeField] private float stretchWeight = 0.11f;
        [SerializeField] private float nuzzleWeight = 0.1f;

        [Header("Thời lượng (giây)")]
        [SerializeField] private float blinkSeconds = 0.16f;
        [SerializeField] private float turnSeconds = 2.25f;
        [SerializeField] private float yawnSeconds = 1.4f;
        [SerializeField] private float nuzzleSeconds = 1.2f;

        private float timeLeft;

        private void Update()
        {
            if ((timeLeft -= Time.deltaTime) > 0f) return;
            timeLeft = intervalSeconds;
            if (!board.IdleEnabled || Random.value < skipChance) return;
            var cat = board.RandomRestingCat();
            if (cat != null) Act(cat);
        }

        private void Act(PieceView cat)
        {
            var side = Random.value < 0.5f ? Vector2.left : Vector2.right;
            var roll = Random.value * (blinkWeight + lookWeight + turnWeight + yawnWeight + stretchWeight + nuzzleWeight);
            if ((roll -= blinkWeight) < 0f) cat.ShowFaceFor(FaceKind.Blink, blinkSeconds);
            else if ((roll -= lookWeight) < 0f) cat.PlayMotion(motions.Look, side);
            else if ((roll -= turnWeight) < 0f) cat.FlipFor(turnSeconds);
            else if ((roll -= yawnWeight) < 0f)
            {
                cat.ShowFaceFor(FaceKind.Yawn, yawnSeconds);
                cat.PlayMotion(motions.Yawn);
            }
            else if ((roll -= stretchWeight) < 0f) cat.PlayMotion(motions.Stretch);
            else
            {
                cat.ShowFaceFor(FaceKind.Blink, nuzzleSeconds);
                cat.PlayMotion(motions.Nuzzle, side);
            }
        }
    }
}
