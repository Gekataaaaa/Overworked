using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Запуск финального скримера (NARRATIVE, сцены 3.2–3.3): Дэнни выиграл Tiny UFO в Room_102 и вышел
    /// в коридор — как только экран после двери проявился, начинается H5_FinalScreamer (дальше FinalCollapse → WhiteRoom).
    /// Без сыгранной партии выход из комнаты ничего не запускает. Партию ловит по onDone аркадного автомата
    /// (MiniGameInteractable вызывает его, когда партия сыграна — победа или поражение, неважно; QUIT — нет),
    /// выход — по Room.onExit комнаты 102.
    /// </summary>
    public class FinalScreamerStarter : MonoBehaviour
    {
        [Tooltip("Комната отдыха (Environment/Rooms/Room_102, компонент Room)")]
        [SerializeField] Room room;

        [Tooltip("Аркадный автомат с Tiny UFO в Room_102 (его onDone = победа)")]
        [SerializeField] InteractableBase arcade;

        [Tooltip("Финальный скример (префаб H5_FinalScreamer в папке Hallucinations сцены)")]
        [SerializeField] HallucinationEvent finalScreamer;

        [Tooltip("Флаг победы для проверки после Continue (пусто — только onDone автомата)")]
        [SerializeField] string wonFlag = "TinyUfoWon";

        [Tooltip("Пауза после того, как экран проявился и Дэнни стоит в коридоре, секунд")]
        [SerializeField] float delay = 0.3f;

        bool won, started;

        void OnEnable()
        {
            if (arcade != null) arcade.onDone.AddListener(OnWon);
            if (room != null) room.onExit.AddListener(OnRoomExit);
        }

        void OnDisable()
        {
            if (arcade != null) arcade.onDone.RemoveListener(OnWon);
            if (room != null) room.onExit.RemoveListener(OnRoomExit);
        }

        void OnWon()
        {
            won = true;
            if (!string.IsNullOrEmpty(wonFlag)) InteractableBase.SetFlag(wonFlag);
            Debug.Log("[Ending] Tiny UFO пройден — финальный скример при выходе из Room_102");
        }

        bool Won => won || (!string.IsNullOrEmpty(wonFlag) && InteractableBase.HasFlag(wonFlag));

        void OnRoomExit()
        {
            if (started || !Won || finalScreamer == null) return;
            started = true;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            // onExit приходит под затемнением двери: ждём, пока дверь проявит экран и вернёт управление,
            // иначе дверь разблокирует игрока посреди скримера.
            var fade = FadeController.Instance;
            var player = PlayerController.Instance;
            for (float t = 0f; t < 5f; t += Time.deltaTime)
            {
                bool clear = fade == null || !fade.IsBlack;
                bool free = player == null || player.State == PlayerState.Normal;
                if (clear && free) break;
                yield return null;
            }
            yield return new WaitForSeconds(delay);
            if (HallucinationManager.Instance != null) HallucinationManager.Instance.Play(finalScreamer);
            else finalScreamer.Play();
        }
    }
}
