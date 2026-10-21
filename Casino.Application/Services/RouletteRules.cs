using Casino.Application.DTOs;
using Casino.Domain.Entities;

namespace Casino.Application.Services;

public static class RouletteRules
{
    private static readonly HashSet<int> RedNumbers = [1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36];

    public static void ValidateBet(RouletteBetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.RequestId == Guid.Empty) throw new ArgumentException("Debe indicar un identificador único para el intento de apuesta.");
        if (request.RoundId == Guid.Empty) throw new ArgumentException("Debe indicar la ronda.");
        if (request.Amount <= 0 || decimal.Round(request.Amount, 2) != request.Amount)
            throw new ArgumentException("El monto debe ser positivo y tener como máximo dos decimales.");
        _ = NormalizeSelection(request.Type, request.Selection);
    }

    public static string NormalizeSelection(RouletteBetType type, string selection)
    {
        var value = (selection ?? string.Empty).Trim().ToLowerInvariant();
        return type switch
        {
            RouletteBetType.Number when int.TryParse(value, out var number) && number is >= 0 and <= 36 => number.ToString(),
            RouletteBetType.Color when value is "red" or "black" => value,
            RouletteBetType.Parity when value is "even" or "odd" => value,
            RouletteBetType.Dozen when value is "1" or "2" or "3" => value,
            RouletteBetType.HighLow when value is "low" or "high" => value,
            RouletteBetType.Split => NormalizeNumberSet(value, 2, IsValidSplit),
            RouletteBetType.Corner => NormalizeNumberSet(value, 4, IsValidCorner),
            _ => throw new ArgumentException("Selección inválida para el tipo de apuesta indicado.")
        };
    }

    private static string NormalizeNumberSet(string value, int requiredCount, Func<int[], bool> isValid)
    {
        var parts = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != requiredCount || parts.Any(part => !int.TryParse(part, out var number) || number is < 0 or > 36))
            throw new ArgumentException("Selección inválida para el tipo de apuesta indicado.");

        var numbers = parts.Select(int.Parse).ToArray();
        if (numbers.Distinct().Count() != requiredCount || !isValid(numbers))
            throw new ArgumentException("Los números elegidos no forman una apuesta válida sobre el paño.");

        return string.Join(",", numbers.Order());
    }

    private static bool IsValidSplit(int[] numbers)
    {
        if (numbers.Contains(0)) return numbers.Length == 2 && numbers.Single(number => number != 0) is >= 1 and <= 3;
        var first = PositionOf(numbers[0]);
        var second = PositionOf(numbers[1]);
        return Math.Abs(first.Row - second.Row) + Math.Abs(first.Column - second.Column) == 1;
    }

    private static bool IsValidCorner(int[] numbers)
    {
        if (numbers.Contains(0)) return false;
        var positions = numbers.Select(PositionOf).ToArray();
        var rows = positions.Select(position => position.Row).Distinct().Order().ToArray();
        var columns = positions.Select(position => position.Column).Distinct().Order().ToArray();
        return rows.Length == 2 && rows[1] == rows[0] + 1 &&
               columns.Length == 2 && columns[1] == columns[0] + 1;
    }

    private static (int Row, int Column) PositionOf(int number) =>
        (2 - ((number - 1) % 3), (number - 1) / 3);

    public static bool IsWinner(RouletteBetDetails bet, int number)
    {
        if (number is < 0 or > 36) throw new ArgumentOutOfRangeException(nameof(number));
        var selection = NormalizeSelection(bet.Type, bet.Selection);
        return bet.Type switch
        {
            RouletteBetType.Number => number.ToString() == selection,
            RouletteBetType.Color => number != 0 && (RedNumbers.Contains(number) ? "red" : "black") == selection,
            RouletteBetType.Parity => number != 0 && (number % 2 == 0 ? "even" : "odd") == selection,
            RouletteBetType.Dozen => number != 0 && ((number - 1) / 12 + 1).ToString() == selection,
            RouletteBetType.HighLow => number != 0 && (number <= 18 ? "low" : "high") == selection,
            RouletteBetType.Split or RouletteBetType.Corner => selection.Split(',').Any(value => int.Parse(value) == number),
            _ => false
        };
    }

    public static decimal PayoutMultiplier(RouletteBetType type) => type switch
    {
        RouletteBetType.Number => 36m,
        RouletteBetType.Dozen => 3m,
        RouletteBetType.Color or RouletteBetType.Parity => 2m,
        RouletteBetType.HighLow => 2m,
        RouletteBetType.Split => 18m,
        RouletteBetType.Corner => 9m,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static bool IsRed(int number) => RedNumbers.Contains(number);
}
