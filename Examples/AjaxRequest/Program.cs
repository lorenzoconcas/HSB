﻿using HSB;
using HSB.Constants;

Configuration c = new()
{
    Port = 8080,
    Address = "",
    StaticFolderPath = "./"
};

var ajaxHandler = (Request req, Response res) =>
{
    if (req.IsAjaxRequest)
    {
        res.Send("<h1>Ajax reply</h1>");
    }
    else
    {
        res.Send(HTTP_CODES.METHOD_NOT_ALLOWED);
    }

};

c.POST("/ajax", ajaxHandler);
c.GET("/ajax", ajaxHandler);

new Server(c).Start();
