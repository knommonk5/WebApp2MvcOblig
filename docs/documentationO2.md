# Dokumentasjon – Issue Tracker

**Emne:** Webapp 2 – Oblig 2  
**Løsning:** ASP.NET Core MVC med PostgreSQL

**NB:** Dokument er fin skrevet med hjelp av AI

## 1\. Hva er laget?

Jeg har laget en nettside som brukes til å holde orden på prosjekter og oppgaver. En oppgave kalles en *sak* eller *issue*. En sak kan for eksempel være en feil som må rettes, eller noe som må utvikles.

Administrator kan opprette prosjekter og saker, redigere dem og bestemme hvem som skal jobbe med en sak. Vanlige brukere kan logge inn, lese saker og skrive kommentarer. Hvis en sak er tildelt dem, kan de også endre status etter hvert som arbeidet går fremover.

Målet er å vise hvordan en nettside kan håndtere informasjon, brukere, tilgang og lagring i en database.

## 2\. Hva kan nettsiden gjøre?

* Vise, opprette, endre og slette prosjekter og saker. Endringer som krever administratortilgang, er beskyttet.
* Knytte saker til et prosjekt.
* Tildele en sak til en bruker, eller fjerne tildelingen.
* La innloggede brukere legge inn kommentarer på saker.
* La tildelte brukere oppdatere status i riktig rekkefølge.
* La administrator overstyre status, blant annet for å gjenåpne en lukket sak.
* Registrere brukere og håndtere innlogging med Identity.

## 3\. Slik er prosjektet delt opp

Løsningen består av fire kodeprosjekter:

|Prosjekt|Enkelt forklart|
|-|-|
|`PersonalAgent.Web`|Selve nettsiden: sider, skjemaer og kode som tar imot klikk og forespørsler.|
|`PersonalAgent.Core`|Beskriver hva et prosjekt, en sak og en kommentar er. Her ligger også statusene.|
|`PersonalAgent.Infrastructure`|Inneholder koden som arbeider med databasen og reglene for sakene.|
|`PersonalAgent.Web.Tests`|Automatiske tester som sjekker at løsningen virker.|

Jeg har delt opp koden for at det skal være lettere å finne frem, teste hver del og gjøre endringer uten at alt blandes sammen.

## 4\. Hvordan fungerer MVC?

MVC betyr **Model, View og Controller**. Det er en måte å dele ansvaret på i en nettside.

* **Model:** Informasjonen vi arbeider med, for eksempel en sak med tittel og beskrivelse.
* **View:** Siden brukeren ser i nettleseren, for eksempel et skjema for å opprette en sak.
* **Controller:** Tar imot det brukeren gjør og bestemmer hva som skal skje videre.

**Eksempel: Når en administrator oppretter en sak**

1. Administrator fyller ut et skjema på nettsiden.
2. `IssuesController` mottar opplysningene og kontrollerer at handlingen er tillatt.
3. `IssueService` kontrollerer opplysningene og oppretter saken.
4. `MvcDbContext` sørger for at saken lagres i PostgreSQL.
5. Nettsiden viser resultatet.

På denne måten trenger ikke nettsiden selv å inneholde alle reglene for hvordan en sak skal behandles.

## 5\. Hva lagres i databasen?

De tre viktigste typene informasjon er:

* **Project:** Et prosjekt som samler flere saker.
* **Issue:** En sak med blant annet tittel, beskrivelse, status, prosjekt og eventuell ansvarlig bruker.
* **IssueComment:** En kommentar som hører til en sak og har en forfatter.

Brukerne håndteres av ASP.NET Core Identity. En sak hører til ett prosjekt, og et prosjekt kan ha flere saker. En sak kan ha flere kommentarer.

Jeg bruker **Entity Framework Core (EF Core)** for å lese og lagre informasjon. Det betyr at jeg kan arbeide med C#-objekter i stedet for å skrive alle databasekommandoene selv. **Migrasjoner** brukes for å opprette og oppdatere tabellene i databasen.

## 6\. Innlogging og tilgang

Jeg bruker **ASP.NET Core Identity** for registrering og innlogging. Identity tar seg blant annet av passord og brukerkontoer.

Det finnes to roller:

|Rolle|Hva brukeren kan gjøre|
|-|-|
|`Admin`|Administrere prosjekter og saker, tildele saker og overstyre status.|
|`User`|Lese saker, kommentere og endre status på saker vedkommende er tildelt.|

Det er viktig at begrensningene ikke bare finnes som skjulte knapper. Koden på serveren kontrollerer også hvem som forsøker å gjøre en endring. Derfor skal en vanlig bruker ikke kunne få administratortilgang ved å skrive en adresse direkte i nettleseren.

Nye registrerte brukere får rollen `User`. Rollene `Admin` og `User` opprettes ved vanlig oppstart, mens egne administrator- og demokontoer bare opprettes i utviklingsmiljøet når de er konfigurert.

