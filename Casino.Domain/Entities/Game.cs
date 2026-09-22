using System;
using System.Collections.Generic;
using System.Text;

namespace Casino.Domain.Entities
{
    public class Game
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = "";
        public decimal ApuestaMinima { get; set; }
        public decimal ApuestaMaxima { get; set; }
        public bool isActive { get; set; } 

        public ICollection<Bet> Bets { get; set; } = new List<Bet>();
    }
}
