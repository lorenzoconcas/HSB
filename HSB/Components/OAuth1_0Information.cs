using System.Diagnostics.CodeAnalysis;
namespace HSB.Components;


public readonly struct OAuth10Information(Dictionary<string, string> parameters)
{
    public readonly string AccessToken = parameters.GetValueOrDefault("access_token", "");
    public readonly string Nonce = parameters.GetValueOrDefault("oauth_nonce", "");
    public readonly string Token = parameters.GetValueOrDefault("oauth_token", "");
    public readonly string Version = parameters.GetValueOrDefault("oauth_version", "");
    public readonly string SignatureMethod = parameters.GetValueOrDefault("oauth_signature_method", "");
    public readonly string Timestamp = parameters.GetValueOrDefault("oauth_timestamp", "");
    public readonly string ConsumerKey = parameters.GetValueOrDefault("oauth_consumer_key", "");
    public readonly string Signature = parameters.GetValueOrDefault("oauth_signature", "");

    public readonly bool IsValid()
    {
        return AccessToken != "" && Nonce != "" && Token != "" && Version != "" && SignatureMethod != "" && Timestamp != "" && ConsumerKey != "" && Signature != "";
    }

    public override string ToString()
    {
        return IsValid() ? $"Valid oAuth1.0 with timestamp {Timestamp}, version {Version}" : "No valid oAuth1.0 found";
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if(obj is OAuth10Information auth){
            return AccessToken == auth.AccessToken && Nonce == auth.Nonce && Token == auth.Token && Version == auth.Version && SignatureMethod == auth.SignatureMethod && Timestamp == auth.Timestamp && ConsumerKey == auth.ConsumerKey && Signature == auth.Signature;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(AccessToken, Nonce, Token, Version, SignatureMethod, Timestamp, ConsumerKey, Signature);
    }

    public static bool operator ==(OAuth10Information left, OAuth10Information right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(OAuth10Information left, OAuth10Information right)
    {
        return !(left == right);
    }
}
