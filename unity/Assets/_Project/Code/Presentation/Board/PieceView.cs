using Meowtosis.Domain;
using UnityEngine;

namespace Meowtosis.Presentation
{
    public enum FaceKind
    {
        None,
        Blink,
        Sad,
        Yawn,
    }

    // Prefab một quân trên bàn (Prefabs/Board/Piece). Gốc trượt giữa các ô; lớp Visual bên trong diễn chuyển động
    // (PieceMotions) và thở nhẹ khi đứng yên. Mặt mèo vẽ đè bằng lớp Face vì ảnh Shikacat không tách mắt, miệng.
    public sealed class PieceView : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer face;
        [SerializeField] private SpriteRenderer shadow;
        [SerializeField] private GameObject sleepMarks;
        [SerializeField] private GameObject sweatDrop;
        [Tooltip("Bề rộng ảnh mèo, tính bằng ô (prototype: 70/90).")]
        [SerializeField] private float catWidth = 0.778f;
        [Tooltip("Ảnh mèo cao hơn tâm ô một chút (prototype: 1/90 ô).")]
        [SerializeField] private float catLift = 0.011f;
        [Tooltip("Bề rộng hình vi khuẩn, virus (vẽ trên khung 1,5 ô).")]
        [SerializeField] private float placeholderWidth = 1.5f;
        [SerializeField] private float slideSpeed = 10f;
        [SerializeField] private float breathSeconds = 2.8f;
        [SerializeField] private float breathWide = 0.04f;
        [SerializeField] private float breathFlat = 0.035f;
        [Tooltip("Mặt chớp mắt, ngáp, buồn hiện và tắt mượt trong chừng này giây.")]
        [SerializeField] private float faceFadeSeconds = 0.07f;
        [Tooltip("Nghiêng tối đa khi kéo hết cỡ: lệch bao nhiêu ô và nghiêng bao nhiêu độ.")]
        [SerializeField] private float leanDistance = 0.14f;
        [SerializeField] private float leanDegrees = 10f;
        [SerializeField] private float leanSpeed = 18f;
        [SerializeField] private Color eatenColor = new Color(0.72f, 0.42f, 1f);

        private BoardArt art;
        private Vector3 targetPosition;
        private PieceMotion motion;
        private Vector2 motionDirection;
        private float motionDelay;
        private float motionTime;
        private float faceTimeLeft;
        private float flipTimeLeft;
        private float breathPhase;
        private bool hasMood;
        private bool showsHappyImage;
        private float faceAlpha;
        private float faceTargetAlpha;
        private Vector2 leanTarget;
        private Vector2 lean;

        public Transform Visual => visual;

        public Piece Piece { get; private set; }

        public bool IsResting => motion == null && faceTimeLeft <= 0f && flipTimeLeft <= 0f;

        public void Place(Vector3 position)
        {
            transform.localPosition = position;
            targetPosition = position;
        }

        public void Show(Piece piece, BoardArt boardArt, bool isHappy)
        {
            Piece = piece;
            art = boardArt;
            showsHappyImage = isHappy;
            if (isHappy && !hasMood)
            {
                faceTimeLeft = 0f;
                SetFace(FaceKind.None);
            }
            breathPhase = piece.Id * 0.73f % breathSeconds;
            if (piece.IsVirus) SetBody(art.VirusSprite(piece.Facing), placeholderWidth, 0f);
            else if (piece.IsCat) SetBody(art.CatSprite(piece.Species, isHappy), catWidth, catLift);
            else SetBody(art.Germ, placeholderWidth, 0f);
            sleepMarks.SetActive(piece.IsAsleep);
        }

        public void MoveTo(Vector3 position) => targetPosition = position;

        // Nghiêng người theo ngón tay đang kéo: `amount` từ 0 tới 1, (0, 0) là đứng thẳng.
        public void Lean(Vector2 amount) => leanTarget = amount;

        // Diễn một chuyển động; `direction` là hướng của nước đi (bỏ trống với chuyển động không theo hướng).
        public void PlayMotion(PieceMotion newMotion, Vector2 direction = default, float delay = 0f)
        {
            motion = newMotion;
            motionDirection = direction;
            motionDelay = delay;
            motionTime = 0f;
        }

        public void PlayThenDestroy(PieceMotion lastMotion)
        {
            PlayMotion(lastMotion);
            sleepMarks.SetActive(false);
            Destroy(gameObject, lastMotion.TotalSeconds);
        }

