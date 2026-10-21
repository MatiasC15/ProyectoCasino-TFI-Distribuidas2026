namespace Casino.Domain.Entities;

public enum RouletteRoundState
{
    BettingOpen = 1,
    Settled = 2
}

public sealed class RouletteRound
{
    public Guid Id { get; set; }
    public RouletteRoundState State { get; set; }
    public DateTime OpenedAtUtc { get; set; }
    public DateTime BettingClosesAtUtc { get; set; }
    public DateTime? SettledAtUtc { get; set; }
    public int? WinningNumber { get; set; }
    public ICollection<Bet> Bets { get; set; } = new List<Bet>();
}
