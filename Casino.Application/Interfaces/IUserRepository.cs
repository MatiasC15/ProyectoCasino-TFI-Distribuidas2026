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
    }
}
