using HSB;

/*
* This example shows how to set HSB in SSL mode.
* Note that HSB does not provide a certificate, you must provide your own.
* On macOS you can extract your localhost certificate to test on the machine
* */

var arglist = args.ToList();
var port = arglist.Find(a => a.StartsWith("--port="))?.Split("=")[1] ?? "8443";
var certificatePath = arglist.Find(a => a.StartsWith("--cert="))?.Split("=")[1];
var certificatePassword = arglist.Find(a => a.StartsWith("--pass="))?.Split("=")[1];

certificatePassword = "lore";
certificatePath = "/Users/lore/Certificati/localhost.p12";
port = "8080";


var sslConfig = new SslConfiguration(
    certificatePath: certificatePath ?? throw new Exception("You must provide a certificate path"),
    certificatePassword: certificatePassword ?? throw new Exception("You must provide a certificate password")
){
    upgradeUnsecureRequests = false,
    SslPort = 443,
};


Configuration c = new(){
    Port = ushort.Parse(port),
    SslSettings = sslConfig,
};

new Server(c).Start();