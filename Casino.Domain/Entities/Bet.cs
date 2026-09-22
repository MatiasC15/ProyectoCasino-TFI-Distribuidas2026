using System;
using System.Collections.Generic;
using System.Text;

namespace Casino.Domain.Entities
{
    public enum BetStatus { Pendiente = 1, Ganada = 2, Perdida = 3, Anulada = 4 }
    public class Bet
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; } 
        public User user { get; set; } = null!;
        public Guid GameId { get; set; } 
        public Game game { get; set; } = null!;
        public Guid TransactionId { get; set; }
        public Transaction? Transaction { get; set; } = null!; 
        public decimal MontoApostado { get; set;}
        public decimal MontoGanado { get; set; } = 0;
        public string DetallesJson { get; set; } = "";
        public BetStatus Estado { get; set; } 
        public DateTime Fecha { get; set; }


    }
}
