using Casino.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Casino.Application.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByDniAsync(string dni);

        Task UpdateAsync(User user);
        Task<bool> TryDebitBalanceAsync(Guid userId, decimal amount);
        Task<decimal> CreditBalanceAsync(Guid userId, decimal amount);
        Task<decimal> CreditBalanceAndRecordAsync(Guid userId, decimal amount, TransactionType type, string reference);
        Task<bool> TryDebitBalanceAndRecordAsync(Guid userId, decimal amount, TransactionType type, string reference);
        Task<decimal?> GetBalanceAsync(Guid userId);
    }
}
