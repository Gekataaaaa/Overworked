using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Запоминает имя игрока из пролога (PlayerPrefs) и подставляет его вместо «Tester», когда
    /// этаж запущен без пролога (из редактора, F2–F5) или игра перезапущена. Так в сообщениях Босса,
    /// репликах Дэнни, листе A4 и бейдже везде стоит введённое имя. Создаёт себя сам (DontDestroyOnLoad),
    /// код пролога и GameState не меняет: следит за GameManager.State.PlayerName.
    /// </summary>
    public class PlayerNameMemory : MonoBehaviour
    {
        const string Key = "overworked_player_name";
        // Имя по умолчанию в GameState, когда пролога не было.
        const string Placeholder = "Tester";

        string saved;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Spawn()
        {
            var go = new GameObject("PlayerNameMemory");
            DontDestroyOnLoad(go);
            go.AddComponent<PlayerNameMemory>();
        }

        void Awake() => saved = PlayerPrefs.GetString(Key, "");

        // Каждый кадр — дёшево: сравнение двух строк. Новая игра сбрасывает GameState на «Tester» —
        // тогда возвращаем запомненное имя, пока пролог не спросит новое.
        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            string current = gm.State.PlayerName;

            if (string.IsNullOrEmpty(current) || current == Placeholder)
            {
                if (!string.IsNullOrEmpty(saved)) gm.State.PlayerName = saved;
                return;
            }
            if (current == saved) return;

            // Введено новое имя — запомнить.
            saved = current;
            PlayerPrefs.SetString(Key, saved);
            PlayerPrefs.Save();
        }
    }
}
