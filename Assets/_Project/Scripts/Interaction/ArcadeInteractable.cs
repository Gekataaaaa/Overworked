namespace Overworked
{
    /// <summary>
    /// Аркадный автомат в Room_102 (комната отдыха): по E запускает Tiny UFO (TinyUfoMiniGame).
    /// Сыгранная партия — победа или поражение, неважно — закрывает задание «Finish work in room 102»;
    /// QUIT из меню автомата возвращает без выполнения.
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
