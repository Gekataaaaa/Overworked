using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Вид коридора меняется после комнаты (Floor_01, превью художника Floor01_Art):
    /// V1 — как при появлении на этаже; после выхода из Room_101 без ключа — V2 (галлюцинации:
    /// глаза в проёме и у архива, перевёрнутая картина, фигура впереди); с ключом (флаг Key102) — V3
    /// (появляется дверь CHILL ZONE 102). Надписи на стене и паук — в обоих вариантах после 101.
    /// Переключение — каждый раз, когда Дэнни выходит в коридор: экран в этот момент ещё чёрный,
    /// поэтому смену не видно. Ставится сборщиком Overworked → P4 → Art → Build Floor_01 Art.
    /// </summary>
    public class CorridorVariants : MonoBehaviour
    {
        [Tooltip("После выхода из этой комнаты коридор меняется (Room_101)")]
        [SerializeField] Room watchedRoom;

        [Tooltip("Флаг, который выбирает вариант с ключом (\"Key102\")")]
        [SerializeField] string flag = "Key102";

        [Tooltip("Видно после первого выхода из комнаты в обоих вариантах (надписи, паук)")]
        [SerializeField] GameObject[] afterVisit;

        [Tooltip("Видно после комнаты, пока флага нет (V2 — галлюцинации)")]
        [SerializeField] GameObject[] withoutFlag;

        [Tooltip("Видно после комнаты с флагом (V3 — дверь 102)")]
        [SerializeField] GameObject[] withFlag;

        bool visited;

        void OnEnable() => Room.OnRoomChanged += OnRoomChanged;

        void OnDisable() => Room.OnRoomChanged -= OnRoomChanged;

        void Start() => Apply();

        void OnRoomChanged(Room room)
        {
            // Вошёл в нужную комнату — после выхода коридор уже другой.
            if (room != null && room == watchedRoom) visited = true;
            if (room == null) Apply();
        }

        /// <summary>Показывает вариант по текущему состоянию (вызвать и из сценария, если флаг поставлен вне комнаты).</summary>
        public void Apply()
        {
            bool key = !string.IsNullOrEmpty(flag) && InteractableBase.HasFlag(flag);
            Set(afterVisit, visited);
            Set(withoutFlag, visited && !key);
            Set(withFlag, visited && key);
        }

        static void Set(GameObject[] objects, bool active)
        {
            if (objects == null) return;
            foreach (var go in objects)
                if (go != null && go.activeSelf != active) go.SetActive(active);
        }
    }
}
