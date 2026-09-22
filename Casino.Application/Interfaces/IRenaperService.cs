using System;
using System.Collections.Generic;
using System.Text;

namespace Casino.Application.Interfaces;

public interface IRenaperService
{
    Task<bool> EsMayorDeEdadAsync(string dni);
}