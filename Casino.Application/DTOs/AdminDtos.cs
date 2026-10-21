using System.ComponentModel.DataAnnotations;

namespace Casino.Application.DTOs;

public sealed record RouletteSettingsResponse(
    int BettingDurationSeconds,
    decimal MinimumBet,
    decimal MaximumBet,
    decimal[] ChipValues,
    DateTime UpdatedAtUtc,
    string? UpdatedByEmail);

public sealed record UpdateRouletteSettingsRequest(
    int BettingDurationSeconds,
    decimal MinimumBet,
    decimal MaximumBet,
    decimal[] ChipValues);

public sealed record AdminRoundSummary(
    Guid Id,
    DateTime OpenedAtUtc,
    DateTime? SettledAtUtc,
    int? WinningNumber,
    int BetCount,
    decimal TotalWagered,
    decimal TotalPaid);

public sealed record AdminStatisticsResponse(
    int RegisteredUsers,
    int AdminUsers,
    int TotalRounds,
    int TotalBets,
    int RoundsToday,
    int BetsToday,
    decimal TotalWagered,
    decimal TotalPaid,
    decimal HouseNet,
    decimal TodayWagered,
    decimal TodayPaid,
    AdminRoundSummary[] RecentRounds);

public sealed record AdminUserSummary(Guid Id, string Name, string Email, string Role, decimal Balance);

public sealed record AdminAuditSummary(
    Guid Id,
    string Action,
    string DetailsJson,
    DateTime CreatedAtUtc,
    Guid? TargetUserId,
    string ActorEmail);

public sealed record CreateAdminRequest(
    [Required, RegularExpression(@"^\d{7,8}$")] string Dni,
    [Required, MinLength(2), MaxLength(100)] string Nombre,
    [Required, MinLength(2), MaxLength(100)] string Apellido,
    [Required] DateTime FechaNacimiento,
    [Required, EmailAddress] string Email,
    [Required, MinLength(12), MaxLength(128)] string Password);
