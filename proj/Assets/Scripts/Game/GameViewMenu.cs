using AI4GamesFinalProj.Gameplay;

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Game
{
    public sealed class GameMenuView : MonoBehaviour
    {
        [SerializeField]
        private AttemptController attemptController;

        [SerializeField] private GameObject menuPanel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button startButton;

        private void Awake()
        {
            startButton.onClick.AddListener(StartGame);
        }

        private void OnEnable()
        {
            if (attemptController == null)
            {
                return;
            }

            attemptController.AttemptStarted += HandleAttemptStarted;
            attemptController.AttemptEnded += HandleAttemptEnded;
        }

        private void OnDisable()
        {
            if (attemptController == null)
            {
                return;
            }

            attemptController.AttemptStarted -= HandleAttemptStarted;
            attemptController.AttemptEnded -= HandleAttemptEnded;

            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartGame);
            }
        }

        private void Start()
        {
            ShowStartMenu();
        }

        private void StartGame()
        {
            menuPanel.SetActive(false);
            attemptController.StartPrototypeAttempt();
        }

        private void HandleAttemptStarted(Attempt attempt)
        {
            menuPanel.SetActive(false);
        }

        private void HandleAttemptEnded(Attempt attempt)
        {
            titleText.text = attempt.World.HasWon ? "Victory!" : "Defeat";
            messageText.text = attempt.World.OutcomeReason;
            startButton.GetComponentInChildren<TMP_Text>().text = "Restart Game";
            menuPanel.SetActive(true);
        }

        private void ShowStartMenu()
        {
            titleText.text = "Wizard Territory";
            messageText.text = "Protect the Life Roots and survive the corruption.";
            startButton.GetComponentInChildren<TMP_Text>().text = "Start Game";
            menuPanel.SetActive(true);
        }
    }
}
