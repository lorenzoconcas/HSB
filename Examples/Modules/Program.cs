using HSB;

var c = new Configuration();

c.EnabledModules.Add("Modules.TestModule");

new Server(c).Start();