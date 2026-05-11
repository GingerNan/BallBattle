public interface IEatable
{
    // 质量
    float Mass { get; set; }
    
    /// <summary>
    /// 被吃掉了
    /// </summary>
    /// <param name="playerBall">那个球吃的自己</param>
    void BeEaten(BallController playerBall);
}