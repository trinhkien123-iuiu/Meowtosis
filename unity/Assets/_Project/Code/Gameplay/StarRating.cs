using System;

namespace Meowtosis.Gameplay
{
    // Số sao khi thắng: bằng par được 3 sao, dư tối đa 2 nước được 2 sao, còn lại 1 sao.
    public static class StarRating
    {
        public const int MaximumStars = 3;
        public const int ExtraMovesForTwoStars = 2;

        public static int For(int movesMade, int par)
        {
            if (movesMade <= par) return MaximumStars;
            return movesMade <= par + ExtraMovesForTwoStars ? 2 : 1;
        }
    }

    // Thanh sao trên HUD: đầy lúc bắt đầu, tụt dần theo số nước. Ba ngôi sao nằm ở 0, 1/3 và 2/3 thanh;
    // thanh còn chạm tới sao nào thì sao đó còn sáng, nên người chơi biết thắng lúc này được mấy sao.
    //   0 … par nước:            thanh từ 1 xuống 2/3 (vẫn 3 sao)
    //   par … par + 2 nước:      từ 2/3 xuống 1/3 (2 sao)
    //   sau đó:                  từ 1/3 tụt dần về 0 trong 5 nước (luôn còn 1 sao)
    public readonly struct StarMeter
    {
        private const int MovesToEmpty = 5;

        private StarMeter(float fill, int stars)
        {
            Fill = fill;
            Stars = stars;
        }

        // Phần thanh còn lại, từ 0 tới 1.
        public float Fill { get; }

        public int Stars { get; }

        // Vị trí ngôi sao thứ `star` (1 tới 3) trên thanh.
        public static float MarkerOf(int star) => (star - 1) / 3f;

        public static StarMeter For(int movesMade, int par)
        {
            var stars = StarRating.For(movesMade, par);
            float fill;
            if (movesMade <= par) fill = 1f - (par == 0 ? 0f : movesMade / (float)par) / 3f;
            else if (movesMade <= par + StarRating.ExtraMovesForTwoStars)
                fill = 2f / 3f - (movesMade - par) / (float)StarRating.ExtraMovesForTwoStars / 3f;
            else fill = 1f / 3f * (1f - Math.Min(1f, (movesMade - par - StarRating.ExtraMovesForTwoStars) / (float)MovesToEmpty));
            return new StarMeter(fill, stars);
        }
    }
}
