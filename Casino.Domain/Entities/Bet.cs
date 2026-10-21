using System;
using System.Collections.Generic;
using System.Text;

namespace Casino.Domain.Entities
{
    public enum BetStatus { Pendiente = 1, Ganada = 2, Perdida = 3, Anulada = 4 }
    public enum RouletteBetType { Number = 1, Color = 2, Parity = 3, Dozen = 4, Split = 5, Corner = 6, HighLow = 7 }
    public class Bet
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; } 
        public User user { get; set; } = null!;
        public Guid GameId { get; set; } 
        public Game game { get; set; } = null!;
        public Guid? RouletteRoundId { get; set; }
        public RouletteRound? RouletteRound { get; set; }
        public Guid? IdempotencyKey { get; set; }
        public Guid TransactionId { get; set; }
        public Transaction? Transaction { get; set; } = null!; 
        public decimal MontoApostado { get; set;}
        public decimal MontoGanado { get; set; } = 0;
        public string DetallesJson { get; set; } = "";
        public BetStatus Estado { get; set; } 
        public DateTime Fecha { get; set; }


    }
}
