using System;
using Meowtosis.Domain;
using Meowtosis.Gameplay;

namespace Meowtosis.Presentation
{
    // Điều phối màn hướng dẫn đầu game, theo kiểu "làm theo rồi mới đọc luật":
    // - Bước hướng dẫn: làm tối mọi thứ trừ các ô của nước đang dạy, bàn tay chỉ nước, bong bóng lời dẫn ở trên.
    // - Đi xong: sáng cả bàn, bong bóng đổi sang câu luật vừa thấy, nút "Đã hiểu!" dưới bàn.
    // - Phần cuối người chơi tự lấp kín; đi sai hoặc đứng yên lâu thì bàn tay hiện ra chỉ nước.
    // - Kín ổ: mèo ăn mừng, nút "Bắt đầu chơi" lưu tiến độ rồi gọi `finished`.
    public sealed class OnboardingPresenter : IDisposable
    {
        private const float IdleSecondsBeforeHelp = 6f;

        private readonly Onboarding onboarding;
        private readonly PlayerProgress progress;
        private readonly IProgressStore store;
        private readonly IBoardView board;
        private readonly IHudView hud;
        private readonly IOnboardingView view;
        private readonly ISoundPlayer sound;
        private readonly Action finished;
        private bool isPlayingMove;
        private bool isHelping;
        private float idleSeconds;

        public OnboardingPresenter(LevelDefinition definition, GameRules rules, PlayerProgress progress, IProgressStore store,
            IBoardView board, IHudView hud, IOnboardingView view, ISoundPlayer sound, Action finished)
        {
            onboarding = new Onboarding(definition, rules.Swipes, rules.Solver);
            this.progress = progress;
            this.store = store;
            this.board = board;
            this.hud = hud;
            this.view = view;
            this.sound = sound;
            this.finished = finished;
            Subscribe(true);
        }

        public void Start()
        {
            sound.SetSoundOn(progress.SoundOn);
            sound.SetMusicOn(progress.MusicOn);
            view.Enter();
            board.Build(onboarding.Session.Level);
            board.Show(onboarding.Session.Board);
            Refresh();
        }

        // Gọi mỗi khung hình: lúc tự chơi mà đứng yên lâu thì chỉ nước.
        public void Tick(float deltaTime)
        {
            if (onboarding.Stage != OnboardingStage.Free || isPlayingMove || isHelping) return;
            idleSeconds += deltaTime;
            if (idleSeconds >= IdleSecondsBeforeHelp) ShowHelp();
        }

        public void Dispose() => Subscribe(false);

        private void OnCellSwiped(int cell, Direction direction)
        {
            if (isPlayingMove) return;
            if (!onboarding.TryPlay(cell, direction, out var move))
            {
                board.Refuse(cell);
                if (onboarding.Stage == OnboardingStage.Free) ShowHelp();
                return;
            }
            isPlayingMove = true;
            board.IdleEnabled = false;
            board.HideTutorial();
            board.Play(move, OnMoveFinished, lingerOnClear: true);
        }

        private void OnCellTapped(int cell)
        {
            if (!isPlayingMove) board.Pet(cell);
        }

        private void OnMoveFinished()
        {
            isPlayingMove = false;
            if (onboarding.Stage == OnboardingStage.Finished)
            {
                board.Celebrate(Refresh);
                return;
            }
            if (onboarding.Stage == OnboardingStage.ShowingRule) sound.Play(SoundId.PopupOpen);
            Refresh();
        }

        private void OnButtonClicked()
        {
            sound.Play(SoundId.ButtonTap);
            if (onboarding.Stage == OnboardingStage.ShowingRule)
            {
                onboarding.AcknowledgeRule();
                Refresh();
            }
            else if (onboarding.Stage == OnboardingStage.Finished)
            {
                progress.HasFinishedOnboarding = true;
                foreach (var mechanic in Onboarding.TaughtMechanics) progress.MarkMechanicSeen(mechanic.ToString());
                store.Save(progress);
                view.Exit();
                finished();
            }
        }

        // Vẽ lại theo giai đoạn hiện tại của hướng dẫn.
        private void Refresh()
        {
            board.HideTutorial();
            board.ClearFocus();
            board.IdleEnabled = true;
            isHelping = false;
            idleSeconds = 0f;
            hud.ShowSpecies(SpeciesCensus.Of(onboarding.Session.Board));

            switch (onboarding.Stage)
            {
                case OnboardingStage.Guided:
                    hud.ShowTip(onboarding.Text);
                    view.HideButton();
                    board.Focus(onboarding.FocusCells());
                    ShowHand(onboarding.CurrentStep.Move);
                    break;
                case OnboardingStage.ShowingRule:
                    hud.ShowTip(onboarding.Text);
                    view.ShowButton(GameTexts.Understood);
                    break;
                case OnboardingStage.Free:
                    hud.ShowTip(onboarding.Text);
                    view.HideButton();
                    break;
                default:
                    hud.ShowTip(GameTexts.OnboardingDone);
                    view.ShowButton(GameTexts.StartPlaying);
                    break;
            }
        }

        private void ShowHelp()
        {
            if (!onboarding.TrySuggest(out var move)) return;
            isHelping = true;
            ShowHand(move);
        }

        private void ShowHand(Move move) =>
            board.ShowTutorial(onboarding.Session.Level.IndexOf(move.Row, move.Column), move.Direction);

        private void Subscribe(bool subscribe)
        {
            if (subscribe)
            {
                board.CellSwiped += OnCellSwiped;
                board.CellTapped += OnCellTapped;
                view.ButtonClicked += OnButtonClicked;
            }
            else
            {
                board.CellSwiped -= OnCellSwiped;
                board.CellTapped -= OnCellTapped;
                view.ButtonClicked -= OnButtonClicked;
            }
        }
    }
}
