using Meowtosis.Domain;

namespace Meowtosis.Tests
{
    // Dựng level nhỏ từ các hàng chữ, giống cách docs/mechanics.md viết ví dụ.
    internal sealed class TestLevel
    {
        private readonly LevelDefinition definition = new LevelDefinition { Id = "test" };

        public static TestLevel FromRows(params string[] rows)
        {
            var level = new TestLevel();
            level.definition.Rows.AddRange(rows);
            return level;
        }

        public TestLevel WithGoal(params string[] goal)
        {
            definition.Goal.AddRange(goal);
            return this;
        }

        public TestLevel WithMembrane(int row, int column, Direction direction)
        {
            definition.Membranes.Add(new Membrane(row, column, direction));
            return this;
        }

        public TestLevel WithPortal(int row, int column, Direction direction, int targetRow, int targetColumn)
        {
            definition.Portals.Add(new Portal(row, column, direction, targetRow, targetColumn));
            return this;
        }

        public TestLevel WithColorSwitch(int row, int column, Species otherColor, int period, int turnsLeft = 0)
        {
            definition.ColorSwitches.Add(new ColorSwitch(row, column, otherColor, period, turnsLeft == 0 ? period : turnsLeft));
            return this;
        }

        public Level Build() => new Level(definition);

        public Board Start() => Build().CreateStartBoard();

        public Playthrough Play(params string[] moves) => Playthrough.Run(Start(), moves);
    }
}
