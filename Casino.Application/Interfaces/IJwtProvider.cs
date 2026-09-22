using Casino.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Casino.Application.Application
{
    public interface IJwtProvider
    {
        string Generate(User user);
    }
}
