using Godot;

namespace MineSweeper;

public partial class AppManager : Control
{
    private MainMenuScreen _menuScreen = null!;
    private GameScreen _gameScreen = null!;
    private AchievementGallery _achievementGallery = null!;
    private CustomModeDialog _customModeDialog = null!;
    private ResultDialog _resultDialog = null!;
    private BestTimes _bestTimes = null!;
    private AchievementData _achievementData = null!;

    private GameMode _lastMode;

    public override void _Ready()
    {
        _bestTimes = new BestTimes();
        _bestTimes.Load();

        _achievementData = new AchievementData();
        _achievementData.Load();

        _menuScreen = new MainMenuScreen();
        _menuScreen.GameStartRequested += OnGameStartRequested;
        _menuScreen.AchievementsRequested += OnAchievementsRequested;
        _menuScreen.CustomModeRequested += OnCustomModeRequested;
        AddChild(_menuScreen);

        _gameScreen = new GameScreen();
        _gameScreen.GameEnded += OnGameEnded;
        _gameScreen.BackRequested += OnBackFromGame;
        _gameScreen.Visible = false;
        AddChild(_gameScreen);

        _achievementGallery = new AchievementGallery();
        _achievementGallery.BackRequested += OnBackFromAchievements;
        _achievementGallery.Visible = false;
        AddChild(_achievementGallery);

        _customModeDialog = new CustomModeDialog();
        _customModeDialog.CustomModeConfirmed += OnCustomModeConfirmed;
        _customModeDialog.Visible = false;
        AddChild(_customModeDialog);

        _resultDialog = new ResultDialog();
        _resultDialog.PlayAgainRequested += OnPlayAgain;
        _resultDialog.BackToMenuRequested += OnBackToMenu;
        _resultDialog.Visible = false;
        AddChild(_resultDialog);

        _menuScreen.Refresh(_bestTimes);
    }

    private void OnGameStartRequested(GameMode mode)
    {
        _lastMode = mode;
        _menuScreen.Visible = false;
        _gameScreen.Visible = true;
        _gameScreen.NewGame(mode);
    }

    private void OnAchievementsRequested()
    {
        _menuScreen.Visible = false;
        _achievementGallery.Refresh(_achievementData);
        _achievementGallery.Visible = true;
    }

    private void OnBackFromAchievements()
    {
        _achievementGallery.Visible = false;
        _menuScreen.Visible = true;
    }

    private void OnCustomModeRequested()
    {
        _customModeDialog.ShowDialog();
    }

    private void OnCustomModeConfirmed(int width, int height, int mines)
    {
        var mode = GameMode.Custom(width, height, mines);
        OnGameStartRequested(mode);
    }

    private void OnGameEnded(bool won, double time, GameMode mode)
    {
        var logic = _gameScreen.CurrentLogic;

        bool allMinesFlagged = false;
        if (won && logic != null)
        {
            allMinesFlagged = true;
            for (int x = 0; x < logic.Model.Width; x++)
                for (int y = 0; y < logic.Model.Height; y++)
                    if (logic.Model.MineMap[x, y] && logic.DisplayState[x, y] != CellState.Flagged)
                        allMinesFlagged = false;
        }

        var newAchievements = _achievementData.CheckOnGameEnd(
            won, mode, time,
            logic?.FlagCount ?? 0,
            allMinesFlagged,
            _gameScreen.FlagsEverUsed,
            _gameScreen.MaxChordRevealCount,
            _gameScreen.FirstClickRevealCount,
            _gameScreen.MaxConsecutiveReveals,
            _bestTimes);

        if (won && !mode.IsCustom)
        {
            bool isNewBest = _bestTimes.TryUpdate(mode.Name, time);
            _resultDialog.ShowResult(won, time, _bestTimes.Get(mode.Name), isNewBest,
                newAchievements);
        }
        else
        {
            _resultDialog.ShowResult(won, time, null, false,
                newAchievements.Count > 0 ? newAchievements : null);
        }
    }

    private void OnPlayAgain()
    {
        _resultDialog.Visible = false;
        _gameScreen.NewGame(_lastMode);
    }

    private void OnBackFromGame()
    {
        _gameScreen.Visible = false;
        _menuScreen.Refresh(_bestTimes);
        _menuScreen.Visible = true;
    }

    private void OnBackToMenu()
    {
        _resultDialog.Visible = false;
        _gameScreen.Visible = false;
        _menuScreen.Refresh(_bestTimes);
        _menuScreen.Visible = true;
    }
}
