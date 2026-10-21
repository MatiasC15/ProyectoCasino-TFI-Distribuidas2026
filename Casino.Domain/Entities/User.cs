using System;
using System.Collections.Generic;
using System.Text;


namespace Casino.Domain.Entities
{
    public enum Role { admin = 1, user = 2, superadmin = 3 }
    public class User
    {
        public Guid Id { get; set; }
        public string Dni { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
        public DateTime FechaNacimiento { get; set; }
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public decimal Saldo { get; set; }
        public Role Rol { get; set; } 
        public string BilleteraID { get; set; } = "";

        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<Bet> Bets { get; set; } = new List<Bet>();



    }
}
