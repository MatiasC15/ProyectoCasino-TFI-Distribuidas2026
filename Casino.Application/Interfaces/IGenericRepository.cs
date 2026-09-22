using System;
using System.Collections.Generic;
using System.Text;

namespace Casino.Application.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(Guid id);
        Task AddAsync(T entity);
    }
}