        // Vẻ mặt tạm thời (chớp mắt, ngáp) rồi tự về bình thường.
        public void ShowFaceFor(FaceKind kind, float seconds)
        {
            if (hasMood || !Piece.IsCat || Piece.IsAsleep || showsHappyImage) return;
            SetFace(kind);
            faceTimeLeft = seconds;
        }

        // Vẻ mặt buồn và giọt mồ hôi khi thua, giữ tới khi ClearMood.
        public void ShowSadMood()
        {
            if (!Piece.IsCat || Piece.IsAsleep) return;
            // Mắt trái tim của ảnh mèo vui nằm khác chỗ mặt buồn: quay về ảnh thường trước khi vẽ mặt buồn.
            if (showsHappyImage) Show(Piece, art, false);
            hasMood = true;
            faceTimeLeft = 0f;
            SetFace(FaceKind.Sad);
            sweatDrop.SetActive(true);
        }

        public void ClearMood()
        {
            if (!hasMood) return;
            hasMood = false;
            SetFace(FaceKind.None);
            sweatDrop.SetActive(false);
        }

        // Quay đầu sang hướng ngược lại một lúc.
        public void FlipFor(float seconds) => flipTimeLeft = seconds;

        private void SetBody(Sprite sprite, float width, float lift)
        {
            SpriteSizing.SetSprite(body, sprite, width);
            body.transform.localPosition = new Vector3(0f, lift, 0f);
        }

        // Đổi mặt: hiện thì mờ dần vào, tắt thì mờ dần ra (giữ nguyên hình cũ tới khi mờ hết) để không bị giật.
        private void SetFace(FaceKind kind)
        {
            faceTargetAlpha = kind == FaceKind.None ? 0f : 1f;
            if (kind != FaceKind.None) face.sprite = art.FaceSprite(Piece.Species, kind);
        }

        private void Update()
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPosition, slideSpeed * Time.deltaTime);
            UpdateTimers();
            ApplyPose();
        }

        private void UpdateTimers()
        {
            if (faceTimeLeft > 0f && (faceTimeLeft -= Time.deltaTime) <= 0f && !hasMood) SetFace(FaceKind.None);
            if (flipTimeLeft > 0f) flipTimeLeft -= Time.deltaTime;
            if (motion == null) return;
            if (motionDelay > 0f)
            {
                motionDelay -= Time.deltaTime;
                return;
            }
            motionTime += Time.deltaTime;
            if (motionTime >= motion.TotalSeconds) motion = null;
        }

        private void ApplyPose()
        {
            var position = Vector2.zero;
            float scaleX = 1f, scaleY = 1f, rotation = 0f, alpha = 1f, tint = 0f;
            if (motion != null && motionDelay <= 0f)
            {
                var progress = Mathf.Repeat(motionTime / motion.Seconds, 1f);
                var isVertical = Mathf.Abs(motionDirection.y) > Mathf.Abs(motionDirection.x);
                position = motionDirection * motion.Offset(progress) + Vector2.up * motion.Lift(progress);
                if (motionDirection == Vector2.zero) position += Vector2.right * motion.Offset(progress);
                scaleX = isVertical ? motion.Across(progress) : motion.Along(progress);
                scaleY = isVertical ? motion.Along(progress) : motion.Across(progress);
                rotation = motion.Rotation(progress) * (motionDirection.x < 0f ? -1f : 1f);
                alpha = motion.Alpha(progress);
                tint = motion.Tint(progress);
            }

            var breath = (1f - Mathf.Cos((Time.time + breathPhase) / breathSeconds * Mathf.PI * 2f)) * 0.5f;
            scaleX *= (1f + breathWide * breath) * (flipTimeLeft > 0f ? -1f : 1f);
            scaleY *= 1f - breathFlat * breath;
            lean = Vector2.Lerp(lean, leanTarget, 1f - Mathf.Exp(-leanSpeed * Time.deltaTime));
            position += lean * leanDistance;
            rotation -= lean.x * leanDegrees;
            visual.localPosition = position;
            visual.localRotation = Quaternion.Euler(0f, 0f, rotation);
            visual.localScale = new Vector3(scaleX, scaleY, 1f);

            var color = Color.Lerp(Color.white, eatenColor, tint);
            color.a = alpha;
            body.color = color;
            faceAlpha = Mathf.MoveTowards(faceAlpha, faceTargetAlpha, Time.deltaTime / Mathf.Max(faceFadeSeconds, 0.0001f));
            face.enabled = faceAlpha > 0.001f;
            face.color = new Color(1f, 1f, 1f, alpha * faceAlpha);
            shadow.color = new Color(1f, 1f, 1f, alpha);
        }
    }
}
