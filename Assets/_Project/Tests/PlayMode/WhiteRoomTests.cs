using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Overworked.Tests
{
    /// <summary>
    /// Проход WhiteRoom целиком: виртуальная клавиатура держит D, Дэнни должен дойти до кровати,
    /// лечь, пульс — погаснуть до нуля, после титров — загрузиться MainMenu.
    /// Запуск: Window → General → Test Runner → PlayMode, или batchmode -runTests -testPlatform PlayMode.
    /// </summary>
    public class WhiteRoomTests
    {
        Keyboard keyboard;

        InputSettings.BackgroundBehavior oldBackground;
        InputSettings.EditorInputBehaviorInPlayMode oldEditor;

        [SetUp]
        public void SetUp()
        {
            // В batchmode нет окна Game с фокусом — без этого виртуальная клавиатура сбрасывается.
            oldBackground = InputSystem.settings.backgroundBehavior;
            oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        [TearDown]
        public void TearDown()
        {
            InputSystem.settings.backgroundBehavior = oldBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditor;
            Time.timeScale = 1f;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        }

        void Hold(Key key, bool down)
        {
            InputSystem.QueueStateEvent(keyboard, down ? new KeyboardState(key) : new KeyboardState());
            InputSystem.Update();
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator DannyWalksRight_LiesDown_HeartStops_ThenMainMenu()
        {
            SceneManager.LoadScene("WhiteRoom");
            yield return null;
            yield return new WaitForSeconds(2f);

            var player = PlayerController.Instance;
            var sequence = Object.FindAnyObjectByType<WhiteRoomSequence>();
            Assert.IsNotNull(player, "в WhiteRoom нет PlayerController");
            Assert.IsNotNull(sequence, "в WhiteRoom нет WhiteRoomSequence");
            Assert.AreEqual(PlayerState.Normal, player.State, "управление не открылось после входа");

            // Влево нельзя.
            float x0 = player.transform.position.x;
            Hold(Key.A, true);
            yield return new WaitForSeconds(1f);
            Hold(Key.A, false);
            Assert.LessOrEqual(player.transform.position.x, x0 + 0.01f, "Дэнни ушёл влево");

            // Вправо идёт.
            Hold(Key.D, true);
            yield return new WaitForSeconds(1.5f);
            float x1 = player.transform.position.x;
            Debug.Log($"[Test] x {x0:0.00} → {x1:0.00} за 1.5 с");
            Assert.Greater(x1, x0 + 1f, "Дэнни не идёт вправо при зажатой D");

            // До кровати — ускоряем время, клавиша зажата (после остановки у кровати управление вернётся).
            Time.timeScale = 4f;
            float timeout = Time.realtimeSinceStartup + 40f;
            while (!sequence.InBed && Time.realtimeSinceStartup < timeout)
            {
                Hold(Key.D, true);
                yield return null;
            }
            Hold(Key.D, false);
            Debug.Log($"[Test] у кровати: x = {player.transform.position.x:0.00}");
            Assert.IsTrue(sequence.InBed, $"Дэнни не дошёл до кровати, остановился на x = {player.transform.position.x:0.00}");
            Assert.AreEqual(PlayerState.Locked, player.State, "на кровати управление не заблокировано");

            var heart = HeartbeatManager.Instance;
            timeout = Time.realtimeSinceStartup + 20f;
            while (heart.Bpm > 0f && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.AreEqual(0f, heart.Bpm, "пульс не погас до нуля");

            timeout = Time.realtimeSinceStartup + 30f;
            while (SceneManager.GetActiveScene().name != "MainMenu" && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name, "после титров не загрузилось меню");
            Assert.IsFalse(heart.Scripted, "в меню пульс остался в сценарном режиме");
        }
    }
}
