# BlazorFeatures 1.2.5: guida architetturale per StarterProject e progetti add-on

> Documento operativo per agenti e sviluppatori. Prima di modificare o ampliare il sistema, leggere almeno le sezioni **Modello mentale**, **Render mode**, **Flusso di esecuzione**, **Creare una feature** e **Vincoli da non violare**.

## 1. Scopo e fonti analizzate

Questo progetto usa un'architettura **vertical slice a feature**: una singola operazione applicativa espone un contratto `Request/Response` comune, ma possiede due comportamenti differenti a seconda del luogo in cui il codice sta girando:

- nel browser WebAssembly, la feature client traduce l'operazione in una richiesta HTTP;
- nel processo server, la feature server esegue direttamente la business logic e può accedere a database, Identity, filesystem e altri servizi server-only.

Il documento descrive:

- lo Starter locale `StarterProject`;
- i pacchetti `BlazorFeatures.Abstractions`, `BlazorFeatures.Base.Client` e `BlazorFeatures.Base.Server`;
- il sorgente pubblico [Federicomang/BlazorFeatures.Base](https://github.com/Federicomang/BlazorFeatures.Base);
- il comportamento dei pacchetti **1.2.5** sul branch `master`, revisione [`af063569`](https://github.com/Federicomang/BlazorFeatures.Base/tree/af063569e5738c5f2fbc28da3b682119767d5edf) del 2026-09-03.

Lo Starter locale referenzia ancora la versione 1.1.6 nei due `.csproj`. Questa guida assume che i progetti host e gli add-on vengano allineati alla **1.2.5**. Le differenze di migrazione dalla 1.1.6 sono riepilogate nella sezione 5.3.

> **Nota sull'evoluzione locale:** i sorgenti attualmente presenti nei repository locali includono la pipeline successiva alla 1.2.5 descritta nelle sezioni 3, 6, 7 e 8: autorizzazione sulla feature concreta, `IHttpFeatureContext`, status HTTP in `FeatureResponse` e valori condivisi nel contesto. Queste API devono essere pubblicate con una nuova versione e i riferimenti dello Starter devono essere allineati prima di distribuire il progetto.

## 2. Modello mentale

Una feature non è un feature flag. È una **unità applicativa invocabile** composta da:

1. un tipo `Request`;
2. un tipo `Response`;
3. una implementazione client (`HandleClient`);
4. una implementazione server (`HandleServer`);
5. opzionalmente, uno o più endpoint HTTP;
6. opzionalmente, validazione, autorizzazione, servizi DI, componenti UI, policy ed estensioni EF Core.

Il chiamante usa sempre la stessa API:

```csharp
FeatureResponse<MyFeature.Response> result =
    await FeatureService.Run(new MyFeature.Request { /* ... */ });
```

Il chiamante non decide se eseguire `HandleClient` o `HandleServer`. Lo decide `FeatureService` in base al runtime corrente.

### 2.1 I tre pacchetti

| Pacchetto | Ruolo | Dove usarlo |
|---|---|---|
| `BlazorFeatures.Abstractions` | Contratti minimi indipendenti dall'hosting: request, response, service, context | Librerie di contratti o integrazioni che non necessitano del runtime Blazor completo |
| `BlazorFeatures.Base.Client` | Discovery, DI, dispatcher, render type, attributi ed estensioni client | Progetto `.Client` e, transitivamente, progetto server |
| `BlazorFeatures.Base.Server` | Endpoint, integrazione ASP.NET Core/EF Core e utility server | Solo progetto server |

Nel repository sorgente, il progetto che produce `BlazorFeatures.Base.Client` si chiama `BlazorFeatures.Base`. Non confondere il nome della cartella/progetto con il `PackageId` NuGet.

### 2.2 Topologia dei progetti

```text
BlazorFeatures.Abstractions
          ^
          |
BlazorFeatures.Base.Client
          ^
          |
BlazorFeatures.Base.Server

StarterProject.Client --------------------> Base.Client
          ^
          |
StarterProject (server) ------------------> Base.Server
```

Il progetto server referenzia anche `StarterProject.Client`. Questo è intenzionale: il server riusa gli stessi tipi `Request`, `Response` e la classe client come base della propria implementazione.

Per un add-on riutilizzabile, replicare la stessa forma:

```text
MyAddon.Client  --> BlazorFeatures.Base.Client
      ^
      |
MyAddon.Server  --> MyAddon.Client + BlazorFeatures.Base.Server
```

## 3. Contratti fondamentali

### `IBaseFeatureRequest<TResponse>`

Marca un oggetto come richiesta di una feature e lega staticamente la richiesta alla risposta.

```csharp
public sealed class Request : IBaseFeatureRequest<Response>
{
    public required Guid Id { get; init; }
}
```

### `IBaseFeature<TRequest, TResponse>`

Definisce i due lati della stessa operazione:

```csharp
Task<FeatureResponse<TResponse>> HandleClient(
    TRequest request,
    IFeatureContext featureContext,
    CancellationToken cancellationToken = default);

Task<FeatureResponse<TResponse>> HandleServer(
    TRequest request,
    IFeatureContext featureContext,
    CancellationToken cancellationToken = default);
```

### `IFeatureService`

È il punto di ingresso applicativo. `Run(...)`:

- ricava il tipo della feature dal tipo concreto della request e dal tipo response;
- risolve la feature da DI;
- crea o riutilizza un `IFeatureContext`;
- sceglie il lato client o server;
- restituisce sempre un `FeatureResponse<T>`.

### `FeatureResponse<T>`

Uniforma il risultato tra chiamate in-process e HTTP:

- `Success`: esito logico; in assenza del valore serializzato può derivare dallo status HTTP;
- `Data`: payload tipizzato;
- `Messages`: messaggi applicativi;
- `ValidationErrors`: errori per campo;
- `StatusCode`: status HTTP opzionale espresso tramite `HttpStatusCode` e preservato nelle conversioni della response;
- `HttpResponseMessage`: disponibile solo sul lato che ha effettuato HTTP, non serializzato.

Usare `AsSuccess`, `AsFailure` o `Create`; non restituire `null`.

### `IFeatureContext`

Rappresenta una singola operazione logica e contiene:

- `OperationId`: identificatore condiviso utile per log e correlazione;
- `InvocationSource`: origine iniziale (`Client`, `Server`, `Http`, `Internal` o `BackgroundJob`);
- `FeatureChain`: sequenza delle request attraversate;
- `Values`: `Dictionary<string, object>` per dati contestuali in-process condivisi tra feature annidate.

`FeatureService` aggiunge la request alla catena prima di autorizzazione, validazione ed esecuzione. Non inserire password, token o altri segreti in `Values`; usare chiavi qualificate per evitare collisioni tra add-on, ad esempio `"MyCompany.MyAddon.TenantId"`.

`Values` vive solo nella stessa esecuzione in-process: non viene serializzato né trasferito automaticamente dal contesto WASM al nuovo contesto HTTP server. Inoltre il dizionario non è thread-safe; una feature che avvia rami paralleli deve sincronizzarne l'accesso oppure evitare scritture concorrenti.

Quando una feature ne richiama un'altra e la catena deve rimanere unica, inoltrare esplicitamente il contesto:

```csharp
var childResult = await featureService.Run(childRequest, featureContext, cancellationToken);
```

Una chiamata `Run(childRequest)` senza `featureContext` crea una nuova catena. Attualmente la catena è tracciamento passivo: il framework non implementa automaticamente loop detection, transazioni o rollback.

Nel percorso HTTP il contesto concreto è `IHttpFeatureContext`: espone l'`HttpContext` della request e permette alla feature radice di associare un `IResult` custom senza usare `HttpContext.Items`.

## 4. `RenderType` e render mode Blazor sono concetti diversi

Questa distinzione è essenziale.

### 4.1 Render mode Blazor

Il render mode determina **dove vive l'istanza interattiva di un componente**:

| Render mode | Esecuzione interattiva | Trasporto degli eventi UI |
|---|---|---|
| Static SSR | server, solo rendering iniziale | nessuno: il componente non è interattivo |
| `InteractiveServer` | server | circuito Blazor su SignalR |
| `InteractiveWebAssembly` | browser | esecuzione locale .NET/WASM; HTTP quando serve il server |
| `InteractiveAuto` | inizialmente server, poi WASM nelle visite successive | dipende dalla scelta fatta all'avvio del componente |

Il prerendering è normalmente attivo per i render mode interattivi. Durante il prerender `RendererInfo.IsInteractive` è `false`.

Riferimento: [ASP.NET Core Blazor render modes (.NET 10)](https://learn.microsoft.com/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0).

### 4.2 `BlazorFeatures.Abstractions.Enums.RenderType`

`RenderType` descrive invece **in quale ambiente un assembly di feature è valido e deve essere registrato**:

- `Client`: implementazioni browser/WASM;
- `Server`: implementazioni server;
- `Both`: assembly utilizzabile in entrambi gli ambienti.

Si applica all'assembly:

```csharp
// MyAddon.Client/AssemblyInfo.cs
[assembly: FeatureAssembly(RenderType.Client)]
```

```csharp
// MyAddon.Server/AssemblyInfo.cs
[assembly: FeatureAssembly(RenderType.Server)]
```

`RenderType` non seleziona direttamente il render mode di una pagina e non indica la destinazione di un evento.

### 4.3 Come lo Starter sceglie il render mode globale

`AddFeatures` registra come cascading value `ApplicationRenderType`, configurabile tramite `FeatureConfigBuilder`. Il default è `RenderType.Both`.

In `StarterProject/Web/App.razor`:

```razor
<Routes @rendermode="ApplicationRenderMode" />
```

e `Constants.GetApplicationRenderMode(...)` effettua questa conversione:

| `ApplicationRenderType` | Render mode assegnato a `Routes` |
|---|---|
| `Client` | `InteractiveWebAssembly` |
| `Server` | `InteractiveServer` |
| `Both` | `null` |

Quindi **`Both` non significa `InteractiveAuto`**. Nello Starter significa che `Routes` non riceve un render mode interattivo globale; le pagine e i componenti possono creare isole interattive tramite `@rendermode` locale.

Esempi presenti:

- `Login.razor`: `InteractiveServer`;
- pagine utenti, `HeaderBar` e `NavMenu`: `InteractiveWebAssembly`;
- `MainLayoutWASM`: provider UI in un'isola WASM;
- `MainLayoutServer`: provider UI in un'isola server.

Il layout usa `ApplicationRenderType` e `RendererInfo.IsInteractive` per evitare di attivare contemporaneamente provider duplicati nel renderer sbagliato.

### 4.4 Come viene scelto `HandleClient` o `HandleServer`

La scelta non usa `RendererInfo`. `FeatureService` usa:

```csharp
RuntimeInformation.ProcessArchitecture == Architecture.Wasm
```

- nel runtime WASM chiama `HandleClient`;
- in qualunque processo non-WASM chiama `HandleServer`.

Conseguenza: una pagina `InteractiveServer` usa direttamente la feature server; una pagina `InteractiveWebAssembly` usa la feature client, che normalmente chiama un endpoint HTTP.

## 5. Discovery e registrazione DI

Entrambi gli host chiamano `AddSharedServices()`, che a sua volta chiama `services.AddFeatures()`.

In fase di startup, `AddFeatures` parte dagli assembly caricati nell'`AppDomain`, applica le aggiunte/rimozioni configurate in `FeatureConfigBuilder` e considera quelli marcati con `[FeatureAssembly(...)]`.

Per l'ambiente corrente:

1. determina `Client` se l'architettura è WASM, altrimenti `Server`;
2. registra le implementazioni dell'assembly compatibile con l'ambiente;
3. cataloga anche gli assembly/feature dell'altro lato, ma non ne registra le implementazioni nel container corrente;
4. registra `IFeatureService` come scoped;
5. registra le feature come scoped, salvo override;
6. registra container, componenti root, policy, option e hook trovati via reflection.

Lo Starter dichiara:

```csharp
// StarterProject.Client
[assembly: FeatureAssembly(RenderType.Client)]

// StarterProject server
[assembly: FeatureAssembly(RenderType.Server)]
```

### 5.1 Perché la coppia client/server risolve correttamente

Nel browser viene registrata, per esempio:

```csharp
StarterProject.Client.Features.Identity.GetUsers
    : IBaseFeature<GetUsers.Request, GetUsers.Response>
```

Nel server viene invece registrata la classe derivata:

```csharp
StarterProject.Features.Identity.GetUsers
    : StarterProject.Client.Features.Identity.GetUsers
```

L'interfaccia generica ereditata è la stessa. Pertanto la risoluzione DI di
`IBaseFeature<GetUsers.Request, GetUsers.Response>` produce:

- la classe client nel browser;
- la classe server nel processo ASP.NET Core.

### 5.2 Registrazione esplicita degli assembly in 1.2.5

La 1.2.5 consente di rendere deterministica la discovery senza dipendere dal fatto che il runtime abbia già caricato casualmente una DLL:

```csharp
services.AddFeatures(config =>
{
    config.AddAssemblyContaining<MyAddon.Client.AddonAssemblyMarker>();
    config.AddAssemblyContaining<MyAddon.Server.AddonAssemblyMarker>(); // solo host server
});
```

API disponibili:

- `AddAssemblyContaining<T>()`: aggiunge l'assembly che contiene `T`;
- `AddAssemblies(params Assembly[])`: aggiunge assembly già risolti;
- `RemoveAssembly(Assembly)`: esclude un assembly dalla scansione;
- `ApplicationRenderType`: configura il cascading value usato dall'app;
- `OptionsConfigurator`: modifica le option di feature scoperte.

La lista viene deduplicata prima della scansione. Per gli add-on usare sempre un marker pubblico e `AddAssemblyContaining<T>()`: è più esplicito e verificabile della sola scansione dell'`AppDomain`.

Se `AddSharedServices()` incapsula `AddFeatures()`, farle accettare una callback o una lista di assembly:

```csharp
public static IServiceCollection AddSharedServices(
    this IServiceCollection services,
    Action<FeatureConfigBuilder>? configureFeatures = null)
{
    services.AddFeatures(configureFeatures);
    // altri servizi condivisi...
    return services;
}
```

Nel client:

```csharp
builder.Services.AddSharedServices(config =>
    config.AddAssemblyContaining<MyAddon.Client.AddonAssemblyMarker>());
```

Nel server:

```csharp
builder.Services.AddSharedServices(config =>
{
    config.AddAssemblyContaining<MyAddon.Client.AddonAssemblyMarker>();
    config.AddAssemblyContaining<MyAddon.Server.AddonAssemblyMarker>();
});
```

### 5.3 Migrazione dello Starter da 1.1.6 a 1.2.5

Aggiornare insieme:

```xml
<!-- StarterProject.Client.csproj -->
<PackageReference Include="BlazorFeatures.Base.Client" Version="1.2.5" />

<!-- StarterProject.csproj -->
<PackageReference Include="BlazorFeatures.Base.Server" Version="1.2.5" />
```

Differenze rilevanti rispetto alla 1.1.6:

- discovery configurabile tramite `AddAssemblies`, `AddAssemblyContaining<T>` e `RemoveAssembly`;
- risoluzione di implementazioni feature open-generic tramite `FeatureTypeResolver`;
- overload HTTP e `FeatureResponse.FromHttpResponse` con `CancellationToken`;
- primitive SSE in `BlazorFeatures.Abstractions`;
- ordinamento query esteso con `QueryableFilterOptions<T,K>`;
- i package client e server marcano anche i propri assembly con `FeatureAssembly(Client/Server)`, permettendo la discovery del corretto handler interno delle policy;
- le policy vengono raccolte anche dagli assembly del render type opposto, pur senza registrarne le feature nel runtime corrente;
- `FeaturePolicyTools` è pubblico.

Il comportamento base di dispatch client/server, il modello di ereditarietà e `UseFeatureEndpoints` rimangono invariati.

### 5.4 Feature generiche in 1.2.5

La 1.2.5 aggiunge `FeatureTypeResolver` per le implementazioni open-generic. La risoluzione segue questo ordine:

1. prova a risolvere direttamente `IBaseFeature<TRequest,TResponse>` da DI;
2. se non esiste una registrazione chiusa, cerca tra le classi feature generiche scoperte;
3. confronta ricorsivamente il template dell'interfaccia con i tipi concreti request/response;
4. verifica i generic constraint;
5. costruisce e risolve il tipo feature chiuso;
6. memorizza il risultato in cache per la coppia `(Request, Response)`.

Se non trova alcuna corrispondenza genera `InvalidOperationException`; fa lo stesso se più feature generiche corrispondono. Evitare template sovrapposti o ambigui e aggiungere test di risoluzione per ogni coppia concreta.

## 6. Flusso di esecuzione completo

### 6.1 Chiamata da Interactive WebAssembly

```text
Componente WASM
  -> IFeatureService.Run(request)
  -> risoluzione della feature Client
  -> HandleClient(request)
  -> HttpClient GET/POST /api/...
  -> endpoint ASP.NET Core
  -> HttpContext.RunFeature(request)
  -> creazione di HttpFeatureContext
  -> IFeatureService.Run(request, context) sul server
  -> ServerFeatureService
  -> HandleServer(request) della classe derivata
  -> FeatureResponse<T> con StatusCode oppure IResult custom
  -> scrittura della risposta HTTP
  -> HandleClient deserializza e restituisce al componente
```

### 6.2 Chiamata da Interactive Server

```text
Componente nel circuito Blazor Server
  -> IFeatureService.Run(request)
  -> risoluzione della feature Server
  -> ServerFeatureService
  -> HandleServer(request) direttamente in-process
  -> FeatureResponse<T> al componente
```

Non c'è un round-trip HTTP per la normale business logic. Questo è il motivo principale per cui esistono entrambi gli handler.

### 6.3 Eccezioni che richiedono comunque il browser

Alcune operazioni devono modificare cookie o seguire semantiche del browser. `DoLogin` è l'esempio dello Starter: durante una chiamata Interactive Server, il server usa `IJSRuntime` e `window.doRequest` per far eseguire al browser una vera `fetch` verso l'endpoint di login.

Non generalizzare questo workaround a tutte le feature. Usarlo solo quando la risposta deve realmente attraversare il browser, per esempio per cookie, redirect o API browser-specifiche.

## 7. Responsabilità di `ServerFeatureService`

Lo Starter fornisce una propria implementazione di `IServerFeatureService` e la registra scoped:

```csharp
builder.Services.AddScoped<IServerFeatureService, ServerFeatureService>();
```

Il wrapper centralizza:

- riconoscimento del percorso HTTP tramite `IHttpFeatureContext`;
- autorizzazione della **feature concreta** prima della business logic;
- validazione FluentValidation server-side;
- gestione differenziata delle eccezioni:
  - in-process: log senza payload sensibili e `FeatureResponse` fallita;
  - su HTTP: rilancio verso `ExceptionMiddleware`;
- propagazione delle cancellazioni senza trasformarle in errori applicativi o log 500;
- propagazione dello stesso `IFeatureContext` alle feature annidate.

La validazione cerca `IValidator<TRequest>` in DI. È disabilitabile applicando `[DisableServerFluentValidation]` al **tipo request**.

### 7.1 Autorizzazione indipendente dal trasporto

`FeatureService` passa a `IServerFeatureService` sia l'handler sia la feature concreta risolta da DI. Se la feature implementa `IBaseFeatureAuthorization`, il wrapper costruisce la policy ed esegue `IAuthorizationService` **prima** della validazione e di `HandleServer`.

Per HTTP usa `HttpContext.User`; per una chiamata in-process Interactive Server usa l'`AuthenticationStateProvider` dello Starter. Un'identità mancante produce `401 Unauthorized`, una policy non soddisfatta produce `403 Forbidden`.

La stessa policy va comunque applicata anche all'endpoint con `.RequireAuthorization(...)`: protegge il confine ASP.NET Core il prima possibile, mentre il controllo nella pipeline protegge le invocazioni in-process e le feature annidate. Una feature solo client o solo server è valida e non deve generare warning per la sola assenza della controparte.

## 8. Anatomia canonica di una feature con API

### 8.1 Lato client/contratto

```csharp
using BlazorFeatures.Abstractions;
using BlazorFeatures.Abstractions.Extensions;
using BlazorFeatures.Base;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace MyAddon.Client.Features.Widgets;

public class GetWidget(IServiceProvider services)
    : IBaseFeature<GetWidget.Request, GetWidget.Response>
{
    public sealed class Request : IBaseFeatureRequest<Response>
    {
        public required Guid Id { get; init; }
    }

    public sealed class Response
    {
        public required WidgetDto Widget { get; init; }
    }

    protected const string ApiPath = "/api/widgets/{0}";

    // Risoluzione lazy: evita di richiedere HttpClient quando la classe base
    // viene costruita nel processo server.
    private HttpClient Client => services.GetRequiredService<HttpClient>();
    private JsonSerializerOptions? JsonOptions =>
        services.GetService<IOptions<JsonSerializerOptions>>()?.Value;

    public async Task<FeatureResponse<Response>> HandleClient(
        Request request,
        IFeatureContext featureContext,
        CancellationToken cancellationToken = default)
    {
        using var httpResponse = await Client.GetAsync(
            string.Format(ApiPath, request.Id), cancellationToken);

        return await httpResponse.AsFeatureResponse<Response>(
            JsonOptions, cancellationToken);
    }

    public virtual Task<FeatureResponse<Response>> HandleServer(
        Request request,
        IFeatureContext featureContext,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException(
            "L'implementazione server deve essere fornita da MyAddon.Server.");
}
```

La classe client possiede il contratto perché il progetto server può referenziare il client, mentre il browser non deve mai referenziare il server.

### 8.2 Lato server

```csharp
using BlazorFeatures.Abstractions;
using BlazorFeatures.Base.Server;
using BlazorFeatures.Base.Server.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using ClientGetWidget = MyAddon.Client.Features.Widgets.GetWidget;

namespace MyAddon.Server.Features.Widgets;

public sealed class GetWidget(
    IServiceProvider services,
    IWidgetRepository repository)
    : ClientGetWidget(services), IBaseFeatureEndpoint
{
    public override async Task<FeatureResponse<Response>> HandleServer(
        Request request,
        IFeatureContext featureContext,
        CancellationToken cancellationToken = default)
    {
        var widget = await repository.FindAsync(request.Id, cancellationToken);

        return widget is null
            ? FeatureResponse<Response>.AsFailure(
                messages: ["Widget non trovato."],
                statusCode: HttpStatusCode.NotFound)
            : FeatureResponse<Response>.AsSuccess(new Response
            {
                Widget = WidgetDto.From(widget)
            });
    }

    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/widgets/{id:guid}", async (
            HttpContext context,
            Guid id,
            [FromServices] IFeatureService featureService) =>
        {
            await context.RunFeature(
                featureService,
                new Request { Id = id });
        });
    }
}
```

### 8.3 Response normale e `IResult` custom

La stessa `HandleServer` deve poter restituire il proprio risultato sia a un componente Interactive Server sia via HTTP. Nel caso ordinario restituisce solo `FeatureResponse<T>` e valorizza `StatusCode`. `RunFeature` converte automaticamente la response in JSON con quello status; se manca, usa `200` per successo e `500` per fallimento.

Per operazioni come login, logout, challenge, redirect, download o streaming, il body JSON standard non è sufficiente. In quel caso la feature verifica il contesto e associa un risultato custom alla propria request:

```csharp
if (featureContext is IHttpFeatureContext httpContext)
{
    httpContext.SetHttpResult(request, Results.SignIn(principal));
}
```

L'`IResult` custom ha precedenza solo per la request proprietaria. Questo evita che una feature annidata sovrascriva accidentalmente la risposta HTTP della feature radice. Fuori da HTTP la feature deve comunque restituire un `FeatureResponse<T>` valido. Nessuno di questi dati tecnici viene conservato in `HttpContext.Items`.

## 9. Endpoint e API generate da un add-on

Una classe server che implementa `IBaseFeatureEndpoint` deve esporre:

```csharp
public static void MapEndpoints(IEndpointRouteBuilder builder)
```

Durante lo startup:

```csharp
app.UseFeatureEndpoints();
```

scansiona gli assembly server catalogati e invoca automaticamente tutti i `MapEndpoints`.

Linee guida:

- mantenere il path canonico nella feature client, così client e server non divergono;
- usare GET e query/route per letture idempotenti;
- usare JSON o form coerentemente su entrambi i lati;
- propagare il `CancellationToken`;
- applicare authorization e antiforgery in base al tipo di autenticazione;
- assegnare tag e metadata OpenAPI nell'endpoint server;
- evitare endpoint nascosti dentro assembly non marcati `FeatureAssembly`.

Lo Starter aggiunge transformer OpenAPI che riconoscono le request feature e descrivono `FeatureResponse<T>`; un add-on dovrebbe mantenere i tipi request visibili nei parametri dell'endpoint oppure fornire metadata OpenAPI espliciti.

## 10. Estensioni disponibili per add-on

### 10.1 Servizi aggiuntivi con `IFeatureRegistrationHandler`

Una classe concreta può implementare l'interfaccia e registrare dipendenze prima/dopo la registrazione automatica delle feature:

```csharp
public sealed class AddonRegistration : IFeatureRegistrationHandler
{
    public static void BeforeRegistration(IServiceCollection services)
        => services.AddOptions<MyAddonOptions>();

    public static void AfterRegistration(IServiceCollection services)
        => services.AddScoped<IWidgetRepository, WidgetRepository>();
}
```

La classe deve trovarsi in un assembly compatibile incluso nella scansione di `AddFeatures`.

### 10.2 Lifetime e interfacce aggiuntive

Il default delle feature è `Scoped`. È possibile cambiarlo:

```csharp
[FeatureServiceLifetime(ServiceLifetime.Singleton)]
```

Per esporre la stessa istanza anche tramite un'interfaccia applicativa:

```csharp
[FeatureOtherImplementation(typeof(IMyEventBus))]
public class EventBus : IBaseFeature<EventBus.Request, EmptyResponse>, IMyEventBus
```

Gli attributi sono ereditabili: se la classe server deriva dalla client, lifetime e interfacce aggiuntive vengono ereditati. Un singleton non deve dipendere da servizi scoped e deve essere thread-safe; sul server sarà condiviso tra richieste/circuiti.

### 10.3 Policy dichiarative

Una classe `IFeaturePolicy` espone staticamente la costruzione di una policy. `AddFeatures` la registra con `AddAuthorizationCore` nel client e `AddAuthorization` nel server. `[FeaturePolicyAuthorize<TPolicy>]` consente di referenziarla tramite attributo.

La policy client serve a governare l'interfaccia, non sostituisce mai l'autorizzazione server.

### 10.4 Componenti root

I componenti che implementano `IFeatureRootComponent` vengono raccolti in `FeatureRootComponentsManager`. `MainLayout.razor` li rende con `DynamicComponent` prima del layout principale.

È utile per provider, modali globali, listener o overlay portati da un add-on. Il componente deve comunque dichiarare o ereditare un render mode compatibile e non deve duplicare provider già presenti.

### 10.5 Estensioni EF Core

Un assembly server può fornire tipi `IDbContextExtension` con:

```csharp
public static void OnModelCreating(ModelBuilder builder)
```

Il framework li invoca soltanto se l'host chiama esplicitamente:

```csharp
featureContainer.AddToDbContext(modelBuilder);
```

Lo Starter attuale non effettua questa chiamata nel proprio `ApplicationDbContext`. Un add-on con entità EF non deve quindi presumere che la configurazione venga applicata automaticamente: integrare prima il container nel DbContext host e aggiungere migration nel progetto corretto.

### 10.6 Option di feature

Una classe di opzioni può implementare `IFeatureOptions<TFeature>`. `AddFeatures` ne crea un'istanza e applica l'eventuale `OptionsConfigurator` globale. Usare questo meccanismo per configurazione statica di feature; usare il normale options pattern dell'host quando servono binding da `IConfiguration`, validazione complessa o secret.

### 10.7 Paginazione e ordinamento query in 1.2.5

`BlazorFeatures.Base.Server` fornisce:

- `ToPaginatedListAsync(pageNumber, pageSize)`;
- `PaginateQuery(pageNumber, pageSize)`;
- `Filter(filter, orderStr, projection)`;
- un overload di `Filter` con `QueryableFilterOptions<TEntity,TModel>`.

La paginazione usa `Skip(pageNumber * pageSize)`, quindi `PageNumber` è **zero-based**. Se sia `pageNumber` sia `pageSize` sono minori o uguali a zero, la query non viene paginata.

Le option di filtro consentono:

- separatore multiplo configurabile, predefinito `|`;
- ordinamento sulle proprietà entity;
- ordinamento sul modello proiettato tramite prefisso predefinito `$`;
- espressioni di ordinamento custom fortemente tipizzate con `OrderExpression<T,TKey>`.

Poiché l'ordinamento non custom usa `System.Linq.Dynamic.Core`, non passare liberamente stringhe non validate provenienti da client esterni: adottare una allowlist di campi o `CustomEntityOrdering`/`CustomModelOrdering`.

## 11. Progettare eventi cross-render-mode

Un bus di eventi cross-render-mode non è soltanto una lista di delegate. Esistono almeno tre domini distinti:

```text
Isola WASM A ---- HTTP ----> Server process/circuit
     ^                           |
     |                           |
     +---- SSE/SignalR/WS -------+

Isola Server B: callback locali nel proprio circuito
```

Una implementazione add-on robusta dovrebbe separare:

1. **contratto evento**: nome/versione, payload e correlation ID nel progetto client/shared;
2. **dispatch locale**: subscriber appartenenti allo stesso runtime;
3. **ingresso browser -> server**: feature/API autenticata;
4. **uscita server -> browser**: SignalR, SSE o WebSocket;
5. **registry connessioni**: utente, tenant, circuito/tab e gruppi;
6. **delivery policy**: at-most-once/at-least-once, ordinamento, replay e retry;
7. **protezione loop**: event ID, origin e hop count;
8. **lifecycle**: subscribe/unsubscribe, reconnessione e cleanup.

La `FeatureContext.FeatureChain` può aiutare nel tracciamento di una catena sincrona di feature, ma non sostituisce correlation ID persistenti per messaggi asincroni o distribuiti.

### Stato dell'`EventManager` presente nello Starter

Il codice attuale è uno scheletro utile per il dispatch locale e mostra:

- registrazione singleton;
- esposizione tramite `IEventManager` con `FeatureOtherImplementation`;
- subscribe/unsubscribe;
- serializzazione del payload;
- intenzione di inoltrare client/server in base a `EventType`.

Non considerarlo ancora un trasporto cross-render-mode completo:

- `StarterProject/Features/Private/EventManager.MapEndpoints` è vuoto;
- `HandleServer` restituisce subito successo;
- non esiste il canale server -> browser attivo;
- i path `send/pipe` non sono mappati;
- autenticazione, isolamento utenti, retry e reconnessione non sono completati.

Per un nuovo progetto di eventi, riusare il pattern feature e l'interfaccia pubblica, non assumere che l'implementazione Starter sia pronta per produzione.

### Supporto SSE della 1.2.5

La 1.2.5 espone `HttpClientExtensions.SSE<T>`. Il metodo:

1. invia un `HttpRequestMessage` con `ResponseHeadersRead`;
2. se lo status non è di successo o il media type non è `text/event-stream`, converte la risposta HTTP in `FeatureResponse<T>`;
3. legge gli eventi SSE e invoca `ISseRequest.OnEventSse(...)` per ciascuno;
4. cerca un evento finale il cui tipo predefinito è `feature_response`;
5. deserializza quell'evento come `FeatureResponse<T>`;
6. restituisce failure se lo stream termina senza l'evento finale.

`SseOptions<T>` permette di configurare:

- `ResponseEventKeyword`;
- `JsonSerializerOptions`;
- `ResponseCustomDeserialize`.

Questo è un parser/adapter client, non un event bus completo: l'add-on deve ancora implementare endpoint SSE server, autenticazione, heartbeat, replay/reconnect e registry dei destinatari. Su .NET 10 usa `System.Net.ServerSentEvents.SseParser`; per target precedenti include un parser compatibile interno.

## 12. Struttura consigliata di un add-on

```text
MyAddon.Client/
  AssemblyInfo.cs                 # FeatureAssembly(Client)
  AddonAssemblyMarker.cs
  Features/
    Widgets/
      GetWidget.cs                # Request, Response, HandleClient
  Models/
    WidgetDto.cs
  Components/                     # pagine/isole browser-safe
  Registration/                   # hook e option client-safe

MyAddon.Server/
  AssemblyInfo.cs                 # FeatureAssembly(Server)
  AddonAssemblyMarker.cs
  Features/
    Widgets/
      GetWidget.cs                # eredita client, HandleServer, endpoint
  Database/
    Entities/
    AddonDbContextExtension.cs
  Registration/                   # servizi server-only
```

Dipendenze consigliate:

```text
MyAddon.Client:
  BlazorFeatures.Base.Client (stessa versione dello Starter)

MyAddon.Server:
  ProjectReference/PackageReference a MyAddon.Client
  BlazorFeatures.Base.Server (stessa versione dello Starter)
```

Non aggiungere riferimenti server, EF Core provider, secret o implementazioni privilegiate a `MyAddon.Client`: l'assembly può essere scaricato ed eseguito nel browser.

## 13. Checklist di integrazione host

### Client WebAssembly

- referenziare `MyAddon.Client`;
- includere esplicitamente l'assembly con `AddAssemblyContaining<MyAddon.Client.AddonAssemblyMarker>()`;
- registrare il named/default `HttpClient` atteso dalle feature;
- chiamare `AddSharedServices()`/`AddFeatures()` una sola volta;
- includere assembly di pagine nel `Router` (lo Starter usa `FeatureSystemContainerService.AllAssemblies`);
- aggiungere eventuali asset statici del Razor Class Library.

### Server

- referenziare `MyAddon.Server` e, transitivamente o direttamente, `MyAddon.Client`;
- includere esplicitamente gli assembly client e server con `AddAssemblyContaining<T>()`;
- registrare dipendenze server, DbContext e option;
- registrare `IServerFeatureService`;
- chiamare `app.UseFeatureEndpoints()` dopo routing/auth secondo la pipeline desiderata;
- abilitare entrambi i render mode se l'add-on li usa;
- aggiungere le assembly UI a `MapRazorComponents(...).AddAdditionalAssemblies(...)` quando necessario;
- integrare `AddToDbContext` prima di generare migration se l'add-on estende EF Core.

## 14. Vincoli da non violare

1. Il contratto `Request/Response` deve essere identico su client e server; definirlo una sola volta nel progetto client/shared.
2. La classe server deve ereditare la classe client o implementare esattamente la stessa `IBaseFeature<TRequest,TResponse>`.
3. `HandleServer` nel client deve essere `virtual` se il server lo sovrascrive.
4. L'assembly deve avere `[FeatureAssembly(RenderType.Client|Server|Both)]`.
5. Includere esplicitamente l'assembly nella configurazione 1.2.5 di `AddFeatures`.
6. Non mettere codice o dipendenze server-only nell'assembly client.
7. Non confondere `RenderType.Both` con `InteractiveAuto`.
8. Implementare `IBaseFeatureAuthorization` sulla classe feature server e replicare la policy sull'endpoint HTTP.
9. Non usare un singleton feature con dipendenze scoped o stato mutabile non sincronizzato.
10. Usare `HttpContext.RunFeature`; impostare un `IResult` custom solo quando la semantica HTTP non è rappresentabile da `FeatureResponse<T>`.
11. Propagare sempre il `CancellationToken` a HTTP, database e feature annidate.
12. Non usare il solo controllo UI/client come misura di sicurezza.
13. Allineare le versioni dei tre pacchetti BlazorFeatures tra host e add-on.
14. Riutilizzare lo stesso `IFeatureContext` e `CancellationToken` nelle feature annidate.
15. Non salvare segreti in `IFeatureContext.Values` e non serializzare request complete nei log.

## 15. Strategia di test minima

Per ogni feature/add-on verificare almeno:

- **client unit test**: `HandleClient` crea metodo, URL, query/body e headers corretti e deserializza `FeatureResponse<T>`;
- **server unit test**: `HandleServer` applica business logic, cancellation e mapping degli errori;
- **integration test HTTP**: endpoint, binding, validation, authorization, status code e schema risposta;
- **Interactive Server test**: la chiamata diretta non aggira authorization o assunzioni legate all'endpoint;
- **WASM/end-to-end test**: la stessa request attraversa client -> HTTP -> server;
- **render test**: prerender e fase interattiva non duplicano provider/subscription;
- **discovery test**: l'assembly add-on compare nel `FeatureSystemContainerService` corretto;
- **event add-on**: isolamento tra utenti/tab, reconnessione, duplicati, ordine e loop.

## 16. Procedura per un agente che crea un nuovo add-on

1. Verificare che host e add-on usino tutti BlazorFeatures 1.2.5.
2. Identificare i runtime supportati e i render mode dei componenti consumatori.
3. Creare la coppia `Addon.Client`/`Addon.Server` e i relativi `AssemblyInfo.cs`.
4. Definire nel client il contratto serializzabile `Request/Response` e il path API.
5. Implementare `HandleClient` come adapter di trasporto, senza business logic privilegiata.
6. Implementare nel server la business logic e, se serve, `IBaseFeatureEndpoint`.
7. Registrare servizi, policy, option, root component ed estensioni DB usando gli hook appropriati.
8. Aggiungere esplicitamente gli assembly alla discovery con `AddAssemblyContaining<T>()`.
9. Integrare routing e render mode senza confondere `RenderType` con `@rendermode`.
10. Testare separatamente il percorso WASM/HTTP e il percorso Interactive Server/in-process.
11. Eseguire una revisione di authorization, antiforgery, serializzazione e lifetime.
12. Documentare nel README dell'add-on le modifiche richieste a `Program.cs`, router, DbContext e pipeline.

## 17. Riferimenti rapidi nel repository Starter

- bootstrap server: `StarterProject/Program.cs`;
- bootstrap WASM: `StarterProject.Client/Program.cs`;
- registrazione condivisa: `StarterProject.Client/Extensions/ServiceExtensions.cs`;
- selezione render mode globale: `StarterProject/Web/App.razor`;
- assembly aggiuntivi del router: `StarterProject.Client/Routes.razor`;
- layout multi-renderer: `StarterProject.Client/Layout/MainLayout/`;
- wrapper server: `StarterProject/Features/ServerFeatureService.cs`;
- esempio GET: `StarterProject.Client/Features/Identity/GetUsers.cs` e `StarterProject/Features/Identity/GetUsers.cs`;
- esempio login browser-mediated: coppia `Features/Identity/DoLogin.cs`;
- esempio feature annidata: `StarterProject/Features/Identity/CreateUser.cs`;
- prototipo eventi: coppia `Features/Private/EventManager.cs`.

## 18. Sintesi finale

Il sistema realizza un'unica API applicativa (`IFeatureService.Run`) sopra due modalità di esecuzione:

```text
stesso Request/Response
        |
        +-- runtime WASM   -> HandleClient -> HTTP -> endpoint -> HandleServer
        |
        +-- runtime server -> HandleServer diretto
```

La separazione Client/Server protegge il confine browser-server e consente alla stessa UI di funzionare in render mode differenti. Gli add-on devono preservare questo confine, rendere esplicita la discovery degli assembly, duplicare correttamente i controlli di sicurezza sui due percorsi e trattare endpoint, UI globale, policy, database ed eventi come capacità opzionali costruite attorno alla feature, non come eccezioni al modello.
