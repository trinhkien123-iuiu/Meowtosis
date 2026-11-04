using System;
using Meowtosis.Domain;
using Meowtosis.Gameplay;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Nội dung một thẻ "Cơ chế mới" (Data/Mechanics): tên, lời giải thích và một bàn nhỏ minh hoạ với một nước vuốt.
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
        [SerializeField] private bool demoHasGerms;
        [Tooltip("Nước vuốt \"hàng cột hướng\", ví dụ \"0 0 right\".")]
        [SerializeField] private string demoMove = "0 0 right";

        public Mechanic Mechanic => mechanic;

        public string Title => title;

        public string Description => description;

        public Move DemoMove => Move.Parse(demoMove);

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
            if (demoHasGerms) definition.KillSpecies = Species.Germ;
            return definition;
        }
    }
}
