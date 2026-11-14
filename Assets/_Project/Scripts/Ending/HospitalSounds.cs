using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Заглушки звуков палаты до прихода настоящих CC0-файлов: генерируются один раз при старте сцены.
    /// Писк монитора и непрерывный сигнал. Заглушку ИВЛ (шум) убрали — шипела; нужен настоящий звук.
    /// </summary>
    public static class HospitalSounds
    {
        const int Rate = 44100;
        const float MonitorHz = 960f;

        /// <summary>Один короткий писк кардиомонитора.</summary>
        public static AudioClip Beep()
        {
            var data = new float[(int)(Rate * 0.14f)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)Rate;
                // Мягкие края (по 8 мс), иначе слышен щелчок.
                float env = Mathf.Min(1f, t / 0.008f) * Mathf.Min(1f, (0.14f - t) / 0.008f);
                data[i] = 0.5f * env * Mathf.Sin(2f * Mathf.PI * MonitorHz * t);
            }
            return Make("Monitor_Beep_Fallback", data);
        }

        /// <summary>Непрерывный сигнал — ровно целое число периодов, чтобы шов петли не щёлкал.</summary>
        public static AudioClip Flatline()
        {
            int samples = Mathf.RoundToInt(Rate / MonitorHz * 960f); // 960 периодов ≈ 1 с
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
                data[i] = 0.4f * Mathf.Sin(2f * Mathf.PI * 960f * i / samples);
            return Make("Flatline_Fallback", data);
        }

        static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
