# Dokumentasjon – Personal Agent Core API

NB! Dette dokumentet er omskrevet med hjelp av LLM verktøy for bedre struktur og rettskrivning, feil kan forekomme.

Webapp 2, arbeidskrav 1. Utviklet med .NET 10, C# og PostgreSQL.

## Hva systemet gjør

API-et lar brukere lagre interesser, preferanser og overvåkingsregler. Behovet er å samle personlige ønsker og kriterier som ellers kan ligge spredt i notater og samtaler. For eksempel kan en bruker registrere Magic the Gathering som interesse, Commander som foretrukket format og en regel med søkeord og makspris for et produkt.

Dette skal senere kunne brukes av en personlig agent. I arbeidskravet er det bare lagring og administrasjon som er implementert. API-et bruker ikke en språkmodell og søker ikke i butikker.

## Hvordan koden er delt opp

Løsningen har PersonalAgent.Api og PersonalAgent.Tests. I API-prosjektet ligger controllers, modeller, DTO-er, services, Data og migrations.

Controllers tar imot HTTP-kall og returnerer svar. Services gjør databaseoppslag og kontrollerer reglene. Modellene beskriver det som lagres, mens DTO-ene beskriver hva klienten får sende og motta. AgentDbContext kobler modellene til databasen. Program.cs setter opp tjenestene, Identity, Swagger og oppstarten.

En oppretting av en overvåkingsregel går gjennom WatchRulesController til WatchRuleService. Controlleren henter bruker-ID fra innloggingen. Tjenesten kontrollerer pris, valuta og at interessen tilhører brukeren. Deretter lagres regelen gjennom AgentDbContext. Svaret er en WatchRuleDto med status 201 og en Location-header.

## Modeller og relasjoner

Datamodellen har tre egne entiteter i tillegg til ApplicationUser fra Identity:

- Interest har navn, kategori, bruker-ID og tidsstempler.
- Preference har en nøkkel og verdi, bruker-ID og valgfri interesse-ID.
- WatchRule har interesse-ID, bruker-ID, tittel, søkeord, valgfri makspris, valuta og Enabled.

En bruker har flere interesser, preferanser og regler. En interesse kan ha flere preferanser og regler. Preferanser uten interesse-ID gjelder generelt, mens en regel alltid må ha en interesse.

Pris er decimal og lagres med presisjon 18,2. Valutaene som støttes er NOK, SEK, EUR og USD. Det utføres ikke valutaomregning.

## EF Core og database

AgentDbContext arver fra IdentityDbContext<ApplicationUser>. Den har DbSet for de tre domenetabellene. OnModelCreating konfigurerer feltlengder, obligatoriske felt, fremmednøkler og sletteoppførsel. UseNpgsql velger PostgreSQL-provider.

InitialCreate-migrasjonen oppretter tabellene med dotnet ef database update. Databasen har tre domenetabeller, sju Identity-tabeller og __EFMigrationsHistory. Migrations følger med kildekoden slik at databasen kan opprettes på nytt.

Lesespørringer bruker AsNoTracking når data bare skal vises. Ved endring hentes et sporet objekt og lagres med SaveChangesAsync. DbContext setter CreatedAt og UpdatedAt i UTC. Opprettingstiden beholdes ved oppdatering.

En interesse med tilknyttede preferanser eller regler kan ikke slettes. InterestService kontrollerer dette og controlleren returnerer 409. Relasjonene har også Restrict i databasen. Tjenesten håndterer fremmednøkkelkonflikt hvis en avhengighet blir opprettet mellom kontroll og sletting.

## Seed-data

DbSeeder oppretter to demobrukere med ulike interesser, preferanser og regler ved oppstart i Development. Kontoene opprettes gjennom UserManager, og konto med tilhørende data lagres i en transaksjon.

Hvis en demokonto finnes fra før, hoppes den over. Dermed kommer det ikke duplikater ved ny oppstart. Seedingen gjenoppretter ikke data som brukeren selv har slettet.

## Innlogging og tilgang

