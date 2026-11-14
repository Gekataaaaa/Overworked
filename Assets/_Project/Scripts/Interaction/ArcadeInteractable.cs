namespace Overworked
{
    /// <summary>
    /// Аркадный автомат в Room_102 (комната отдыха): по E запускает Tiny UFO (TinyUfoMiniGame).
    /// Победа закрывает задание «Finish work in room 102», QUIT возвращает без выполнения.
    /// </summary>
    public class ArcadeInteractable : MiniGameInteractable
    {
        protected override void Reset()
        {
            base.Reset();
            prompt = "Play";
        }
    }
}
