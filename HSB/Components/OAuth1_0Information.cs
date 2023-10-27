using System.Diagnostics.CodeAnalysis;

namespace HSB;

public struct OAuth1_0Information
{
    public readonly string access_token;
    public readonly string nonce;
    public readonly string token;
    public readonly string version;
    public readonly string signature_method;
    public readonly string timestamp;
    public readonly string consumer_key;
    public readonly string signature;

    public OAuth1_0Information(Dictionary<string, string> parameters)
    {
        access_token = parameters.TryGetValueFromDict("access_token", "");
        nonce = parameters.TryGetValueFromDict("oauth_nonce", "");
        token = parameters.TryGetValueFromDict("oauth_token", "");
        version = parameters.TryGetValueFromDict("oauth_version", "");
        signature_method = parameters.TryGetValueFromDict("oauth_signature_method", "");
        timestamp = parameters.TryGetValueFromDict("oauth_timestamp", "");
        consumer_key = parameters.TryGetValueFromDict("oauth_consumer_key", "");
        signature = parameters.TryGetValueFromDict("oauth_signature", "");
    }

    public readonly bool IsValid()
    {
        return access_token != "" && nonce != "" && token != "" && version != "" && signature_method != "" && timestamp != "" && consumer_key != "" && signature != "";
    }

    public override readonly string ToString()
    {
        return IsValid() ? $"Valid oAuth1.0 with timestamp {timestamp}, version {version}" : "No valid oAuth1.0 found";
    }

    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        if(obj is OAuth1_0Information auth){
            return access_token == auth.access_token && nonce == auth.nonce && token == auth.token && version == auth.version && signature_method == auth.signature_method && timestamp == auth.timestamp && consumer_key == auth.consumer_key && signature == auth.signature;
        }
        return false;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(access_token, nonce, token, version, signature_method, timestamp, consumer_key, signature);
    }

}
