using Casino.Application.Application;
using Casino.Application.DTOs;
using Casino.Application.Interfaces;
using Casino.Application.Services;
using Casino.Domain.Entities;

namespace Casino.Tests;

[TestClass]
public class RegisterUserServiceTests
{
    [TestMethod]
    public async Task ExecuteAsync_StoresBirthDateAsUtcMidnight_WhenRequestDateHasNoTimezone()
    {
        var users = new InMemoryUserRepository();
        var service = new RegisterUserService(users, new AdultCheckStub(), new JwtProviderStub());
        var selectedBirthDate = new DateTime(2000, 8, 25, 0, 0, 0, DateTimeKind.Unspecified);
        var request = new RegisterUserRequest(
            "12345678", "Test", "User", selectedBirthDate, "test@example.invalid", "password123");

        await service.ExecuteAsync(request);

        Assert.IsNotNull(users.SavedUser);
        Assert.AreEqual(DateTimeKind.Utc, users.SavedUser.FechaNacimiento.Kind);
        Assert.AreEqual(selectedBirthDate.Date, users.SavedUser.FechaNacimiento);
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        public User? SavedUser { get; private set; }

        public Task<User?> GetByEmailAsync(string email) => Task.FromResult<User?>(null);
        public Task<User?> GetByDniAsync(string dni) => Task.FromResult<User?>(null);
        public Task AddAsync(User entity) { SavedUser = entity; return Task.CompletedTask; }
        public Task<User?> GetByIdAsync(Guid id) => Task.FromResult<User?>(null);
        public Task UpdateAsync(User user) => Task.CompletedTask;
        public Task<bool> TryDebitBalanceAsync(Guid userId, decimal amount) => throw new NotImplementedException();
        public Task<decimal> CreditBalanceAsync(Guid userId, decimal amount) => throw new NotImplementedException();
        public Task<decimal> CreditBalanceAndRecordAsync(Guid userId, decimal amount, TransactionType type, string reference) => throw new NotImplementedException();
        public Task<bool> TryDebitBalanceAndRecordAsync(Guid userId, decimal amount, TransactionType type, string reference) => throw new NotImplementedException();
        public Task<decimal?> GetBalanceAsync(Guid userId) => throw new NotImplementedException();
    }

    private sealed class AdultCheckStub : IRenaperService
    {
        public Task<bool> EsMayorDeEdadAsync(string dni) => Task.FromResult(true);
    }

    private sealed class JwtProviderStub : IJwtProvider
    {
        public string Generate(User user) => "test-token";
    }
}
