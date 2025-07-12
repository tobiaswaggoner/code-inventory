## Implementierungsplan: Phase 1 - Datenerfassung & Konsolidierung (MVP)

### **EPIC 1.1: Core-Setup & Datenbank-Modell**

**Ziel:** Eine lauffähige .NET-Anwendung mit einer leeren, aber korrekt strukturierten PostgreSQL-Datenbank zu erstellen.

-----

#### **Schritt 1: Solution und Projektstruktur anlegen**

  * **Aufgaben:**
    1.  Erstellen eines leeren Solution-Verzeichnisses.
    2.  Anlegen der Solution-Datei mittels CLI: `dotnet new sln -n CodeInventory`
    3.  Erstellen des Backend-Projekts: `dotnet new webapi -n CodeInventory.Backend`
    4.  Erstellen des Frontend-Projekts: `dotnet new blazorserver -n CodeInventory.WebApp`
    5.  Erstellen der gemeinsamen Klassenbibliothek für Datenmodelle (Contract): `dotnet new classlib -n CodeInventory.Common`
    6.  Alle Projekte zur Solution hinzufügen:
        ```bash
        dotnet sln add CodeInventory.Backend
        dotnet sln add CodeInventory.WebApp
        dotnet sln add CodeInventory.Common
        ```
    7.  Projektreferenzen hinzufügen: Backend und WebApp sollen `CodeInventory.Common` referenzieren.
        ```bash
        dotnet add CodeInventory.Backend reference CodeInventory.Common
        dotnet add CodeInventory.WebApp reference CodeInventory.Common
        ```
  * **Verifikation:** Die Solution lässt sich ohne Fehler bauen (`dotnet build`). Die Projekt-Abhängigkeiten sind korrekt in den `.csproj`-Dateien eingetragen.

-----

#### **Schritt 2: Docker & Datenbank-Setup**

  * **Aufgaben:**

    1.  Im Root-Verzeichnis der Solution eine `docker-compose.yml`-Datei erstellen. Diese definiert einen PostgreSQL-Service.
    2.  Ein `.env`-File anlegen, um die Credentials und den Port für die Datenbank zu speichern (z.B. `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`).
    3.  Die `docker-compose.yml` so konfigurieren, dass sie die Variablen aus dem `.env`-File nutzt.

    **Beispiel `docker-compose.yml`:**

    ```yaml
    version: '3.8'
    services:
      postgres:
        image: postgres:17
        restart: always
        environment:
          POSTGRES_USER: ${POSTGRES_USER}
          POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
          POSTGRES_DB: ${POSTGRES_DB}
        ports:
          - "5432:5432"
        volumes:
          - postgres_data:/var/lib/postgresql/data
    volumes:
      postgres_data:
    ```

  * **Verifikation:** Der Befehl `docker-compose up -d` startet erfolgreich einen PostgreSQL-Container. Eine Verbindung zur Datenbank ist mit einem DB-Client (z.B. DBeaver, DataGrip) unter Verwendung der Credentials aus der `.env`-Datei möglich.

-----

