using UnityEngine;

namespace Shidaku
{
    // Thin wire-up between Board and GameplayUI — no game logic of its own.
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private Board board;
        [SerializeField] private GameplayUI gameplayUI;

        private int currentLevelIndex;
        private bool overviewMode;

        private void Awake()
        {
            gameplayUI.OnHintClicked += () => board.RevealHint();
            gameplayUI.OnOverviewClicked += ToggleOverview;
            gameplayUI.OnNextLevelClicked += NextLevel;
            board.OnProgressChanged += () => gameplayUI.FlashProgress(board.ProgressFraction);
            board.OnLevelCompleted += OnLevelCompleted;
        }

        private void Start()
        {
            currentLevelIndex = 1;
            gameplayUI.SetLevelLabel(currentLevelIndex);
            board.SpawnLevel(currentLevelIndex);
            board.FrameToCurrentSection(instant: true);
        }

        private void Update()
        {
            // The only input GameManager itself polls: tap anywhere outside UI to leave Overview.
            if (overviewMode && Input.GetMouseButtonDown(0) && !board.IsPointerOverUI())
                ToggleOverview();
        }

        private void ToggleOverview()
        {
            overviewMode = !overviewMode;
            board.InputEnabled = !overviewMode;
            if (overviewMode) board.FrameToOverview(instant: false);
            else board.FrameToCurrentSection(instant: false);
            gameplayUI.SetOverviewVisuals(overviewMode, board.ProgressFraction);
        }

        private void OnLevelCompleted()
        {
            board.InputEnabled = false;
            gameplayUI.ShowWin();
            board.FrameToOverview(instant: false);
        }

        private void NextLevel()
        {
            gameplayUI.HideWin();
            overviewMode = false;
            board.InputEnabled = true;
            currentLevelIndex++;
            gameplayUI.SetLevelLabel(currentLevelIndex);
            board.SpawnLevel(currentLevelIndex);
            board.FrameToCurrentSection(instant: true);
        }
    }
}
