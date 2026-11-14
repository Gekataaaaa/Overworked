using System;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Затемнение экрана (FadePanel поверх всего HUD).
    /// ЗАГЛУШКА: колбэк вызывается сразу, анимацию делает P1.
    /// </summary>
    public class FadeController : SingletonBehaviour<FadeController>
    {
        /// <summary>Экран уходит в чёрное за sec секунд.</summary>
        public void FadeOut(float sec, Action done)
        {
            Debug.Log($"[UI] FadeOut({sec})");
            done?.Invoke();
        }

        /// <summary>Экран проявляется из чёрного за sec секунд.</summary>
        public void FadeIn(float sec, Action done)
        {
            Debug.Log($"[UI] FadeIn({sec})");
            done?.Invoke();
        }
    }
}
