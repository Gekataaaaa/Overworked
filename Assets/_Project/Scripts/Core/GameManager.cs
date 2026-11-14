using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Хранит GameState и переключает этажи. Не превращать в «бога игры» —
    /// у каждой системы свой менеджер (паспорт, раздел 25).
    /// </summary>
    public class GameManager : SingletonBehaviour<GameManager>
    {
        /// <summary>Состояние текущего прохождения.</summary>
        public GameState State { get; private set; } = new GameState();

        /// <summary>Куда вернёт Continue; null — продолжать нечего (первый запуск, новая игра, игра пройдена).</summary>
        public ResumePoint Resume { get; private set; }

        /// <summary>Показывать ли Continue в главном меню.</summary>
        public bool CanContinue => Resume != null;

        /// <summary>Загружает сцену этажа: 3, 2, 1; 0 = WhiteRoom.</summary>
        public void LoadFloor(int floor)
        {
            // Во время перехода вызов проигнорируется — не меняем этаж в состоянии зря.
            if (SceneTransitionManager.Instance.IsLoading) return;
            State.CurrentFloor = floor;
            if (floor == 0) Resume = null;   // WhiteRoom — игра пройдена, продолжать нечего
            string scene = floor == 0 ? "WhiteRoom" : $"Floor_0{floor}";
            Debug.Log($"[Core] LoadFloor({floor}) → {scene}");
            SceneTransitionManager.Instance.Load(scene);
        }

        /// <summary>Возврат в главное меню (после титров, из паузы).</summary>
        public void LoadMainMenu()
        {
            Debug.Log("[Core] LoadMainMenu");
            SceneTransitionManager.Instance.Load("MainMenu");
        }

        /// <summary>Выход в меню из паузы: запоминает, где был игрок, чтобы Continue вернул туда же.</summary>
        public void LeaveToMainMenu()
        {
            if (SceneTransitionManager.Instance.IsLoading) return;
            Resume = ResumePoint.Capture();
            LoadMainMenu();
        }

        /// <summary>
        /// Continue: загружает сцену из Resume, затем ставит игрока на место, возвращает рассудок
        /// и камеру. Имя, флаги и телефон и так живут в памяти — State не сбрасывается.
        /// </summary>
        public void ContinueGame()
        {
            if (Resume == null || SceneTransitionManager.Instance.IsLoading) return;
            var point = Resume;
            void OnLoaded(string scene)
            {
                SceneTransitionManager.Instance.OnSceneLoaded -= OnLoaded;
                if (scene != point.Scene) return;
                point.Apply();
                Resume = null;
            }
            SceneTransitionManager.Instance.OnSceneLoaded += OnLoaded;
            Debug.Log($"[Core] Continue → {point.Scene} {point.Position}");
            SceneTransitionManager.Instance.Load(point.Scene);
        }

        /// <summary>Новое прохождение (из главного меню): имя спросят заново, Continue пропадает, задания сбрасываются.</summary>
        public void ResetState()
        {
            State = new GameState();
            Resume = null;
            // Задания живут в _Systems и пережили бы новую игру (просьба P2).
            if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.ResetAll();
        }
    }
}
