using System;
using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Мини-игра через MiniGameHost (сам блокирует Дэнни). Game — компонент, реализующий IMiniGame.
    /// С UntilWin выход без победы запускает мини-игру заново (печать отчёта, обязательные мини-игры).
    /// </summary>
    [Serializable]
    public class MiniGameStep : SequenceStep
    {
        [Tooltip("Компонент мини-игры (реализует IMiniGame)")]
        public MonoBehaviour Game;

        [Tooltip("Повторять, пока игрок не выиграет")]
        public bool UntilWin;

        public override string Describe() => $"Mini-game: {NameOf(Game)}";

        public override IEnumerator Run(ScriptedSequence sequence)
        {
            if (!(Game is IMiniGame game) || MiniGameHost.Instance == null)
            {
                Debug.LogWarning($"[Story] {sequence.name}: в MiniGameStep не мини-игра ({NameOf(Game)})");
                yield break;
            }
            bool won;
            do
            {
                bool finished = false;
                won = false;
                MiniGameHost.Instance.Open(game, w => { won = w; finished = true; });
                while (!finished) yield return null;
            } while (UntilWin && !won);
        }
    }
}
