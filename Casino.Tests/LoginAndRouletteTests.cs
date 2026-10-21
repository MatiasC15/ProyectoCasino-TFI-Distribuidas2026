using Casino.Application.Application;
using Casino.Application.DTOs;
using Casino.Application.Interfaces;
using Casino.Application.Services;
using Casino.Domain.Entities;
using Casino.Infrastructure.Data;
using Casino.Infrastructure.Data.Repositories;
using Casino.Infrastructure.Data.Migrations;
using Casino.Infrastructure.External;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;

namespace Casino.Tests;

[TestClass]
[DoNotParallelize]
public sealed class LoginAndRouletteTests
{
    private DbContextOptions<ApplicationDbContext> _options = null!;
    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public async Task InitializeDatabase()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(TestDatabaseFixture.ConnectionString)
            .Options;

        await using var context = new ApplicationDbContext(_options);
        await context.Database.MigrateAsync();
    }

    [TestMethod]
    public void EfSnapshotMatchesCurrentModel()
    {
        using var context = new ApplicationDbContext(_options);
        var snapshot = context.GetService<IMigrationsAssembly>().ModelSnapshot!.Model;
        var snapshotModel = context.GetService<IModelRuntimeInitializer>().Initialize(snapshot, designTime: true);
        var currentModel = context.GetService<IDesignTimeModel>().Model;
        var differ = context.GetService<IMigrationsModelDiffer>();
        var differences = differ.GetDifferences(snapshotModel.GetRelationalModel(), currentModel.GetRelationalModel());
        foreach (var operation in differences)
            TestContext.WriteLine(operation is DropIndexOperation drop
                ? $"DropIndex: {drop.Name} on {drop.Table}"
                : $"{operation.GetType().Name}: {System.Text.Json.JsonSerializer.Serialize(operation)}");
        Assert.IsEmpty(differences, "El snapshot EF debe coincidir con el modelo actual.");
    }

    [TestMethod]
    public async Task DepositCreatesFundedTestUserAndLoginAcceptsOnlyCorrectPassword()
    {
        var user = NewUser();
        await using var context = new ApplicationDbContext(_options);
        var repository = new UserRepository(context);
        await repository.AddAsync(user);

        var deposit = new DepositService(repository, new SuccessfulWallet());
        var balance = await deposit.ExecuteAsync(user.Id, 250m);
        Assert.AreEqual(250m, balance, "El depósito de prueba debe acreditar el saldo.");

        var jwtConfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:Secret"] = "UnitTestSecretKey_AtLeastThirtyTwoCharacters_LongEnough",
            ["JwtSettings:Issuer"] = "Casino.Tests",
            ["JwtSettings:Audience"] = "Casino.Tests.Client",
            ["JwtSettings:ExpirationInMinutes"] = "15"
        }).Build();
        var login = new LoginUserService(repository, new JwtProvider(jwtConfiguration));
        var response = await login.ExecuteAsync(new LoginRequest(user.Email, "TestPassword_2026"));
        Assert.AreEqual(user.Id, response.UserId);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.Token);
        Assert.AreEqual(user.Id.ToString(), token.Subject);
        Assert.AreEqual("Casino.Tests", token.Issuer);
        Assert.IsTrue(token.Audiences.Contains("Casino.Tests.Client"));
        Assert.AreEqual(250m, (await repository.GetBalanceAsync(user.Id)));

        var badPassword = await Assert.ThrowsAsync<Exception>(() =>
            login.ExecuteAsync(new LoginRequest(user.Email, "incorrecta")));
        Assert.AreEqual("Credenciales inválidas.", badPassword.Message);

        var unknownUser = await Assert.ThrowsAsync<Exception>(() =>
            login.ExecuteAsync(new LoginRequest("missing@example.test", "TestPassword_2026")));
        Assert.AreEqual("Credenciales inválidas.", unknownUser.Message);
    }

    [TestMethod]
    public async Task ConcurrentBetsCannotOverspendAndSettlementIsIdempotent()
    {
        var user = NewUser();
        Guid roundId;
        await using (var seedContext = new ApplicationDbContext(_options))
        {
            var repository = new UserRepository(seedContext);
            await repository.AddAsync(user);
            await new DepositService(repository, new SuccessfulWallet()).ExecuteAsync(user.Id, 100m);
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Roulette:BettingDurationSeconds"] = "60" })
            .Build();
        await using (var roundContext = new ApplicationDbContext(_options))
        {
            var service = new RouletteService(roundContext, new NoOpEventStream(), configuration);
            roundId = (await service.EnsureOpenRoundAsync(CancellationToken.None)).Id;
        }

        var first = new RouletteBetRequest(Guid.NewGuid(), roundId, 75m, RouletteBetType.Number, "0");
        var second = new RouletteBetRequest(Guid.NewGuid(), roundId, 75m, RouletteBetType.Number, "0");
        var attempts = await Task.WhenAll(
            PlaceBetInFreshContext(user.Id, first, configuration),
            PlaceBetInFreshContext(user.Id, second, configuration));

        var accepted = attempts.Where(attempt => attempt.Response is not null).ToArray();
        var rejected = attempts.Where(attempt => attempt.Error is not null).ToArray();
        Assert.HasCount(1, accepted, "Con dos apuestas de 75 y saldo 100 solo una puede confirmarse.");
        Assert.HasCount(1, rejected);
        Assert.AreEqual("Usuario inexistente o saldo insuficiente.", rejected[0].Error!.Message);

        var acceptedRequest = accepted[0].Request;
        var betId = accepted[0].Response!.BetId;
        await using (var replayContext = new ApplicationDbContext(_options))
        {
            var replay = await new RouletteService(replayContext, new NoOpEventStream(), configuration)
                .PlaceBetAsync(user.Id, acceptedRequest, CancellationToken.None);
            Assert.AreEqual(betId, replay.BetId, "El mismo requestId debe devolver la apuesta original.");
        }
        await using (var conflictContext = new ApplicationDbContext(_options))
        {
            var conflict = new RouletteBetRequest(acceptedRequest.RequestId, roundId, 74m, RouletteBetType.Number, "0");
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new RouletteService(conflictContext, new NoOpEventStream(), configuration)
                    .PlaceBetAsync(user.Id, conflict, CancellationToken.None));
        }

        decimal balanceBeforeSettlement;
        await using (var closeContext = new ApplicationDbContext(_options))
        {
            var round = await closeContext.RouletteRounds.SingleAsync(item => item.Id == roundId);
            round.BettingClosesAtUtc = DateTime.UtcNow.AddSeconds(-1);
            await closeContext.SaveChangesAsync();
            balanceBeforeSettlement = await closeContext.Users.Where(item => item.Id == user.Id)
                .Select(item => item.Saldo).SingleAsync();
        }

        await using (var settleContext = new ApplicationDbContext(_options))
        {
            await new RouletteService(settleContext, new NoOpEventStream(), configuration)
                .SettleRoundAsync(roundId, CancellationToken.None);
        }

        decimal balanceAfterSettlement;
        int winningNumber;
        await using (var verifyContext = new ApplicationDbContext(_options))
        {
            var round = await verifyContext.RouletteRounds.SingleAsync(item => item.Id == roundId);
            Assert.AreEqual(RouletteRoundState.Settled, round.State);
            winningNumber = round.WinningNumber!.Value;
            var bet = await verifyContext.Bets.SingleAsync(item => item.Id == betId);
            Assert.AreEqual(winningNumber == 0 ? BetStatus.Ganada : BetStatus.Perdida, bet.Estado);
            Assert.AreEqual(winningNumber == 0 ? 2700m : 0m, bet.MontoGanado);

            balanceAfterSettlement = await verifyContext.Users.Where(item => item.Id == user.Id)
                .Select(item => item.Saldo).SingleAsync();
            Assert.AreEqual(winningNumber == 0 ? balanceBeforeSettlement + 2700m : balanceBeforeSettlement, balanceAfterSettlement);
        }

        await using (var repeatContext = new ApplicationDbContext(_options))
        {
            await new RouletteService(repeatContext, new NoOpEventStream(), configuration)
                .SettleRoundAsync(roundId, CancellationToken.None);
        }
        await using (var finalContext = new ApplicationDbContext(_options))
        {
            var finalBalance = await finalContext.Users.Where(item => item.Id == user.Id).Select(item => item.Saldo).SingleAsync();
            Assert.AreEqual(balanceAfterSettlement, finalBalance, "Liquidar dos veces no debe duplicar premios.");
            Assert.AreEqual(1, await finalContext.Bets.CountAsync(item => item.RouletteRoundId == roundId));
        }

        await using (var closedRoundContext = new ApplicationDbContext(_options))
        {
            var closedRoundBet = new RouletteBetRequest(Guid.NewGuid(), roundId, 1m, RouletteBetType.Number, "0");
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new RouletteService(closedRoundContext, new NoOpEventStream(), configuration)
                    .PlaceBetAsync(user.Id, closedRoundBet, CancellationToken.None));
        }
    }

    private async Task<(RouletteBetRequest Request, RouletteBetResponse? Response, Exception? Error)> PlaceBetInFreshContext(
        Guid userId, RouletteBetRequest request, IConfiguration configuration)
    {
        try
        {
            await using var context = new ApplicationDbContext(_options);
            var response = await new RouletteService(context, new NoOpEventStream(), configuration)
                .PlaceBetAsync(userId, request, CancellationToken.None);
            return (request, response, null);
        }
        catch (Exception exception)
        {
            return (request, null, exception);
        }
    }

    private static User NewUser() => new()
    {
        Id = Guid.NewGuid(), Dni = Random.Shared.Next(10_000_000, 99_999_999).ToString(),
        Nombre = "Usuario", Apellido = "Prueba", FechaNacimiento = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Email = $"casino-test-{Guid.NewGuid():N}@example.test", PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword_2026"),
        Saldo = 0m, Rol = Role.user, BilleteraID = "test-wallet"
    };

    private sealed class SuccessfulWallet : IWalletService
    {
        public Task<bool> DepositToCasinoAsync(string userWalletId, decimal amount) => Task.FromResult(true);
        public Task<bool> WithdrawFromCasinoAsync(string userWalletId, decimal amount) => Task.FromResult(true);
    }

    private sealed class NoOpEventStream : IRouletteEventStream
    {
        public ValueTask PublishAsync(RouletteEvent rouletteEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public async IAsyncEnumerable<RouletteEvent> ReadAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