## 7\. Status på en sak

Jeg har brukt fire statuser:

1. **Open:** Saken er opprettet, men ikke tildelt.
2. **Assigned:** Saken er tildelt en bruker, men arbeidet er ikke startet.
3. **InProgress:** Den ansvarlige jobber med saken.
4. **Closed:** Saken er ferdig eller lukket.

Vanlige brukere kan gå fra `Assigned` til `InProgress`, og deretter fra `InProgress` til `Closed`, men bare på egne tildelte saker. De kan ikke hoppe rett til `Closed` eller gjenåpne en lukket sak. Administrator kan endre status fritt mellom de definerte statusene.

Jeg valgte å ha med `Assigned` som et ekstra steg i forhold til den enklere statusflyten i oppgaven. Det gjør det lettere å se forskjellen på at en sak er tildelt og at arbeidet faktisk er startet.

Hvis en sak tildeles en annen bruker, settes status til `Assigned`. Fjernes tildelingen, settes den til `Open`. Å tildele saken til samme bruker på nytt beholder statusen.

## 8\. Kontroll av opplysninger og feil

Skjemaene bruker egne modeller for informasjonen som sendes inn. Felt kan ha krav, for eksempel at en tittel må fylles ut. `ModelState` brukes for å sjekke om informasjonen er gyldig.

I tillegg kontrollerer tjenestene viktige regler. Eksempler er at en sak ikke kan opprettes i et prosjekt som ikke finnes, at en sak ikke kan tildeles en ukjent bruker, og at en bruker ikke kan endre status uten rettighet.

Skjemaer som endrer informasjon, bruker også beskyttelse mot falske POST-forespørsler (*antiforgery-token*).

## 9\. Testing

Jeg har brukt automatiske tester for å kontrollere både små deler av koden og større deler av løsningen.

Testene kontrollerer blant annet:

* At prosjekter og saker kan opprettes og behandles.
* At tildeling og statusendringer følger reglene.
* At vanlige brukere ikke får tilgang til administratorhandlinger.
* At en bruker ikke kan endre status på en sak som tilhører en annen.
* At kommentarer blir lagret med riktig bruker.
* At registrering fungerer og gir rollen `User`.
* At en POST-forespørsel uten nødvendig beskyttelse blir avvist.

HTTP-testene sender forespørsler til testutgaven av nettsiden og kontrollerer både svaret og, der det er relevant, hva som ble lagret. Testene bruker egne PostgreSQL-databaser slik at de ikke endrer de vanlige prosjektdataene.

**Siste testresultat: 98 av 98 tester bestått.**

## 10\. Oppstart og konfigurasjon

Prosjektet trenger .NET 10 og PostgreSQL. Tilkoblingen til databasen angis med `ConnectionStrings:MvcDatabase`. Databasen må være opprettet og migrert før nettsiden starter.

Kjør nettsiden fra løsningsmappen:

```powershell
dotnet run --project PersonalAgent.Web
```

For automatiske tester må miljøvariabelen `PERSONALAGENT\_TEST\_CONNECTION` være satt til en PostgreSQL-tilkobling som kan opprette og slette testdatabaser:

```powershell
dotnet test PersonalAgent.slnx
```

Passord og andre hemmeligheter bør lagres i User Secrets eller miljøvariabler, ikke i kildekoden.

## 11\. Utfordringer og valg underveis

En viktig del av arbeidet var å få **innlogging, roller og tilgang** til å fungere sammen. Det er forskjell på å skjule en knapp og å faktisk nekte en handling på serveren. Derfor er tilgang kontrollert i både kontrollere og tjenester, og testet med HTTP-forespørsler.

Et annet valg var å skille reglene for saker fra nettsidene. `IssueService` håndterer for eksempel tildeling og statusendringer. Det gjør at samme regler kan testes uten å måtte klikke gjennom nettsiden hver gang.

Under arbeidet oppstod også et problem med opprettelse av roller før testdatabasen var klar. Dette ble løst ved at testmiljøet håndterer sine egne databaser og roller, mens vanlige miljøer oppretter rollene ved oppstart.

## 12\. Begrensninger og mulig videreutvikling

Løsningen er laget for å oppfylle arbeidskravet, ikke som et ferdig system for en stor bedrift. Senere kunne den for eksempel fått bedre søk og filtrering, flere oversikter eller en mobilklient. Dette er muligheter for videre arbeid og ikke funksjoner jeg påstår er ferdige nå.

## 13\. Oppsummering

Jeg har laget en nettside hvor prosjekter, saker, brukere og kommentarer henger sammen. Arbeidet viser hvordan MVC, database, innlogging, tilgangskontroll og tester kan brukes i én løsning. Det viktigste jeg har lært, er hvordan delene samarbeider, og hvorfor det er nyttig å skille nettsidene fra reglene og datalagringen.

