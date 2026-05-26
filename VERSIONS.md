# HSB Versions

Questo file documenta l'evoluzione del framework HSB. Le versioni piu recenti sono descritte in modo dettagliato per supportare migrazioni e manutenzione; le versioni storiche sono sintetizzate dalle informazioni disponibili nel repository, negli esempi e nella roadmap precedente.

HSB e ancora pre-1.0: fino a una release stabile le API possono cambiare. Le sezioni "Breaking changes" e "Migration notes" vanno controllate prima di aggiornare.

## 0.0.18

Versione corrente indicata da `HSB/Properties/AssemblyInfo.cs`.

### Breaking changes

- Rimossa completamente l'API servlet-style:
  - `Servlet`
  - `[Binding]`
  - `AssociateFile`
  - routing basato su ereditarieta e override `GET()`, `POST()`, ecc.
- Gli esempi e il Runner sono stati migrati a controller o route dirette su `Configuration`.
- Il modello WebSocket pubblico resta solo endpoint-based tramite `WebSocketConnection`, `config.WebSocket(...)` e `[Ws]`.

### Migration notes

- Usa `[Controller]` con `[Get]`, `[Post]`, `[Route]`, ecc. per route organizzate.
- Usa `config.Get(...)`, `config.Post(...)`, ecc. per route minimal.
- Usa `Response.SendHtmlFile(...)` dentro una route al posto di `AssociateFile`.

## 0.0.17

Versione precedente.

### Added

- Nuovo sistema di Controller moderni con attributi:
  - `[Controller("/path")]`
  - `[Get]`, `[Post]`, `[Put]`, `[Delete]`, `[Patch]`, `[Head]`, `[Options]`
  - `[Route("/path", HttpMethod.X)]`
- Supporto a route parametriche con segmenti `:name`.
- Injection di `Request` e `Response` nei metodi dei controller.
- Injection di `Request` e `Response` nei campi del controller.
- Supporto a parametri tipizzati tramite `[NamedParameter]`.
- Routing stile minimal API/Express tramite:
  - `config.Get(...)`
  - `config.Post(...)`
  - `config.Put(...)`
  - `config.Delete(...)`
  - `config.Patch(...)`
  - `config.Options(...)`
  - `config.WebSocket(...)`
- Nuovo modello WebSocket con `WebSocketConnection`.
- Endpoint WebSocket nei controller tramite `[Ws("/path")]`.
- Lifecycle WebSocket:
  - `OnOpen`
  - `OnMessage`
  - `OnClose`
  - `OnError`
- Invio WebSocket sync/async per testo e binario.
- Broadcast WebSocket per endpoint:
  - `Broadcast`
  - `BroadcastAsync`
  - `BroadcastExceptSelf`
  - `BroadcastExceptSelfAsync`
- Snapshot di header e query string nella connessione WebSocket.
- Streaming HTTP chunked:
  - `Response.InitStream(...)`
  - `Response.AddStreamChunk(...)`
  - `Response.EndStream()`
- Esempi e stress endpoint per streaming NDJSON.
- Stress test con endpoint realistici in `Experiments/StressTest`.

### Changed

- `Servlet` e deprecata con attributo `[Obsolete]`.
- Il routing moderno viene risolto prima del fallback static files.
- Le route minimal API sono trattate in modo uniforme rispetto alle route controller.
- Il parser HTTP e piu difensivo su request line, query string, header, cookie, auth e body.
- Gli header HTTP sono gestiti con dizionario case-insensitive.
- Parametri e cookie sono gestiti con dizionari case-insensitive.
- `Response` crea snapshot di header/cookie globali durante la generazione degli header.
- Il server imposta timeout di ricezione e invio sulle socket accettate.
- Il server legge gli header fino al delimitatore `\r\n\r\n` prima di costruire `Request`.
- Il runtime WebSocket usa lock/semafori per registrazione handler e invio frame.
- La gestione delle connessioni WebSocket usa strutture concorrenti.
- TLS/SSL e stato consolidato in `SslConfiguration`, con supporto a handler nativo e handler HSB sperimentale.

