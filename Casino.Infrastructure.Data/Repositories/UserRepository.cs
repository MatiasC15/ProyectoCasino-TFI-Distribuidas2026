using Casino.Application.Interfaces;
using Casino.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Casino.Infrastructure.Data.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByDniAsync(string dni)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Dni == dni);
        }

        public async Task<bool> TryDebitBalanceAsync(Guid userId, decimal amount)
        {
            return await _dbSet
                .Where(user => user.Id == userId && user.Saldo >= amount)
                .ExecuteUpdateAsync(update => update.SetProperty(user => user.Saldo, user => user.Saldo - amount)) == 1;
        }

        public async Task<decimal> CreditBalanceAsync(Guid userId, decimal amount)
        {
            var updated = await _dbSet
                .Where(user => user.Id == userId)
                .ExecuteUpdateAsync(update => update.SetProperty(user => user.Saldo, user => user.Saldo + amount));
            if (updated != 1) throw new InvalidOperationException("Usuario no encontrado.");
            return (await GetBalanceAsync(userId))!.Value;
        }

        public async Task<decimal> CreditBalanceAndRecordAsync(Guid userId, decimal amount, TransactionType type, string reference)
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            var updated = await _dbSet
                .Where(user => user.Id == userId)
                .ExecuteUpdateAsync(update => update.SetProperty(user => user.Saldo, user => user.Saldo + amount));
            if (updated != 1) throw new InvalidOperationException("Usuario no encontrado.");

            _context.Transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(), UserId = userId, Tipo = type, Monto = amount,
                Fecha = DateTime.UtcNow, ReferenciaExterna = reference
            });
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return (await GetBalanceAsync(userId))!.Value;
        }

        public async Task<bool> TryDebitBalanceAndRecordAsync(Guid userId, decimal amount, TransactionType type, string reference)
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            var updated = await _dbSet
                .Where(user => user.Id == userId && user.Saldo >= amount)
                .ExecuteUpdateAsync(update => update.SetProperty(user => user.Saldo, user => user.Saldo - amount));
            if (updated != 1)
            {
                await dbTransaction.RollbackAsync();
                return false;
            }

            _context.Transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(), UserId = userId, Tipo = type, Monto = amount,
                Fecha = DateTime.UtcNow, ReferenciaExterna = reference
            });
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return true;
        }

        public async Task<decimal?> GetBalanceAsync(Guid userId)
        {
            return await _dbSet.AsNoTracking().Where(user => user.Id == userId)
                .Select(user => (decimal?)user.Saldo).SingleOrDefaultAsync();
        }


    }
}
