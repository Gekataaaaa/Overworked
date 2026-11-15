using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Запускает сценарий, когда Дэнни впервые входит в комнату с заданным именем (Floor_02: вход в Room_201 →
    /// сообщение Босса). Срабатывает один раз. Слушает Room.OnRoomChanged, а не onEnter комнаты: onEnter
    /// пересобирает P2StoryWiring и снимает с него чужие сценарии. Кладётся на объект со ScriptedSequence.
    /// </summary>
    public class RoomSequenceTrigger : MonoBehaviour
    {
        [Tooltip("Имя объекта комнаты (Room), например Room_201")]
        [SerializeField] string roomName = "Room_201";

        [Tooltip("Какой сценарий запустить (пусто — ScriptedSequence на этом же объекте)")]
        [SerializeField] ScriptedSequence sequence;

        bool fired;

        void Awake()
        {
            if (sequence == null) sequence = GetComponent<ScriptedSequence>();
        }

        void OnEnable() => Room.OnRoomChanged += OnRoomChanged;

        void OnDisable() => Room.OnRoomChanged -= OnRoomChanged;

        void OnRoomChanged(Room room)
        {
            if (fired || room == null || room.name != roomName || sequence == null) return;
            fired = true;
            sequence.Play();
        }
    }
}
