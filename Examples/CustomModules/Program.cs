using HSB;

var c = new Configuration();

c.EnabledModules.Add("CustomModules.TestModule");

new Server(c).Start();