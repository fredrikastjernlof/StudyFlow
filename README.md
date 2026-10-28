# 📚 StudyFlow

## 🧾 Beskrivning

StudyFlow är en personlig studieplanerare som jag har utvecklat som slutprojekt i kursen **Programmering i C#.NET (DT071G)** vid Mittuniversitetet.

Idén bakom projektet var att skapa något som jag själv skulle kunna ha användning av i mina studier. Jag ville bygga en webbapplikation där det går att samla kurser, planera uppgifter och få lite bättre koll på vad som behöver göras härnäst.

Jag ville också ta vara på möjligheten att arbeta med ett större projekt och utveckla mina kunskaper inom C# och webbutveckling. 

Resultatet blev StudyFlow – en digital studiealmanacka med anteckningsböcker, anslagstavlor och Post-it-lappar.

---

## 🎯 Syfte

Syftet med slutprojektet är att använda de kunskaper vi fått under kursens gång och omsätta dem i ett större, sammanhängande projekt.

Med StudyFlow har jag försökt tillämpa det jag lärt mig om C#, objektorienterad programmering och problemlösning, samtidigt som jag fått möjlighet att fördjupa mig i nya delar som databashantering, autentisering och webbutveckling med ASP.NET Core.

---

## ⚙️ Funktioner

I StudyFlow går det bland annat att:

- Registrera ett konto och logga in.
- Skapa, redigera och ta bort kurser och uppgifter.
- Ange deadlines, prioritet och uppskattad arbetstid.
- Skapa checklistor och bocka av delmoment.
- Följa uppgifternas status och se vad som är slutfört.
- Få en överblick över studierna på en personlig dashboard.
- Se vilka uppgifter som bör prioriteras genom en regelbaserad algoritm.

Varje användare har sina egna kurser och uppgifter.

---

## 🛠️ Tekniker

Projektet är utvecklat med följande tekniker:

- **C# och .NET 10**
- **ASP.NET Core Razor Pages**
- **ASP.NET Core Identity** för inloggning och användarhantering
- **Entity Framework Core** för databashantering
- **SQLite** som lokal databas
- **HTML, SCSS, CSS och JavaScript** för gränssnittet
- **Git och GitHub** för versionshantering

---

## 🚀 Kom igång

StudyFlow körs lokalt och kräver .NET 10 SDK samt Entity Framework Core CLI-verktyget (dotnet-ef). Om verktyget saknas kan det installeras med:

 ```bash
   dotnet tool install --global dotnet-ef --version "10.*"
   ```

### Kloning och körning

1. Klona projektet och öppna projektmappen i terminalen.
2. Återställ projektets beroenden:

   ```bash
   dotnet restore
   ```

3. Skapa databasen med projektets migreringar:

   ```bash
   dotnet ef database update
   ```

4. Starta applikationen:

   ```bash
   dotnet run
   ```

5. Öppna adressen som visas i terminalen.

### Skapa ett konto

För att använda StudyFlow behöver du registrera ett konto.

Projektet har ingen konfigurerad e-posttjänst, så efter registreringen visas en länk där du kan bekräfta kontot manuellt. När kontot är bekräftat kan du logga in och börja skapa dina egna kurser och uppgifter.

### Databasen

StudyFlow använder SQLite och lagrar informationen i en lokal databas (`app.db`).

Databasfilen följer inte med i GitHub-repot. Den skapas i stället lokalt genom att köra migreringarna enligt instruktionerna ovan.

---

## 📦 Projektstruktur

Projektet är uppdelat i flera mappar för att hålla koden organiserad:

- `Data/` – databaskontext
- `Models/` – datamodeller
- `Services/` – applikationslogik och prioriteringsalgoritm
- `ViewModels/` – modeller för formulär och datahantering
- `Pages/` – Razor Pages och sidornas funktionalitet
- `scss/` – styling uppdelad i olika komponenter
- `wwwroot/` – CSS, JavaScript och andra statiska filer

---

## 💭 Vad jag tar med mig

Det här projektet har gett mig möjlighet att knyta ihop flera av de delar vi arbetat med under kursen och använda dem i en större applikation.

Jag har framför allt fått en bättre förståelse för hur C# kan användas tillsammans med databaser, autentisering och ett webbgränssnitt. Jag har också fått arbeta mer med objektorienterad programmering, strukturering av kod och att dela upp funktionalitet i olika klasser och tjänster.

En av de större utmaningarna har varit att sätta mig in i Razor Pages och hur de olika delarna i ASP.NET Core hänger ihop. Samtidigt har det varit en av de mest lärorika delarna av arbetet.

Det jag framför allt tar med mig är en bättre förståelse för hur man bygger upp en applikation från grunden, och hur viktigt det är att tänka på både funktionalitet, kodstruktur och användarupplevelse.

StudyFlow är utvecklat för lokal körning och har inte publicerats på någon extern webbserver.