# Issue Tracker – Webapp 2, Oblig 2
NB: Dokument er fin skrevet med hjelp av AI

En nettapp laget med ASP.NET Core MVC (.NET 10). Administrator kan opprette prosjekter og saker og tildele saker til brukere. Innloggede brukere kan kommentere, og tildelte brukere kan oppdatere status.

**Brukt:** C#, ASP.NET Core MVC, Entity Framework Core, PostgreSQL, ASP.NET Core Identity og xUnit.

## Oppsett med egen PostgreSQL-database

Kjør kommandoene fra mappen som inneholder `PersonalAgent.slnx`.

1. Installer **.NET 10 SDK** og **PostgreSQL**, og start PostgreSQL-serveren. Installer EF-verktøyet hvis du ikke har det: `dotnet tool install --global dotnet-ef` (eller bruk eksisterende lokalt verktøyoppsett).
2. Opprett en tom database, for eksempel i pgAdmin, med navnet `personalagent_mvc`. Du kan også bruke SQL:

   ```sql
   CREATE DATABASE personalagent_mvc;
   ```

3. Konfigurer tilkoblingen for webprosjektet. Bruk for eksempel **User Secrets** (bytt ut brukernavn og passord):

   ```powershell
   dotnet user-secrets init --project PersonalAgent.Web
   dotnet user-secrets set "ConnectionStrings:MvcDatabase" "Host=localhost;Port=5432;Database=personalagent_mvc;Username=postgres;Password=DITT_PASSORD" --project PersonalAgent.Web
   ```

   Bruk en PostgreSQL-bruker som har rettigheter til å opprette tabeller i databasen. Ikke legg ekte passord i Git.

4. Opprett tabellene med EF Core-migrasjonene:

   ```powershell
   dotnet ef database update --project PersonalAgent.Infrastructure --startup-project PersonalAgent.Web
   ```

5. Start appen i **Development**:

   ```powershell
   dotnet run --project PersonalAgent.Web
   ```

   Åpne adressen som vises i terminalen. Rollene `Admin` og `User` opprettes automatisk. Nye registrerte brukere får rollen `User`.

**Valgfritt – utviklingsadministrator:** Legg inn `SeedAdmin:Email` og `SeedAdmin:Password` i User Secrets for å opprette en administratorkonto ved oppstart i Development. Tilsvarende kan `SeedDemo:Email` og `SeedDemo:Password` brukes for en demobruker. Bruk et passord som oppfyller Identity-kravene (minst 10 tegn, stor og liten bokstav, tall og spesialtegn). Ikke bruk slike testkontoer i produksjon.

## Tester

Testene oppretter og sletter **egne midlertidige databaser**. Sett tilkobling til en PostgreSQL-server der testbrukeren har rettighet til å opprette og slette databaser:

```powershell
$env:PERSONALAGENT_TEST_CONNECTION = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=DITT_PASSORD"
dotnet test PersonalAgent.slnx
```

Siste kontroll: **98 av 98 tester bestått**.

Se `docs/documentation.md` for forklaring og video for videoutkast.
