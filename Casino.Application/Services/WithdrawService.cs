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

            var reference = $"withdrawal:{Guid.NewGuid():N}";
            // El débito y su movimiento se confirman juntos para evitar saldo sin historial.
            if (!await _userRepository.TryDebitBalanceAndRecordAsync(userId, amount, TransactionType.Retiro, reference))
                throw new Exception("Saldo insuficiente en el casino para retirar esa cantidad.");

            bool walletSuccess;
            try
            {
                walletSuccess = await _walletService.WithdrawFromCasinoAsync(user.BilleteraID, amount);
            }
            catch
            {
                // Si el servicio externo falla, liberamos la reserva local.
                await _userRepository.CreditBalanceAndRecordAsync(userId, amount, TransactionType.Reintegro, $"refund:{reference}");
                throw;
            }
            if (!walletSuccess)
            {
                await _userRepository.CreditBalanceAndRecordAsync(userId, amount, TransactionType.Reintegro, $"refund:{reference}");
                throw new Exception("La billetera externa rechazó el pago.");
            }

            return (await _userRepository.GetBalanceAsync(userId))!.Value;
        }
    }
}
