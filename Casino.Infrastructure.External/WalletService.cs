using Casino.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

/*
 * 
 *TODO ESTO ESTA SIMULADO HASTA QUE UN GRUPO DE TRABAJO REALIZE SU WALLET 
 * 
 * NADA DE ESTO ES PARTE DEL CODIGO DE LA APLICACION, ES SIMULACION PARA QUE FUNCIONE EL PROYECTO
 * 
 */

namespace Casino.Infrastructure.External
{
    public class WalletService : IWalletService
    {
        public async Task<bool> DepositToCasinoAsync(string userWalletId, decimal mount)
        {
            await Task.Delay(500);

            return true;
        }

        public async Task<bool> WithdrawFromCasinoAsync(string userWalletId, decimal amount)
        {
            await Task.Delay(500);
            return true;
        }
    }
}