### Fixed

- Migliorata la resilienza a richieste malformate.
- Ridotto il rischio di crash su header/cookie/formati non validi.
- Migliorata la gestione di connessioni lente o incomplete.
- Migliorata la stabilita in scenari di connection storm.
- Migliorata la thread safety di WebSocket broadcast e invio frame.
- Migliorata la sicurezza del parsing di Basic/Bearer auth.
- Migliorata la gestione di chiusura socket in percorsi TLS, WebSocket e richieste invalide.

### Security

- Hardening anti-Slowloris tramite timeout e limiti sugli header.
- Limite massimo dimensione header.
- Limite massimo lunghezza request line.
- Limite massimo numero header.
- Chiusura anticipata delle connessioni che non completano gli header.
- Supporto a CORS globale.
- Supporto a allowlist/banlist IP tramite modulo `Filter`.
- TLS 1.0 e TLS 1.1 deprecati tramite `DeprecatedTLSVersionException`.

### Performance

- Test recenti osservati:
  - circa 18k req/s su endpoint minimale;
  - circa 8k req/s su JSON medio;
  - stabilita migliorata in stress concurrency;
  - resilienza migliorata a Slowloris;
  - stabilita migliorata con connection storm.
- I risultati sono indicativi e non sostituiscono benchmark riproducibili con ambiente, tool e configurazione fissati.

### Deprecated

- `Servlet` resta disponibile ma non e piu consigliata.
- Il modello WebSocket servlet-style/legacy va sostituito da `WebSocketConnection`.
- La documentazione precedente basata su `ProcessGet()`/`ProcessPOST()` e superata: l'API reale legacy usa `GET()`, `POST()`, ecc.

### Breaking changes

- I nuovi controller non ereditano da `Servlet`.
- Il codice nuovo dovrebbe usare attributi controller invece di `[Binding]`.
- I vecchi esempi WebSocket servlet-style non rappresentano piu il modello preferito.
- Il target del progetto core e `net10.0`; chi consuma la libreria deve allineare SDK/runtime.

### Migration notes

#### Servlet -> Controller

Prima:

```csharp
[Binding("/hello")]
public class HelloServlet : Servlet
{
    public HelloServlet(Request req, Response res) : base(req, res)
    {
    }

    public override void GET()
    {
        res.Send("hello");
    }
}
```

Dopo:

```csharp
[Controller("/hello")]
public class HelloController
{
    [Get("/")]
    private void Get(Response res)
    {
        res.Send("hello");
    }
}
```

#### WebSocket legacy -> WebSocketConnection

Prima: endpoint WebSocket modellati come servlet/classi legacy.

Dopo:

```csharp
config.WebSocket("/ws", socket =>
{
    socket.OnMessage(msg => socket.Send(msg.Text));
});
```

oppure:

```csharp
[Controller("/realtime")]
public class RealtimeController
{
    [Ws("/chat")]
    private void Chat(WebSocketConnection socket)
    {
        socket.OnMessage(msg => socket.BroadcastExceptSelf(msg.Text));
    }
}
```

## 0.0.16

Release/refactor intermedio ricostruito dalle modifiche presenti nel repository.

### Added

- Prime API di routing moderno stile minimal API/Express.
- Eventi interni su `Configuration` per registrazione route.
- Miglioramenti ai moduli request-level.
- Preparazione del nuovo router WebSocket.

### Changed

- Razionalizzazione della risoluzione route in `Server`.
- Maggiore separazione tra mapping delegate, controller e fallback statico.
- Miglioramento del lifecycle `Request`/`Response`.

### Fixed

- Correzioni su gestione header globali e cookie globali.
- Correzioni su route e injection di parametri base.

## 0.0.15

Release/refactor intermedio ricostruito dalle feature attuali.

### Added

- Prime basi del sistema Controller attribute-based.
- Attributi HTTP dedicati per controller.
- Supporto a route custom tramite `[Route]`.
- Prime integrazioni OpenAPI tramite attributi.

