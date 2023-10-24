using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSB.Exceptions;

public class InvalidHttpCodeException : Exception
{
    public InvalidHttpCodeException(int code)
    {
        Terminal.ERROR($"Invalid HTTP redirection code -> {code}");
    }
}

