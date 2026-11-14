namespace Overworked
{
    /// <summary>
    /// Компьютер: по E запускает мини-игру (Room_201 — «Настройка сигнала монитора» от P3).
    /// Вся логика — в MiniGameInteractable.
    /// </summary>
    public class ComputerInteractable : MiniGameInteractable
    {
        protected override void Reset()
        {
            base.Reset();
            prompt = "Check the computer";
        }
    }
}
