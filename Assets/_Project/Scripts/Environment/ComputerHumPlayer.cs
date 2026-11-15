using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Гул компьютеров (Audio/Ambient/Resources/AMB_ComputerHum): звучит, пока в кадре камеры есть работающий
    /// компьютер (ComputerHum); ушёл из кадра — плавно стихает. Создаёт себя сам на всю игру, цикл без провала
    /// (SeamlessLoop).
    /// </summary>
    public class ComputerHumPlayer : MonoBehaviour
    {
        const string ClipName = "AMB_ComputerHum";

        [Tooltip("Громкость гула, когда в кадре работающий компьютер (0..1, умножается на канал SFX)")]
        [Range(0f, 1f)] [SerializeField] float volume = 0.35f;

        [Tooltip("Скорость появления/затухания гула: долей полной громкости в секунду")]
        [SerializeField] float fadeSpeed = 2f;

        [Tooltip("Запас за краем кадра (доля экрана): компьютер чуть за краем ещё слышен")]
        [SerializeField] float viewMargin = 0.05f;

        SeamlessLoop loop;
        float level;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Spawn()
        {
            if (FindAnyObjectByType<ComputerHumPlayer>() != null) return;
            var go = new GameObject("ComputerHumPlayer");
            DontDestroyOnLoad(go);
            go.AddComponent<ComputerHumPlayer>();
        }

        void Awake()
        {
            var clip = Resources.Load<AudioClip>(ClipName);
            if (clip == null) Debug.LogWarning($"[Audio] нет Resources/{ClipName} — компьютеры не гудят");
            loop = new SeamlessLoop(clip);
        }

        void Update()
        {
            level = Mathf.MoveTowards(level, AnyInView() ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);
            loop.Update(volume * level);
        }

        bool AnyInView()
        {
            var cam = Camera.main;
            if (cam == null) return false;
            foreach (var pc in ComputerHum.Active)
                if (pc != null && pc.Working && LampHum.InView(cam, pc.Center, viewMargin)) return true;
            return false;
        }
    }
}
