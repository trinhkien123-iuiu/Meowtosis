namespace Meowtosis.Presentation
{
    // Mọi view của màn chơi mà GamePresenter cần, gom lại để constructor gọn.
    public sealed class GameViews
    {
        public GameViews(IBoardView board, IHudView hud, IWinPopup win, ILosePopup lose, ISettingsPopup settings,
            INoHeartsPopup noHearts, IMechanicPopup mechanic, ISoundPlayer sound, IAmbientEvents ambient, ICheatPopup cheat)
        {
            Ambient = ambient;
            Board = board;
            Hud = hud;
            Win = win;
            Lose = lose;
            Settings = settings;
            NoHearts = noHearts;
            Mechanic = mechanic;
            Sound = sound;
            Cheat = cheat;
        }

        public IBoardView Board { get; }

        public IHudView Hud { get; }

        public IWinPopup Win { get; }

        public ILosePopup Lose { get; }

        public ISettingsPopup Settings { get; }

        public INoHeartsPopup NoHearts { get; }

        public IMechanicPopup Mechanic { get; }

        public ISoundPlayer Sound { get; }

        public IAmbientEvents Ambient { get; }

        public ICheatPopup Cheat { get; }
    }
}
