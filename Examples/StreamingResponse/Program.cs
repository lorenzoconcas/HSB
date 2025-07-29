//this is the example shown on www.lorenzoconcas.github.io/HSB

using HSB;

Configuration c = new()
{
    Port = 8080, //you must be root to listen on port 80, so 8080 will be used instead (see http alternate port)
    Address = "" //with empty string the server will still listen to any address 
};




const string htmlPage = """
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <title>AI Assistant - Chat</title>
  <style>
    body { font-family: sans-serif; padding: 2rem; }
    textarea { width: 100%; height: 80px; margin-top: 1rem; font-size: 1rem; }
    #chat {
      margin-top: 1rem;
      background: #f5f5f5;
      padding: 1rem;
      border: 1px solid #ccc;
      height: 300px;
      overflow-y: auto;
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }
    .message {
      padding: 0.5rem 1rem;
      border-radius: 10px;
      max-width: 80%;
      white-space: pre-wrap;
    }
    .user {
      background-color: #d1e7dd;
      align-self: flex-end;
    }
    .assistant {
      background-color: #e2e3e5;
      align-self: flex-start;
    }
    #loading {
      display: inline-block;
      margin-left: 0.5rem;
      width: 1rem;
      height: 1rem;
      border-radius: 50%;
      background-color: #333;
      animation: blink 1s infinite;
    }
    @keyframes blink {
      0% { opacity: 0.2; }
      50% { opacity: 1; }
      100% { opacity: 0.2; }
    }
    button { margin-top: 0.5rem; padding: 0.5rem 1rem; font-size: 1rem; }
  </style>
</head>
<body>
  <h1>AI Assistant</h1>
  <textarea id="questionInput" placeholder="Type your question here..."></textarea><br />
  <button id="startBtn">Send question</button>

  <div id="chat"></div>

  <script>
    document.getElementById("startBtn").addEventListener("click", async () => {
      const chat = document.getElementById("chat");
      const question = document.getElementById("questionInput").value.trim();
      if (!question) return;

      // Mostra il messaggio dell'utente
      const userMsg = document.createElement("div");
      userMsg.className = "message user";
      userMsg.textContent = question;
      chat.appendChild(userMsg);

      // Prepara il messaggio dell'assistente
      const assistantMsg = document.createElement("div");
      assistantMsg.className = "message assistant";
      const loading = document.createElement("span");
      loading.id = "loading";
      assistantMsg.appendChild(loading);
      chat.appendChild(assistantMsg);
      chat.scrollTop = chat.scrollHeight;

      const response = await fetch("http://localhost:8080/streaming_service", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ question }),
      });

      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let fullText = "";

      while (true) {
        const { value, done } = await reader.read();
        if (done) break;
        const chunk = decoder.decode(value, { stream: true });
        fullText += chunk;
        assistantMsg.textContent = fullText;
        assistantMsg.appendChild(loading);
        chat.scrollTop = chat.scrollHeight;
      }

      loading.remove();
    });
  </script>
</body>
</html>
""";




c.GET("/", (Request req, Response res) =>
{
    //reply to request with an hello world
    res.SendHTMLContent(htmlPage);
});


c.POST("/streaming_service", async (Request req, Response res) =>
{
    // Estrai la domanda dal corpo JSON
    var body = req.Body;
    string question = "";
    try
    {
        var json = System.Text.Json.JsonDocument.Parse(body);
        question = json.RootElement.GetProperty("question").GetString() ?? "";
    }
    catch
    {
        await res.InitStream();
        await res.AddStreamChunk("I couldn't read your question.");
        await res.EndStream();
        return;
    }

    await res.InitStream();

    // Analisi semplificata della domanda
    string lower = question.ToLowerInvariant();
    string[] chunks;

    if (lower.Contains("meteo") || lower.Contains("tempo"))
    {
        chunks = new[]
        {
            "Checking current weather...",
            "Fetching weather data for your area...",
            "It's 24°C and sunny today. 🌤️"
        };
    }
    else if (lower.Contains("who are you") || lower.Contains("what are you"))
    {
        chunks = new[]
        {
            "Good question.",
            "I'm a virtual assistant designed to help you with your questions.",
            "I don't have a body, but I'm always here for you. 😊"
        };
    }
    else if (lower.Contains("hello") || lower.Contains("hi"))
    {
        chunks = new[]
        {
            "Hello there! 👋",
            "How can I assist you today?"
        };
    }
    else
    {
        chunks = new[]
        {
            "Analyzing your question...",
            "That's an interesting question.",
            "I might not have the perfect answer, but I'm doing my best to help.",
            "Let me know if you'd like to explore further!"
        };
    }

    foreach (var chunk in chunks)
    {
        await res.AddStreamChunk(chunk + "\n\n");
        await Task.Delay(1300);
    }

    await res.EndStream();
});


new Server(c).Start(true);