using UnityEngine;
using UnityEngine.UI;

namespace Overworked
{
    /// <summary>
    /// Анимированный шум («зерно» старого экрана) поверх фона меню: RawImage со случайной текстурой,
    /// которая каждые 1/fps секунды сдвигается в случайное место. Текстура генерируется сама при старте.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class UiNoise : MonoBehaviour
    {
        [Tooltip("Размер текстуры шума, пикселей (крупнее — мельче зерно на экране)")]
        [SerializeField] int textureSize = 256;

        [Tooltip("Во сколько раз текстура повторяется по высоте экрана (крупность зерна)")]
        [SerializeField] float tiling = 3f;

        [Tooltip("Смен кадра шума в секунду")]
        [SerializeField] float fps = 24f;

        RawImage image;
        float nextFrame;

        void Awake()
        {
            image = GetComponent<RawImage>();
            image.texture = CreateNoise(textureSize);
            image.raycastTarget = false;
        }

        void Update()
        {
            if (Time.unscaledTime < nextFrame) return;
            nextFrame = Time.unscaledTime + 1f / Mathf.Max(1f, fps);
            var rt = (RectTransform)transform;
            float aspect = rt.rect.height > 0f ? rt.rect.width / rt.rect.height : 1f;
            image.uvRect = new Rect(Random.value, Random.value, tiling * aspect, tiling);
        }

        static Texture2D CreateNoise(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                name = "UiNoise",
            };
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                byte v = (byte)Random.Range(0, 256);
                pixels[i] = new Color32(v, v, v, 255);
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }
    }
}
