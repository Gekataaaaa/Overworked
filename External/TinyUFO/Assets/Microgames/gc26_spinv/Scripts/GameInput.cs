using UnityEngine;

namespace GC26.Spinv
{
    // Обёртка над вводом: работает и со старым Input Manager, и с новым Input System
    public static class GameInput
    {
        // -1 = влево, 1 = вправо, 0 = стоим (A/D и стрелки)
        public static float Horizontal()
        {
    #if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetAxisRaw("Horizontal");
    #elif ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return 0f;
            float value = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) value -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) value += 1f;
            return value;
    #else
            return 0f;
    #endif
        }

        // Касание, неподвижное дольше этого, считаем залипшим (см. PointerHeld)
        const double StaleTouchSeconds = 1.0;

        // EnhancedTouch включается на время жизни корабля и выключается обратно (счётчик внутри Input System),
        // глобально ничего не остаётся
        public static void EnableTouch()
        {
    #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
    #endif
        }

        public static void DisableTouch()
        {
    #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
    #endif
        }

        // Палец на экране или зажатая мышь в редакторе: где находится в пикселях экрана
        public static bool PointerHeld(out Vector2 screenPosition)
        {
    #if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
            {
                screenPosition = Input.GetTouch(0).position;
                return true;
            }
            screenPosition = Input.mousePosition;
            return Input.GetMouseButton(0);
    #elif ENABLE_INPUT_SYSTEM
            // При частых касаниях (точно в Device Simulator) касание залипало «нажатым» в точке
            // прошлого клика: корабль тянуло туда, ни палец, ни клавиатура не помогали.
            // Поэтому касания берём из EnhancedTouch (он сам собирает начало и конец касания
            // из событий, даже если они пришли в одном кадре) и не верим касанию, которое
            // давно не обновлялось и стоит на месте: настоящий палец к этому времени корабль уже довёл
            if (UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled)
            {
                double now = UnityEngine.InputSystem.LowLevel.InputState.currentTime;
                foreach (var touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
                {
                    if (!touch.isInProgress) continue;
                    bool stale = touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary && now - touch.time > StaleTouchSeconds;
                    if (stale) continue;
                    screenPosition = touch.screenPosition;
                    return true;
                }
            }
            else if (UnityEngine.InputSystem.Touchscreen.current != null)
            {
                foreach (var touch in UnityEngine.InputSystem.Touchscreen.current.touches)
                {
                    if (!touch.isInProgress || !touch.press.isPressed) continue;
                    screenPosition = touch.position.ReadValue();
                    return true;
                }
            }
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                screenPosition = mouse.position.ReadValue();
                return true;
            }
            screenPosition = Vector2.zero;
            return false;
    #else
            screenPosition = Vector2.zero;
            return false;
    #endif
        }

        // Способности: Z, X, C (по порядку слотов)
        public static bool AbilityPressed(int index)
        {
    #if ENABLE_LEGACY_INPUT_MANAGER
            return index == 0 ? Input.GetKeyDown(KeyCode.Z) : index == 1 ? Input.GetKeyDown(KeyCode.X) : index == 2 && Input.GetKeyDown(KeyCode.C);
    #elif ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return false;
            switch (index)
            {
                case 0: return keyboard.zKey.wasPressedThisFrame;
                case 1: return keyboard.xKey.wasPressedThisFrame;
                case 2: return keyboard.cKey.wasPressedThisFrame;
                default: return false;
            }
    #else
            return false;
    #endif
        }

        // Вместо касания: Space (или Enter)
        public static bool ConfirmPressed()
        {
    #if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
    #elif ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame);
    #else
            return false;
    #endif
        }

        // Ключ клавиши способности для подписи в интерфейсе
        public static string AbilityKeyName(int index) => index switch { 0 => "Z", 1 => "X", 2 => "C", _ => "" };

        // Esc или P
        public static bool PausePressed()
        {
    #if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
    #elif ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame);
    #else
            return false;
    #endif
        }
    }
}
