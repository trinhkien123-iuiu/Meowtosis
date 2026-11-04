using UnityEngine;

namespace Meowtosis.Presentation
{
    // Khoảng cách (tính bằng điểm ảnh) từ mỗi điểm tới điểm gốc gần nhất, tính chính xác theo Euclid
    // (thuật toán Felzenszwalb, chạy hai lượt theo cột rồi theo hàng). Dùng để nới và co hình khung bàn.
    public static class DistanceTransform
    {
        private const float Far = 1e8f;

        public static float[] ToSeeds(bool[] isSeed, int width, int height)
        {
            var squared = new float[width * height];
            for (var index = 0; index < squared.Length; index++) squared[index] = isSeed[index] ? 0f : Far;

            var longest = Mathf.Max(width, height);
            var input = new float[longest];
            var output = new float[longest];
            var parabolaIndex = new int[longest];
            var boundary = new float[longest + 1];

            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++) input[row] = squared[row * width + column];
                Pass(input, output, height, parabolaIndex, boundary);
                for (var row = 0; row < height; row++) squared[row * width + column] = output[row];
            }
            for (var row = 0; row < height; row++)
            {
                for (var column = 0; column < width; column++) input[column] = squared[row * width + column];
                Pass(input, output, width, parabolaIndex, boundary);
                for (var column = 0; column < width; column++) squared[row * width + column] = output[column];
            }

            for (var index = 0; index < squared.Length; index++) squared[index] = Mathf.Sqrt(squared[index]);
            return squared;
        }

        private static void Pass(float[] input, float[] output, int count, int[] parabolaIndex, float[] boundary)
        {
            var last = 0;
            parabolaIndex[0] = 0;
            boundary[0] = -Far;
            boundary[1] = Far;
            for (var position = 1; position < count; position++)
            {
                var crossing = Crossing(input, position, parabolaIndex[last]);
                while (crossing <= boundary[last])
                {
                    last--;
                    crossing = Crossing(input, position, parabolaIndex[last]);
                }
                last++;
                parabolaIndex[last] = position;
                boundary[last] = crossing;
                boundary[last + 1] = Far;
            }

            last = 0;
            for (var position = 0; position < count; position++)
            {
                while (boundary[last + 1] < position) last++;
                var offset = position - parabolaIndex[last];
                output[position] = offset * offset + input[parabolaIndex[last]];
            }
        }

        private static float Crossing(float[] input, int position, int other) =>
            ((input[position] + position * position) - (input[other] + other * other)) / (2f * position - 2f * other);
    }
}