ASP.NET Core Identity håndterer registrering, passord og innlogging. AddIdentityApiEndpoints registrerer tjenestene, og MapIdentityApi legger endepunktene under /api/auth. E-post må være unik. Passordet krever minst ti tegn, stor og liten bokstav, tall og spesialtegn.

Innlogging med useCookies=false gir Identitys innebygde bearer-token. Det er ikke JWT. Swagger sender tokenet i Authorization-headeren. UseAuthentication kjøres før UseAuthorization.

De tre domenecontrollerne og AccountController krever Identity.Bearer. Statusendepunktet er åpent. Autorisering handler også om eierskap: tjenestene filtrerer oppslag og endringer med bruker-ID fra innloggingen. Klienten kan ikke velge en annen UserId i DTO-en.

En annen brukers ressurs gir 404. En regel eller preferanse kan ikke kobles til en annens interesse. Det er ikke laget administratorroller fordi første versjon bare håndterer brukerens private registreringer. Eierskapskontroll er den relevante autoriseringen her.

## API og validering

Interesser, preferanser og regler har GET, POST, PUT og DELETE. Preferanser kan filtreres på interestId og regler på interestId og enabled.

Ved oppretting returneres 201, ved henting og oppdatering 200, og ved sletting 204. Ugyldige data gir 400. Manglende innlogging gir 401, en ressurs utenfor brukerens tilgang gir 404, og en interesse med avhengigheter gir 409.

DTO-attributter kontrollerer obligatoriske felt og lengde. Koden har ekstra kontroller for tekst med bare mellomrom, interessekoblinger, pris og valuta. Makspris må være positiv når den er satt, passe i databasefeltet og ha maksimalt to desimaler. Valideringsfeil og domenefeil bruker ValidationProblemDetails eller ProblemDetails der det passer.

## Hva som er særlig viktig

Brukeravgrensede oppslag og kontroll av interessekoblinger er viktige for at data ikke skal blandes mellom kontoer. En fremmednøkkel alene sjekker ikke at interessen tilhører samme bruker.

DTO-ene hindrer at hele databaseobjekter eksponeres og at klienten setter bruker-ID eller tidsstempler. Slettesperren beskytter relasjonene. Migrations gjør databaseoppsettet gjenskapbart.

## Tester

PersonalAgent.Tests er et eget xUnit-prosjekt. AgentCoreTests kjører tjenestene mot ekte PostgreSQL. DatabaseScope oppretter en unik personalagent_test_<guid>-database per testtilfelle, bruker migrations og sletter databasen etterpå. De vanlige demodataene endres ikke.

Testene kontrollerer:

- CRUD og lagring for interesser, preferanser og regler.
- At en annen bruker ikke kan lese, endre eller slette registreringene.
- At en preferanse eller regel ikke kan knyttes til en annens interesse.
- At interesse med avhengigheter ikke kan slettes.
- Prisverdiene -10, 0 og 12.345 avvises uten lagring.
- At enabled-filteret fungerer etter deaktivering.

Verifisert kjøring 9. oktober 2026 ga 8 testtilfeller og 8 bestått. Seks testmetoder gir åtte tilfeller fordi prisvalideringen bruker tre Theory-verdier.

PostgreSQL lagrer tidsstempler med mikrosekundpresisjon, mens .NET har finere oppløsning. Testen for bevart opprettingstid sammenligner derfor med verdien lest fra databasen.

Dette er service-/databaseintegrasjonstester, ikke automatiserte HTTP-tester. Registrering, innlogging, statuskoder, seed-data og tilgang mellom demobrukerne er også kontrollert manuelt i Swagger.

## Begrensninger og videre arbeid

Løsningen er et første fundament. Overvåking, AI, app, varsler og dokumentarkiv kommer senere. Automatiserte HTTP-tester, mer komplett Swagger-responsdokumentasjon, paginering og generell feilhåndtering kan forbedres. Databasebrukeren har vide rettigheter i utviklingsoppsettet; ved senere drift bør test- og applikasjonsrettigheter skilles.
