using Casino.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Casino.Domain.Entities;

namespace Casino.Application.Services
{
    public class WithdrawService
    {
        private readonly IUserRepository _userRepository;
        private readonly IWalletService _walletService;

        public WithdrawService(IUserRepository userRepository, IWalletService walletService)
        {
            _userRepository = userRepository;
            _walletService = walletService;
        }

        public async Task<decimal> ExecuteAsync(Guid userId, decimal amount)
        {
            if (amount <= 0) throw new Exception("El monto a retirar debe ser mayor a cero.");

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new Exception("Usuario no encontrado.");

            // 1. Chequeamos que tenga plata en el casino antes de pedirle a la billetera que envíe
            if (user.Saldo < amount)
                throw new Exception("Saldo insuficiente en el casino para retirar esa cantidad.");

            // 2. Le decimos a la billetera externa que le mande la plata a la cuenta del usuario
            bool walletSuccess = await _walletService.WithdrawFromCasinoAsync(user.BilleteraID, amount);

            if (!walletSuccess)
                throw new Exception("La billetera externa rechazó el pago.");

            // 3. Descontamos el saldo de nuestro Postgres
            user.Saldo -= amount;
            await _userRepository.UpdateAsync(user);

            return user.Saldo;
        }
    }
}
