namespace Casino.Domain.Entities;

public sealed class RouletteConfiguration
{
    public int Id { get; set; }
    public int BettingDurationSeconds { get; set; }
    public string ChipValuesJson { get; set; } = "[10,25,50,100]";
    public DateTime UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
