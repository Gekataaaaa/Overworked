using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Overworked
{
    /// <summary>
    /// Точка для кнопки Continue: где был игрок, когда вышел в меню через паузу.
    /// Живёт только в памяти (в GameManager) до конца запуска игры — на диск не пишется.
    /// После перезапуска игры Continue нет, только New Game.
    /// </summary>
    public class ResumePoint
    {
        /// <summary>Сцена этажа.</summary>
        public string Scene;

        /// <summary>Позиция Дэнни.</summary>
        public Vector3 Position;

        /// <summary>Рассудок (целевое значение, 0..100).</summary>
        public float Sanity;

        /// <summary>Пути включённых камер Cinemachine: игрок мог стоять в комнате, а не в коридоре.</summary>
        public List<string> LiveCameras = new List<string>();

        /// <summary>Запоминает текущее место. null — запоминать нечего (нет игрока).</summary>
        public static ResumePoint Capture()
        {
            var player = PlayerController.Instance;
            if (player == null) return null;
            var point = new ResumePoint
            {
                Scene = SceneManager.GetActiveScene().name,
                Position = player.transform.position,
                Sanity = SanityManager.Instance != null ? SanityManager.Instance.Target : 100f,
            };
            foreach (var cam in Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include))
                if (cam.gameObject.activeInHierarchy && cam.enabled) point.LiveCameras.Add(PathOf(cam.transform));
            return point;
        }

        /// <summary>
        /// Возвращает игрока на место. Вызывать после Start() объектов сцены: FloorSettings к этому
        /// времени уже выставил стартовый рассудок этажа, и мы его перезаписываем сохранённым.
        /// </summary>
        public void Apply()
        {
            var player = PlayerController.Instance;
            if (player != null) player.Teleport(Position);
            if (SanityManager.Instance != null) SanityManager.Instance.SetSanityInstant(Sanity);

            if (LiveCameras.Count == 0) return;
            foreach (var cam in Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include))
            {
                cam.gameObject.SetActive(LiveCameras.Contains(PathOf(cam.transform)));
                // Без этого камера «доедет» от старой позиции с демпфированием, а не встанет сразу.
                cam.PreviousStateIsValid = false;
            }
        }

        static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    }
}
