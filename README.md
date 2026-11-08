# Personal Agent Core API

Dette er arbeidskrav 1 i Webapp 2. API-et lagrer interesser, preferanser og overvåkingsregler for hver bruker. Tanken er å bruke det videre i et personlig agentprosjekt, men denne versjonen har ikke AI eller faktisk overvåking.

NB! Dette dokumentet er omskrevet med hjelp av LLM verktøy for bedre struktur og rettskrivning, feil kan forekomme.

Prosjektet bruker .NET 10, EF Core, PostgreSQL, Identity og xUnit. Utviklingsmiljøet er Rider. Swagger brukes til å prøve API-et.

## Oppstart

Åpne PersonalAgent.slnx i Rider, eller bruk PowerShell. Kommandoene nedenfor kjøres fra prosjektmappen som inneholder PersonalAgent.slnx.

Prosjektet krever .NET 10 SDK. global.json angir 10.0.401 med latestPatch. Ved bruk av et annet SDK-feature-band må global.json eventuelt tilpasses. Første restore krever tilgang til NuGet. Docker med Compose er nødvendig dersom det medfølgende databaseoppsettet skal brukes.

Ved første kjøring på en ny maskin:

```powershell
dotnet restore PersonalAgent.slnx
dotnet tool restore
```

Databasen må kjøre før API-et startes. Følg databaseoppsettet nedenfor ved første kjøring. Når tilkoblingen er konfigurert og migrations er brukt, startes API-et med:

```powershell
dotnet run --project PersonalAgent.Api --launch-profile https
```

Swagger ligger på https://localhost:7008/swagger. Ved endrede porter brukes adressen i terminalen med /swagger til slutt. Stopp API-et med Ctrl+C.

## Database og konfigurasjon

PostgreSQL 17 kan kjøres lokalt eller på en tilgjengelig databaseserver. En eksisterende instans kan brukes dersom en database kalt personalagent og nødvendige tilgangsrettigheter er satt opp.

Det følger med et enkelt docker-compose.yml for lokal PostgreSQL. På en maskin med Docker: kopier .env.example til .env og skriv inn et eget databasepassord før du starter:

```powershell
Copy-Item .env.example .env
docker compose up -d
```

Compose-oppsettet bruker localhost:5433 og lagrer data i et vedvarende Docker-volum. Kontroller oppstarten med docker compose ps; postgres skal vise healthy.

Databasepassord konfigureres lokalt gjennom user-secrets eller miljøvariabler. .env.example viser nødvendig konfigurasjon for Docker. Faktiske passord og .env følger ikke innleveringen.

API-tilkoblingen kan lagres med user-secrets fra prosjektmappen:

```powershell
$dbCredential = Get-Credential -UserName personalagent -Message "Databasepassord"
$dbPassword = $dbCredential.GetNetworkCredential().Password
dotnet user-secrets set "ConnectionStrings:AgentDatabase" "Host=localhost;Port=5433;Database=personalagent;Username=personalagent;Password=$dbPassword" --project PersonalAgent.Api
Remove-Variable dbCredential, dbPassword
```

Bytt localhost til serveradressen hvis databasen kjører på en annen maskin. Eksemplet passer for et passord uten semikolon eller anførselstegn. User-secrets er lokal lagring utenfor prosjektet, men er ikke kryptert.

Opprett tabellene med migrations:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project PersonalAgent.Api --startup-project PersonalAgent.Api
```

Bruk migrations som allerede ligger i prosjektet. Du trenger ikke lage en ny InitialCreate for å starte på nytt. Hvis det lokale HTTPS-sertifikatet ikke er godkjent, kjør dotnet dev-certs https --trust.

## Bruke Swagger

Når API-et starter i Development, opprettes disse demokontoene hvis de ikke finnes:

- demo.a@example.com / DemoAgent!2026
- demo.b@example.com / DemoAgent!2026

Begge får en interesse, en preferanse og en regel. Eksisterende kontoer hoppes over, så oppstart tilbakestiller ikke data du har endret eller slettet.

Logg inn med POST /api/auth/login. Sett useCookies og useSessionCookies til false. Kopier accessToken fra svaret, trykk Authorize og lim inn tokenet uten Bearer foran. GET /api/account/me kan brukes til å sjekke at du er innlogget.

Du kan også registrere en ny konto med POST /api/auth/register. Tokenet er Identitys innebygde bearer-token, ikke JWT.

Interesser, preferanser og regler har endepunkter for oppretting, henting, endring og sletting. Brukeren får bare tilgang til egne data. En preferanse kan være generell eller knyttet til en interesse. En regel må være knyttet til en interesse.

## Kjør tester

Testene bruker ekte PostgreSQL og lager en egen midlertidig database per test. De trenger en databasebruker som kan opprette og slette databaser. Bruk utviklings-/testserveren, ikke en produksjonsdatabase.

Sett tilkoblingen i PowerShell-vinduet hvor testene skal kjøres:

```powershell
$dbCredential = Get-Credential -UserName personalagent -Message "Databasepassord for tester"
$dbPassword = $dbCredential.GetNetworkCredential().Password
$env:PERSONALAGENT_TEST_CONNECTION = "Host=localhost;Port=5433;Database=postgres;Username=personalagent;Password=$dbPassword"
Remove-Variable dbCredential, dbPassword
dotnet test PersonalAgent.slnx
```

Erstatt localhost med databaseadressen ved bruk av en server. Variabelen gjelder bare den aktuelle terminaløkten. Testdatabasene heter personalagent_test_<guid> og slettes etter bruk. Demodatabasen personalagent beholdes.

Verifisert resultat 9. oktober 2026: 8 testtilfeller, alle bestått. Testene kontrollerer tjenester og database. Innlogging og HTTP-svar er kontrollert manuelt i Swagger.

Ved feilsøking: 401 på et beskyttet kall betyr at du må sjekke tokenet. 401 ved selve innloggingen betyr at konto/passord må kontrolleres. PostgreSQL-feil 28P01 betyr feil databasepassord. Endring av .env alene endrer ikke passordet i en eksisterende database.

Mer om hvordan koden fungerer står i docs/documentation.md. Passordfilen .env og user-secrets skal ikke være med i innleveringen.