### Changed

- Spostamento progressivo dalla modellazione servlet a controller.
- Esempi iniziali in `Examples/ControllerExample`.

### Deprecated

- Inizio deprecazione concettuale del modello Servlet per nuovi progetti.

## 0.0.14

### Added

- Miglioramenti al parser HTTP.
- Miglior gestione di body, form data, multipart e upload.
- Miglioramenti al lifecycle session/cookie.

### Changed

- Parsing request piu tollerante verso input malformati.
- Infrastruttura piu pronta a request handler moderni.

### Fixed

- Fix su richieste invalide.
- Fix su cookie malformati e header anomali.

## 0.0.13

### Added

- Miglioramenti SSL/TLS.
- Supporto piu esteso a certificati `.p12/.pkcs12`.
- Preparazione del debug certificate locale.

### Changed

- `SslConfiguration` diventa il punto centrale per configurare HTTPS.
- Consolidamento del redirect HTTP -> HTTPS.

### Security

- Versioni TLS obsolete considerate deprecate.
- Supporto a revocation check e client certificate settings.

## 0.0.12

### Added

- Refactor del networking e gestione socket.
- Prime protezioni strutturate contro connessioni lente.
- Miglioramenti alla concorrenza nel server.

### Changed

- Lettura request piu controllata.
- Preparazione ai successivi limiti anti-Slowloris.

### Fixed

- Stabilita migliorata sotto carico.
- Riduzione di race condition nel percorso request/response.

## 0.0.11

Release presente in `Releases/` come `HSB_v0.0.11_ALPHA`.

### Added

- Supporto SSL/TLS iniziale.
- `SslConfiguration`.
- Modalita dual-port per HTTP/HTTPS.
- Redirect opzionale delle richieste non sicure.

### Changed

- Integrazione TLS nel ciclo di accettazione connessioni.

### Known limitations

- La vecchia roadmap indicava il custom TLS implementation come work in progress.

## 0.0.10

Release presente in `Releases/` come `HSB_v0.0.10`.

### Added

- Supporto WebSocket iniziale.
- Esempi WebSocket legacy.

### Changed

- Estensione del server oltre il solo HTTP request/response.

### Deprecated later

- Il modello WebSocket originale e stato superato dal sistema `WebSocketConnection`.

## 0.0.9

### Added

- Form data.
- File upload.
- Componenti multipart.

### Changed

- `Request` acquisisce funzionalita piu ampie per body e upload.

## 0.0.8

### Added

- Autenticazione HTTP.
- Supporto a Basic/Bearer e componenti auth.
- Esempi di autenticazione.

## 0.0.7

### Changed

- Refactor/cleanup pianificato nella roadmap storica.

### Notes

- La roadmap precedente indicava che questa versione poteva essere saltata.

## 0.0.6

### Added

- Implementazione delle sessioni HTTP.
- Gestione cookie/session token.

## 0.0.5 / 0.0.5 RC

### Added

- Debugging migliorato.
- Prime utility per sviluppo e diagnostica.

## 0.0.1 - 0.0.4

### Added

- Prime versioni del server HTTP.
- Routing servlet-based.
- `Request` e `Response` iniziali.
- Static files e pagine di default.
- Struttura base della libreria `HSB`.

## Roadmap versioning

### Prossime aree da stabilizzare

- API controller come superficie primaria.
- Migrazione completa esempi legacy.
- Test automatici per parser, timeout, WebSocket e streaming.
- Benchmark riproducibili con script e ambiente dichiarato.
- Documentazione manuale in `Documentation/` coerente con README e `VERSIONS.md`; `docs/` resta output Doxygen generato.
- Compatibilita e policy target framework.

### Verso 1.0

Prima di una 1.0 il progetto dovrebbe definire:

- API pubbliche stabili;
- policy di deprecazione;
- matrice runtime supportata;
- suite test automatica;
- benchmark riproducibili;
- documentazione completa per controller, WebSocket, streaming, TLS e moduli.
