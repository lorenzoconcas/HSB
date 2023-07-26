using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSB.Exceptions
{
    public class InvalidConfigurationParameterException : Exception
    {
        public InvalidConfigurationParameterException(string parameterName, string? reason) {
            string msg = $"The configuration contains an invalid parameter : {parameterName}";
            if(reason != null)
                msg += "\nReason:" + reason ;
            Terminal.ERROR(msg);
        }
    }
}
