using System.Collections.Generic;
using UnityEngine;

namespace AI4GamesFinalProj.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField]
        private AttemptController attemptController;

        [SerializeField]
        private bool rebuildBoardOnAttemptStart = true;

        [SerializeField]
        private bool clearBoardWhenAttemptEnds;

        private readonly Dictionary<Vector2Int, CellView> cellViews = new Dictionary<Vector2Int, CellView>();

        private Transform boardRoot;

        private void Awake()
        {
            if (attemptController == null)
            {
                attemptController = GetComponent<AttemptController>();
            }

            EnsureBoardRoot();
        }

        private void OnEnable()
        {
            if (attemptController == null)
            {
                return;
            }

            attemptController.AttemptStarted += HandleAttemptStarted;
            attemptController.AwaitingPlayerInput += HandleAttemptStateChanged;
            attemptController.TurnResolved += HandleTurnResolved;
            attemptController.AttemptEnded += HandleAttemptEnded;

            if (attemptController.CurrentAttempt != null)
            {
                BuildOrRefresh(attemptController.CurrentAttempt, true);
            }
        }

        private void OnDisable()
        {
            if (attemptController == null)
            {
                return;
            }

            attemptController.AttemptStarted -= HandleAttemptStarted;
            attemptController.AwaitingPlayerInput -= HandleAttemptStateChanged;
            attemptController.TurnResolved -= HandleTurnResolved;
            attemptController.AttemptEnded -= HandleAttemptEnded;
        }

        public void BuildOrRefresh(Attempt attempt, bool forceRebuild)
        {
            if (attempt?.Board is not SquareGameBoard board)
            {
                return;
            }

            EnsureBoardRoot();

            if (forceRebuild)
            {
                ClearBoard();
            }

            foreach (BoardCell cell in board.AllCells)
            {
                Vector2Int key = new Vector2Int(cell.X, cell.Y);
                bool refreshWithRebuild = forceRebuild;
                if (!cellViews.TryGetValue(key, out CellView view) || view == null)
                {
                    view = CreateCellView(key, cell);
                    refreshWithRebuild = false;
                }

                view.Refresh(cell, refreshWithRebuild);
            }
        }

        private CellView CreateCellView(Vector2Int key, BoardCell cell)
        {
            GameObject root = new GameObject($"Cell_{cell.X}_{cell.Y}");
            root.transform.SetParent(boardRoot, false);
            CellView cellView = root.AddComponent<CellView>();
            cellView.Initialize(attemptController.BoardPrefabs, cell);
            cellViews[key] = cellView;
            return cellView;
        }

        private void EnsureBoardRoot()
        {
            if (boardRoot != null)
            {
                return;
            }

            Transform configuredParent = attemptController != null
                ? attemptController.BoardPrefabs.GetSpawnParent()
                : null;

            if (configuredParent != null)
            {
                boardRoot = configuredParent;
                return;
            }

            GameObject rootObject = new GameObject("BoardViewRoot");
            rootObject.transform.SetParent(transform, false);
            boardRoot = rootObject.transform;
        }

        private void ClearBoard()
        {
            foreach (CellView view in cellViews.Values)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            cellViews.Clear();
        }

        private void HandleAttemptStarted(Attempt attempt)
        {
            BuildOrRefresh(attempt, rebuildBoardOnAttemptStart);
        }

        private void HandleAttemptStateChanged(Attempt attempt)
        {
            BuildOrRefresh(attempt, false);
        }

        private void HandleTurnResolved(Attempt attempt, PlayerAction action)
        {
            BuildOrRefresh(attempt, false);
        }

        private void HandleAttemptEnded(Attempt attempt)
        {
            if (clearBoardWhenAttemptEnds)
            {
                ClearBoard();
                return;
            }

            BuildOrRefresh(attempt, false);
        }
    }
}
