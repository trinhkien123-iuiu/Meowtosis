using System;
using System.Collections.Generic;
using System.Linq;
using Meowtosis.Domain;
using Meowtosis.Gameplay;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Nội dung một thẻ "Luật mới" (Data/Mechanics): tên, lời giải thích và một bàn nhỏ minh hoạ chạy lặp một chuỗi nước vuốt.
    // Sửa chữ hoặc đổi minh hoạ ngay trong Inspector.
    [CreateAssetMenu(fileName = "MechanicCard", menuName = "Meowtosis/Mechanic Card")]
    public sealed class MechanicCard : ScriptableObject
    {
        [SerializeField] private Mechanic mechanic;
        [SerializeField] private string title = "";
        [SerializeField, TextArea(2, 5)] private string description = "";

        [Header("Minh hoạ")]
        [Tooltip("Các hàng của bàn, viết như trong level (A. B# ...).")]
        [SerializeField] private string[] demoRows = Array.Empty<string>();
        [SerializeField] private string[] demoGoal = Array.Empty<string>();
        [Tooltip("Mỗi dòng \"hàng cột hướng\", ví dụ \"0 2 left\".")]
        [SerializeField] private string[] demoMembranes = Array.Empty<string>();
        [Tooltip("Mỗi dòng \"hàng cột hướng hàng-kia cột-kia\", ví dụ \"0 0 left 0 2\".")]
        [SerializeField] private string[] demoPortals = Array.Empty<string>();
        [Tooltip("Mỗi dòng \"hàng cột màu-kia chu-kỳ lượt-còn-lại\", ví dụ \"0 2 A 1 1\" (màu: A đỏ, B xanh, C vàng, D hồng, E tím).")]
        [SerializeField] private string[] demoColorSwitches = Array.Empty<string>();
        [Tooltip("Nước vuốt \"hàng cột hướng\", ví dụ \"0 0 right\". Bỏ qua nếu có danh sách nhiều nước bên dưới.")]
        [SerializeField] private string demoMove = "0 0 right";
        [Tooltip("Chuỗi nước minh hoạ, đi lần lượt rồi lặp lại từ đầu (mỗi dòng \"hàng cột hướng\").")]
        [SerializeField] private string[] demoMoves = Array.Empty<string>();

        public Mechanic Mechanic => mechanic;

        public string Title => title;

        public string Description => description;

        public IReadOnlyList<Move> DemoMoves =>
            demoMoves.Length > 0 ? demoMoves.Select(Move.Parse).ToList() : new List<Move> { Move.Parse(demoMove) };

        public LevelDefinition CreateDemoLevel()
        {
            var definition = new LevelDefinition { Id = name };
            definition.Rows.AddRange(demoRows);
            definition.Goal.AddRange(demoGoal);
            foreach (var text in demoMembranes)
            {
                var position = Move.Parse(text);
                definition.Membranes.Add(new Membrane(position.Row, position.Column, position.Direction));
            }
            foreach (var text in demoPortals) definition.Portals.Add(LevelCatalogParser.ParsePortal(text));
            foreach (var text in demoColorSwitches) definition.ColorSwitches.Add(LevelCatalogParser.ParseColorSwitch(text));
            return definition;
        }
    }
}
