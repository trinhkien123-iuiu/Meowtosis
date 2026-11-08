using System;
using System.Collections.Generic;
using System.Globalization;

namespace Meowtosis.Domain
{
    // Đọc danh sách level (Levels/LevelCatalog.txt, do tools/export-unity.js ghi ra).
    // Mỗi dòng là "từ-khoá giá-trị"; dòng "level <id>" mở level kế tiếp. Bỏ qua dòng trống và dòng "//".
    //   level meow-017
    //   block 2
    //   par 2
    //   row AB..
    //   goal ..B.
    //   membrane 0 1 left
    //   portal 0 1 right 0 3         (đi ra ô (0,1) theo hướng phải thì vào ô (0,3); chiều ngược lại cũng vậy)
    //   dual 0 2 A 2 2               (mèo hai màu ở (0,2): màu kia là đỏ, chu kỳ 2 lượt, còn 2 lượt)
    //   tutorial 0 0 right Đẩy mèo xanh vào hộp xanh
    //   rule Câu luật hiện sau nước hướng dẫn ngay trên (chỉ dùng trong Levels/Onboarding.txt)
    //   tip Một câu mẹo
    //   solution 0 0 right
    public static class LevelCatalogParser
    {
        public static List<LevelDefinition> Parse(string text)
        {
            var levels = new List<LevelDefinition>();
            LevelDefinition current = null;
            var lines = text.Split('\n');
            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index].Trim();
                if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal)) continue;

                var space = line.IndexOf(' ');
                var keyword = space < 0 ? line : line.Substring(0, space);
                var value = space < 0 ? "" : line.Substring(space + 1).Trim();
                try
                {
                    if (keyword == "level")
                    {
                        current = new LevelDefinition { Id = value };
                        levels.Add(current);
                    }
                    else if (current == null) throw new FormatException($"'{keyword}' đứng trước dòng 'level' đầu tiên");
                    else Apply(current, keyword, value);
                }
                catch (FormatException error)
                {
                    throw new FormatException($"Danh sách level, dòng {index + 1}: {error.Message}", error);
                }
            }
            return levels;
        }

        private static void Apply(LevelDefinition level, string keyword, string value)
        {
            switch (keyword)
            {
                case "block":
                    level.Block = int.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case "par":
                    level.Par = int.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case "row":
                    level.Rows.Add(value);
                    break;
                case "goal":
                    level.Goal.Add(value);
                    break;
                case "membrane":
                    var position = Move.Parse(value);
                    level.Membranes.Add(new Membrane(position.Row, position.Column, position.Direction));
                    break;
                case "portal":
                    level.Portals.Add(ParsePortal(value));
                    break;
                case "dual":
                    level.ColorSwitches.Add(ParseColorSwitch(value));
                    break;
                case "tutorial":
                    level.Tutorial.Add(ParseStep(value));
                    break;
                case "rule":
                    if (level.Tutorial.Count == 0) throw new FormatException("'rule' phải đứng ngay sau một dòng 'tutorial'");
                    var last = level.Tutorial.Count - 1;
                    level.Tutorial[last] = level.Tutorial[last].WithRule(value);
                    break;
                case "card":
                    level.Card = value;
                    break;
                case "trap":
                    level.Trap = ParseStep(value);
                    break;
                case "tip":
                    level.Tip = value;
                    break;
                case "solution":
                    level.Solution.Add(Move.Parse(value));
                    break;
                default:
                    throw new FormatException($"Không rõ từ khoá '{keyword}'");
            }
        }

        public static Portal ParsePortal(string value)
        {
            var parts = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5) throw new FormatException($"Cần 'hàng cột hướng hàng-kia cột-kia' nhưng gặp '{value}'");
            var from = Move.Parse($"{parts[0]} {parts[1]} {parts[2]}");
            return new Portal(from.Row, from.Column, from.Direction,
                int.Parse(parts[3], CultureInfo.InvariantCulture), int.Parse(parts[4], CultureInfo.InvariantCulture));
        }

        public static ColorSwitch ParseColorSwitch(string value)
        {
            var parts = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5 || parts[2].Length != 1 || !LevelSymbols.TryReadSpecies(parts[2][0], out var other))
                throw new FormatException($"Cần 'hàng cột màu-kia chu-kỳ lượt-còn-lại' nhưng gặp '{value}'");
            return new ColorSwitch(int.Parse(parts[0], CultureInfo.InvariantCulture), int.Parse(parts[1], CultureInfo.InvariantCulture),
                other, int.Parse(parts[3], CultureInfo.InvariantCulture), int.Parse(parts[4], CultureInfo.InvariantCulture));
        }

        private static TutorialStep ParseStep(string value)
        {
            var parts = value.Split(new[] { ' ' }, 4);
            if (parts.Length < 3) throw new FormatException($"Cần 'hàng cột hướng lời-thoại' nhưng gặp '{value}'");
            var move = Move.Parse($"{parts[0]} {parts[1]} {parts[2]}");
            return new TutorialStep(move, parts.Length > 3 ? parts[3] : "");
        }
    }
}
