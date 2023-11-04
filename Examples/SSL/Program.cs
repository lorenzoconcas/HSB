using HSB;

/*
* This example shows how to set HSB in SSL mode.
* Note that HSB does not provide a certificate, you must provide your own.
* On macOS you can extract your localhost certificate to test on the machine
* */

var sslConfig = new SslConfiguration(
    certificatePath: "PATH_TO_CERTIFICATE",
    certificatePassword: "CERTIFICATE_PASSWORD"
);


Configuration c = new(){
    Port = 443,
    SslSettings = sslConfig
};

new Server(c).Start();