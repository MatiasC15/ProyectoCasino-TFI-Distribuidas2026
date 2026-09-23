using Casino.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Casino.Domain.Entities;

namespace Casino.Application.Services
{
    public class DepositService
    {
        private readonly IUserRepository _userRepository;
        private readonly IWalletService _walletService;

        public DepositService(IUserRepository userRepository, IWalletService walletService)
        {
            _userRepository = userRepository;
            _walletService = walletService;
        }

        public async Task<decimal> ExecuteAsync(Guid userId, decimal amount)
        {
            if (amount <= 0) throw new Exception("El monto debe ser mayor a 0.");

            var user = await _userRepository.GetByIdAsync(userId);
            if(user == null) throw new Exception("Usuario no encontrado.");

            bool walletSucces = await _walletService.DepositToCasinoAsync(user.BilleteraID, amount);

            if(!walletSucces) throw new Exception("La billetera externa rechazo la conexion.");

            user.Saldo += amount;
            await _userRepository.UpdateAsync(user);
            return user.Saldo;
        }
    }
}
