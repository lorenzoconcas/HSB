/*namespace HSB;

class Header{
    public string alg;
    public string typ = "JWT";
}

class Payload{
    public string iss;
    public string sub;
    public string aud;
    public string exp;
    public string nbf;
    public string iat;
    public string jti;
}

public class JWT{
    public Header header;
    public Payload payload;
}

public class JWTManager{

    public static JWTManager? _instance = null;


    public JWTManager(){
        
    }

    public static JWTManager GetInstance(){
        _instance ??= new JWTManager();
        return _instance;
    }


    public string GenerateJWT(){
        return "";
    }

    public boolean VerifyJWT(JWT jwt){

    }
}*/