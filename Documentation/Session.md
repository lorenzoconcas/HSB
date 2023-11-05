# The Session Class

A session is created every time a request without the cookie "hsbst" is made.
To get the session data the code is:

```cs
    Session sessionData = req.GetSession()
```

The session class is quiet simple for the moment, you can only Add or Get attributes

| Name                       | Return Type | Description                          |
| -------------------------- | ----------- | ------------------------------------ |
| SetAttribute<T>(string, T) | void        | Set an attribute for a given session |
| GetAttribute<T>(string)    | T           | Returns and attribute previously set |