#### **Schritt 3: Entitäten des Datenmodells definieren**

  * **Aufgaben:**
    1.  In der `CodeInventory.Common`-Bibliothek die folgenden C\#-Klassen (POCOs) für die Entitäten erstellen:
    2.  **`Author.cs`**:
          * `Guid Id` (Primary Key)
          * `string Name`
          * `string Email` (Eindeutiger Schlüssel für die Identifikation)
    3.  **`Project.cs`**:
          * `Guid Id` (Primary Key)
          * `string Name`
          * `string FilePath` (Eindeutiger Pfad zum Repository)
    4-  **`ProjectLocation`**
          * `Guid Id` (Primary Key)
          * `Guid ProjectId` (Foreign Key)`
          * `string Location` (Name des Rechners oder "Web" für URLs)
          * `string Path` (Root Pfad auf dem Rechner oder URL zum Git Repo)
    4.  **`Commit.cs`**:
          * `string Sha` (Primary Key, da global eindeutig)
          * `string Message`
          * `DateTimeOffset AuthorTimestamp`
          * `Guid AuthorId` (Foreign Key)
          * `Guid ProjectId` (Foreign Key)
  * **Verifikation:** Die Klassen sind erstellt und können vom `CodeInventory.Backend`-Projekt referenziert werden.

-----

#### **Schritt 4: Entity Framework Core Integration**

  * **Aufgaben:**
    1.  Im `CodeInventory.Backend`-Projekt die notwendigen NuGet-Pakete hinzufügen:
        ```bash
        dotnet add package Microsoft.EntityFrameworkCore.Design
        dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
        ```
    2.  Eine `ApplicationDbContext.cs`-Klasse im Backend-Projekt erstellen, die von `DbContext` erbt.
    3.  `DbSet<Project>`, `DbSet<ProjectLocation>`, `DbSet<Commit>` und `DbSet<Author>` als Properties hinzufügen.
    4.  In `OnModelCreating` die Beziehungen und eindeutigen Schlüssel konfigurieren (z.B. `HasAlternateKey` für `Author.Email`).
    5.  Den Connection-String in `appsettings.json` im Backend-Projekt hinterlegen.
    6.  In `Program.cs` den `DbContext` für die Dependency Injection registrieren (`builder.Services.AddDbContext<ApplicationDbContext>(...)`).
  * **Verifikation:** Das Projekt lässt sich weiterhin kompilieren. Die Konfiguration für den `DbContext` ist vollständig.

-----

#### **Schritt 5: Erste Migration und Schema-Erstellung**

  * **Aufgaben:**
    1.  Im Terminal, im Verzeichnis `CodeInventory.Backend`, die erste Migration erstellen:
        `dotnet ef migrations add InitialCreate --startup-project ../CodeInventory.Backend`
    2.  Die erstellte Migration prüfen. Sie sollte die SQL-Befehle zur Erstellung der Tabellen `Projects`, `Commits`, und `Authors`, `ProjectLocations` enthalten.
    3.  Die Migration auf die laufende PostgreSQL-Datenbank anwenden:
        `dotnet ef database update --startup-project ../CodeInventory.Backend`
  * **Verifikation:** Im DB-Client sind die Tabellen `Projects`, `ProjectLocation`, `Commits`, `Authors` und die Migrationstabelle `__EFMigrationsHistory` mit den korrekten Spalten und Beziehungen sichtbar.

### **EPIC 1.2: Lokaler Verzeichnis-Crawler**

**Ziel:** Einen Hintergrunddienst zu haben, der das lokale Dateisystem nach Git-Repositories durchsucht und diese als Projekte in der Datenbank speichert.

-----

#### **Schritt 6: Konfiguration für zu scannende Verzeichnisse**

  * **Aufgaben:**
    1.  In `appsettings.json` einen neuen Abschnitt hinzufügen:
        ```json
        "CrawlSettings": {
          "RootDirectories": [
            "C:\\Users\\MyUser\\source\\repos",
            "/home/myuser/dev"
          ]
        }
        ```
    2.  In `CodeInventory.Common` eine Klasse `CrawlSettings.cs` erstellen, die diese Struktur abbildet.
    3.  In `Program.cs` des Backend-Projekts die Konfiguration als `IOptions` registrieren: `builder.Services.Configure<CrawlSettings>(builder.Configuration.GetSection("CrawlSettings"));`
  * **Verifikation:** Die Konfiguration kann per Dependency Injection in einen Service injiziert werden und enthält die korrekten Pfade.

-----

#### **Schritt 7: Implementierung des Background-Service**

  * **Aufgaben:**
    1.  Im Backend-Projekt eine neue Klasse `DirectoryCrawlerService.cs` erstellen, die von `BackgroundService` erbt.
    2.  Die Logik implementieren, um beim Programmstart zu prüfen, ob ein bestimmter Kommandozeilenparameter (z.B. `-execute-crawl`) übergeben wurde. Der Service führt seine `ExecuteAsync`-Methode nur dann aus.
    3.  Den `DirectoryCrawlerService` in `Program.cs` registrieren: `builder.Services.AddHostedService<DirectoryCrawlerService>();`
  * **Verifikation:** Startet man die Anwendung mit `dotnet run -- -execute-crawl`, wird der Code in `ExecuteAsync` ausgeführt. Startet man sie ohne den Parameter, passiert nichts (später API).

-----

#### **Schritt 8: Crawler-Logik und idempotente Speicherung**

  * **Aufgaben:**
    1.  Im `DirectoryCrawlerService` die konfigurierten `RootDirectories` auslesen.
    2.  Für jedes Root-Verzeichnis eine rekursive Suche nach Unterverzeichnissen implementieren, die einen `.git`-Ordner enthalten.
    3.  Für jedes gefundene Git-Repository (identifiziert durch seinen absoluten Pfad):
        a. Prüfen, ob in der `ProjectLocation`-Tabelle bereits ein Eintrag mit diesem `FilePath` für den aktuellen Computernamen existiert.
        b. **Nur wenn kein Eintrag existiert**, ein neues `ProjectLocation` und`Project`-Objekt erstellen  (Name = Verzeichnisname, FilePath = absoluter Pfad) und in der Datenbank speichern.
  * **Verifikation:** Nach dem Ausführen des Crawlers (`dotnet run -- -execute-crawl`) enthält die `Projects`-Tabelle einen Eintrag für jedes Git-Repository unter den konfigurierten Pfaden. Ein erneuter Lauf fügt keine Duplikate hinzu.

### **EPIC 1.3: Git-Historien-Extraktion**

**Ziel:** Für jedes gefundene Projekt die gesamte Commit-Historie auslesen, parsen und dedupliziert in der Datenbank speichern.

-----

#### **Schritt 9: Service für Git-Kommandozeilen-Interaktion**

  * **Aufgaben:**
    1.  Eine neue Service-Klasse `GitCliService.cs` erstellen.
    2.  Eine Methode `string GetGitLog(string repositoryPath)` implementieren. Diese Methode:
        a. Startet einen neuen Prozess (`System.Diagnostics.Process`).
        b. Führt `git log --all --pretty=format:"%H|||%an|||%ae|||%aI|||%s" --no-patch` im angegebenen `repositoryPath` aus. Der spezielle Separator `|||` erleichtert das Parsen.
        c. Gibt die Standardausgabe des Prozesses als String zurück.
    3.  Den Service für DI in `Program.cs` registrieren.
  * **Verifikation:** Der Aufruf von `GetGitLog` für ein Test-Repository liefert einen langen String, der alle Commits im erwarteten Format enthält.

-----

#### **Schritt 10: Parser für Git-Log und Integration in den Crawler**

  * **Aufgaben:**
    1.  Eine private Parser-Methode im `DirectoryCrawlerService` (oder einer dedizierten `GitLogParser`-Klasse) erstellen. Diese Methode nimmt den String-Output von `GetGitLog` entgegen und transformiert ihn in eine `IEnumerable<Commit>`.
    2.  Den `DirectoryCrawlerService` erweitern: Nachdem ein Projekt gefunden (oder geladen) wurde, wird `GitCliService.GetGitLog` aufgerufen.
    3.  Die zurückgegebenen Commits werden geparst.
  * **Verifikation:** Der Parser wandelt den `git log`-Output korrekt in eine Liste von `Commit`-Objekten im Speicher um.

-----

#### **Schritt 11: Deduplizierung & Speicherung der Commits**

  * **Aufgaben:**
    1.  Die Logik im `DirectoryCrawlerService` verfeinern. Für ein gegebenes Projekt:
        a. Lade alle bereits in der DB für dieses Projekt gespeicherten Commit-SHAs in ein `HashSet<string>`.
        b. Iteriere durch die geparsten Commits aus dem Git-Log.
        c. **Ignoriere** jeden Commit, dessen SHA bereits im HashSet vorhanden ist.
    2.  Für jeden **neuen** Commit:
        a. Prüfe, ob der Autor (anhand der E-Mail) bereits in der `Authors`-Tabelle existiert. Wenn ja, verwende diese ID. Wenn nein, erstelle einen neuen `Author` und speichere ihn.
        b. Füge den neuen Commit dem `DbContext` hinzu und verknüpfe ihn mit dem Projekt und dem Autor.
    3.  Rufe `_context.SaveChangesAsync()` **einmal** auf, nachdem alle neuen Commits für ein Projekt verarbeitet wurden, um die Transaktion abzuschließen.
  * **Verifikation:** Nach einem Lauf sind alle Commits eines Repos in der DB. Die Tabellen `Commits` und `Authors` sind korrekt befüllt. Ein zweiter Lauf fügt keine Commits hinzu, da diese über den SHA-Hash als Duplikate erkannt werden.

### **EPIC 1.4: Behandlung von Sonderfällen**

**Ziel:** Den Zustand "uncommittete Änderungen" für jedes Projekt erkennen und in der Datenbank vermerken.

-----

#### **Schritt 12: Erkennung von uncommitteten Änderungen**

  * **Aufgaben:**
    1.  Eine neue Eigenschaft zum `ProjectLocation`-Modell hinzufügen: `public bool HasUncommittedChanges { get; set; }`.
    2.  Eine neue EF-Migration erstellen (`dotnet ef migrations add AddUncommittedChangesFlag`) und anwenden (`dotnet ef database update`).
    3.  Den `GitCliService` um eine Methode `bool HasUncommittedChanges(string repositoryPath)` erweitern.
    4.  Diese Methode führt `git status --porcelain` aus. Wenn die Ausgabe des Befehls **nicht leer** ist, gibt die Methode `true` zurück, andernfalls `false`.
  * **Verifikation:** Die Methode `HasUncommittedChanges` gibt für ein Repository mit lokalen Änderungen `true` und für ein "sauberes" Repository `false` zurück. Das Datenbankschema ist aktualisiert.

-----

#### **Schritt 13: Status in der Datenbank aktualisieren**

  * **Aufgaben:**
    1.  Den `DirectoryCrawlerService` anpassen: Bei jeder Verarbeitung eines Projekts (unabhängig davon, ob es neu ist oder bereits existiert), rufe `GitCliService.HasUncommittedChanges()` auf.
    2.  Setze den Wert von `Project.HasUncommittedChanges` entsprechend dem Ergebnis.
    3.  Speichere die Änderungen am Projekt-Objekt in der Datenbank.
  * **Verifikation:** Führe den Crawler aus. Für Projekte mit uncommitteten Änderungen ist der Wert in der `Projects`-Tabelle auf `true` gesetzt. Macht man in einem anderen Repo eine Änderung und führt den Crawler erneut aus, wird dessen Flag ebenfalls auf `true` aktualisiert. Committet man die Änderungen, wird das Flag beim nächsten Lauf auf `false` gesetzt.