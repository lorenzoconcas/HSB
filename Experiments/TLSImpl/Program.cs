namespace HSB;
public class TLSImpl
{

    public static void Main(string[] args)
    {
        var custom = false;
        if(custom){
            new Custom(args).Run();
        }else{
            new SystemTLS(args).Run();
        }
    }
}