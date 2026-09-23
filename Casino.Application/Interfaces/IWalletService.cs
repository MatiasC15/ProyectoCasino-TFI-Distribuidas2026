using System;
using System.Collections.Generic;
using System.Text;

namespace Casino.Application.Interfaces
{
    public interface IWalletService
    {
        Task<bool> DepositToCasinoAsync(string userWalletId, decimal amount);
        Task<bool> WithdrawFromCasinoAsync(string userWalletId, decimal amount);

    }
}
