namespace HSB;
public class TLSImpl
{
    public static void Main()
    {
        var custom = true;
        if(custom){
            new Custom().Run();
        }else{
            new SystemTLS().Run();
        }
    }
}