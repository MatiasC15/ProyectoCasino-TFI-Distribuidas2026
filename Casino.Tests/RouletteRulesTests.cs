using Casino.Application.DTOs;
using Casino.Application.Services;
using Casino.Domain.Entities;

namespace Casino.Tests;

[TestClass]
public sealed class RouletteRulesTests
{
    [TestMethod]
    public void EuropeanRouletteRulesCoverEveryPocketAndPayout()
    {
        for (var number = 0; number <= 36; number++)
        {
            Assert.IsTrue(RouletteRules.IsWinner(new RouletteBetDetails(RouletteBetType.Number, number.ToString()), number));
            Assert.IsFalse(RouletteRules.IsRed(number) && RouletteRules.IsWinner(new RouletteBetDetails(RouletteBetType.Color, "black"), number));
        }

        Assert.IsTrue(RouletteRules.IsWinner(new RouletteBetDetails(RouletteBetType.Color, " red "), 1));
        Assert.IsTrue(RouletteRules.IsWinner(new RouletteBetDetails(RouletteBetType.Color, "black"), 2));
        Assert.IsTrue(RouletteRules.IsWinner(new RouletteBetDetails(RouletteBetType.Parity, "even"), 36));
        Assert.IsTrue(RouletteRules.IsWinner(new RouletteBetDetails(RouletteBetType.Dozen, "2"), 24));
        Assert.IsFalse(RouletteRules.IsWinner(new RouletteBetDetails(RouletteBetType.Parity, "even"), 0));
        Assert.IsFalse(RouletteRules.IsWinner(new RouletteBetDetails(RouletteBetType.Color, "red"), 0));

        Assert.AreEqual(36m, RouletteRules.PayoutMultiplier(RouletteBetType.Number));
        Assert.AreEqual(3m, RouletteRules.PayoutMultiplier(RouletteBetType.Dozen));
        Assert.AreEqual(2m, RouletteRules.PayoutMultiplier(RouletteBetType.Color));
        Assert.AreEqual(2m, RouletteRules.PayoutMultiplier(RouletteBetType.Parity));
        Assert.AreEqual(18m, RouletteRules.PayoutMultiplier(RouletteBetType.Split));
        Assert.AreEqual(9m, RouletteRules.PayoutMultiplier(RouletteBetType.Corner));
        Assert.AreEqual(2m, RouletteRules.PayoutMultiplier(RouletteBetType.HighLow));
        Assert.Throws<ArgumentOutOfRangeException>(() => RouletteRules.IsWinner(new RouletteBetDetails(RouletteBetType.Number, "1"), 37));
    }

    [TestMethod]
    public void SplitCornerAndLowHighBetsFollowTheFeltLayout()
    {
        Assert.AreEqual("1,4", RouletteRules.NormalizeSelection(RouletteBetType.Split, "4, 1"));
        Assert.AreEqual("0,3", RouletteRules.NormalizeSelection(RouletteBetType.Split, "3,0"));
        Assert.IsTrue(RouletteRules.IsWinner(new RouletteBetDetails(RouletteBetType.Split, "1,4"), 4));
        Assert.IsFalse(RouletteRules.IsWinner(new RouletteBetDetails(RouletteBetType.Split, "1,4"), 2));
        Assert.AreEqual("1,2,4,5", RouletteRules.NormalizeSelection(RouletteBetType.Corner, "5,1,4,2"));
        Assert.IsTrue(RouletteRules.IsWinner(new RouletteBetDetails(RouletteBetType.Corner, "1,2,4,5"), 5));
        Assert.IsTrue(RouletteRules.IsWinner(new RouletteBetDetails(RouletteBetType.HighLow, "low"), 18));
        Assert.IsTrue(RouletteRules.IsWinner(new RouletteBetDetails(RouletteBetType.HighLow, "high"), 19));
        Assert.IsFalse(RouletteRules.IsWinner(new RouletteBetDetails(RouletteBetType.HighLow, "low"), 0));

        Assert.Throws<ArgumentException>(() => RouletteRules.NormalizeSelection(RouletteBetType.Split, "1,5"));
        Assert.Throws<ArgumentException>(() => RouletteRules.NormalizeSelection(RouletteBetType.Corner, "1,2,4,6"));
        Assert.Throws<ArgumentException>(() => RouletteRules.NormalizeSelection(RouletteBetType.Corner, "0,1,2,3"));
    }

    [TestMethod]
    public void RouletteRejectsInvalidSelectionsAmountsAndRequestIds()
    {
        var roundId = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => RouletteRules.ValidateBet(
            new RouletteBetRequest(Guid.NewGuid(), roundId, 1m, RouletteBetType.Number, "37")));
        Assert.Throws<ArgumentException>(() => RouletteRules.ValidateBet(
            new RouletteBetRequest(Guid.NewGuid(), roundId, 0m, RouletteBetType.Color, "red")));
        Assert.Throws<ArgumentException>(() => RouletteRules.ValidateBet(
            new RouletteBetRequest(Guid.NewGuid(), roundId, 1.001m, RouletteBetType.Color, "red")));
        Assert.Throws<ArgumentException>(() => RouletteRules.ValidateBet(
            new RouletteBetRequest(Guid.Empty, roundId, 1m, RouletteBetType.Color, "red")));
        Assert.Throws<ArgumentException>(() => RouletteRules.ValidateBet(
            new RouletteBetRequest(Guid.NewGuid(), roundId, 1m, RouletteBetType.Color, "green")));
        Assert.Throws<ArgumentException>(() => RouletteRules.ValidateBet(
            new RouletteBetRequest(Guid.NewGuid(), roundId, 1m, RouletteBetType.Dozen, "0")));
    }
}
