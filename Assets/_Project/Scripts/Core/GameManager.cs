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

        /// <summary>Загружает сцену этажа: 3, 2, 1; 0 = WhiteRoom.</summary>
        public void LoadFloor(int floor)
        {
            State.CurrentFloor = floor;
            string scene = floor == 0 ? "WhiteRoom" : $"Floor_0{floor}";
            Debug.Log($"[Core] LoadFloor({floor}) → {scene}");
            SceneTransitionManager.Instance.Load(scene);
        }

        /// <summary>Новое прохождение (из главного меню).</summary>
        public void ResetState() => State = new GameState();
    }
}
