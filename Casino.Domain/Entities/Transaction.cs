using System;
using System.Collections.Generic;
using System.Text;

namespace Casino.Domain.Entities
{
    public enum TransactionType { Deposito = 1, Retiro = 2, Apuesta = 3, Premio = 4 }
    public class Transaction
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User user { get; set; } = null!;
        public TransactionType Tipo { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
        public string ReferenciaExterna { get; set; } = "";
    }
}
