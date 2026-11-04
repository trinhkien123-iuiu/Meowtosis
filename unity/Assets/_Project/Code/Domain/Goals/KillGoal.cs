namespace Meowtosis.Domain
{
    // Trên bàn không còn quân nào của loài này (level diệt khuẩn).
    public sealed class KillGoal : ILevelGoal
    {
        private readonly Species species;

        public KillGoal(Species species)
        {
            this.species = species;
        }

        public bool IsMet(Board board) => !board.Contains(species);
    }
}
