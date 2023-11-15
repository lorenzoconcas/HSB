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
