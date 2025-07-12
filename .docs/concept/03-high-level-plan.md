# **Implementierungsplan: Code Inventory**

Dieses Dokument beschreibt den Implementierungsplan für das Projekt "Code Inventory" auf der Ebene von EPICS. Der Plan ist in vier Phasen gegliedert, die jeweils einer Iteration entsprechen und die Grundlage für die Erstellung von sequentiellen Tasks bilden.

## **1\. Eckdaten & Nicht-funktionale Anforderungen**

Bevor wir in die Phasenplanung einsteigen, definieren wir die technologischen und qualitativen Rahmenbedingungen.

### **Technologie-Stack**

* **Backend:** .NET 9 Web API mit Background-Workern (für Crawling & Analyse)  
* **Frontend:** .NET 9 Blazor Server (für eine einfache, reaktive UI)  
* **Datenbank:** PostgreSQL (lokal betrieben, z.B. via Docker)  
* **Datenzugriff:** Entity Framework Core 9  
* **Git-Interaktion:** Direkte Aufrufe der git Kommandozeile (System.Diagnostics.Process), um Abhängigkeiten zu minimieren und maximale Kompatibilität zu gewährleisten.

### **Architektur-Prinzipien**

* **Lokale Anwendung:** Das gesamte System (Backend, Frontend, DB) läuft auf dem lokalen Rechner des Anwenders. Es gibt keine Cloud-Komponenten.  
* **Mono Repository Ansatz:** Backend und Frontend sind eng gekoppelt in einer einzigen .NET-Solution. Der Contract (Model, API) wird über eine gemeinsame Abhängigkeit (Common class library) geteilt.
* **Idempotente Prozesse:** Der Datenscan- und Importprozess muss so gestaltet sein, dass er beliebig oft ausgeführt werden kann, ohne Duplikaten zu erzeugen oder bestehende Daten fälschlicherweise zu überschreiben.

### **Nicht-funktionale Anforderungen**

* **Skalierbarkeit:** Nicht relevant. Das System wird für das Datenvolumen eines einzelnen Entwicklers ausgelegt. Performance-Optimierungen sind sekundär.  
* **Test-Strategie:** Pragmatischer Ansatz.  
  * **Unit Tests (nUnit):** Für kritische Logik wie das Parsen von Git-Logs und die Daten-Deduplizierung. Eine Testabdeckung von \>70% für diese Kernkomponenten wird angestrebt.
  * **Mocks (nSubstitute)_** Die Implementation muss flächendeckendes Testen ermöglichen. Dependencies werden per DI injected. Dedizierte Interfaces sind zu verwenden. Statische Klassenfunktionen (insbesondere DateTime.Now und Guid.NewGuid) müssen im Produktionscode gewrapped werden.  
  * **Integration Tests:** UI-Tests und Integrations-Tests sind nicht vorgesehen.  
* **Plattformunabhängigkeit:** Der Backend-Service muss unter Windows und Linux lauffähig sein. Die Verwendung von .NET und Standard-System-APIs stellt dies sicher. Wichtig ist die Beachtung von Dateisystem Unterschieden.
* **Konfigurierbarkeit:** Die zu scannenden Root-Verzeichnisse und Remote-Git-URLs müssen in einer Konfigurationsdatei (appsettings.json) einfach zu verwalten sein. Diese wird später über eine UI pflegbar.
* **Datensicherheit:** Nicht relevant, da die Anwendung und die Daten den lokalen Rechner nicht verlassen.

### **Implementationsvorgaben**
* **Infrastruktur:** Postgres läuft innerhalb von Docker auf dem Standart Port. Der Pfad muss konfigurierbar sein. Wir benötigen eine Test und eine Prod Datenbank. Wir verwenden .env Dateien um Laufzeitparameter und Credentials zu injecten.
* **Projekt Setup**: Wo möglich sollen CLI Kommandos zum Erstellen der Solution, der Projekte, dem Hinzufügen von Abhängigkeiten und der Erstellung von Migrations in Entity Framework verwendet werden. Die direkte Manipultation von .csproj oder .sln Files ist zu vermeiden. 


## **2\. Implementierungsplan nach Phasen**

### **Phase 1: Datenerfassung & Konsolidierung (MVP)**

**Ziel:** Alle Git-Historien und Projekt-Metadaten in einer zentralen PostgreSQL-Datenbank zusammenführen.

| EPIC | Beschreibung | Umsetzungsschritte | Akzeptanzkriterien (Beispiele) |
| :---- | :---- | :---- | :---- |
| **EPIC 1.1: Core-Setup & Datenbank-Modell** | Initiales Aufsetzen des Projekts, der Datenbank und des Datenmodells. | 1\. .NET-Solution mit Web API & Blazor-Projekt erstellen.\<br\>2. PostgreSQL-Datenbank (lokal via Docker) aufsetzen.\<br\>3. EF Core einrichten und die initialen Modelle für Projekt, Commit und Autor erstellen.\<br\>4. Eine erste Migration erstellen und auf die DB anwenden. | \- Die Anwendung kann starten und sich erfolgreich mit der DB verbinden.\<br\>- Das Datenbankschema für Projekte und Commits ist erstellt.\<br\>- Ein Projekt kann manuell in der DB angelegt werden. |
| **EPIC 1.2: Lokaler Verzeichnis-Crawler** | Implementierung eines Background-Services, der das Dateisystem nach Git-Repositories durchsucht. | 1\. Einen BackgroundService in der Web API erstellen.\<br\>2. Logik implementieren, die eine Liste von Root-Verzeichnissen aus der appsettings.json liest.\<br\>3. Rekursive Suche nach .git-Verzeichnissen implementieren.\<br\>4. Gefundene Repositories werden als Projekt-Einträge in der DB gespeichert (falls noch nicht vorhanden). | \- Der Service startet beim Anwendungsstart wenn ein "-execute-crawl" Parameter übergeben wurde.\<br\>- Alle Git-Repositories unterhalb der konfigurierten Pfade werden gefunden.\<br\>- Für jedes gefundene Repo wird ein eindeutiger Eintrag in der Projekt-Tabelle erstellt. |
| **EPIC 1.3: Git-Historien-Extraktion** | Implementierung der Logik zum Auslesen der Commit-Historie aus den lokalen Repos. | 1\. Eine Service-Klasse erstellen, die git log per Kommandozeile ausführt.\<br\>2. Das Ausgabeformat von git log definieren (z.B. mit \--pretty=format:"..."), um SHA, Autor, Datum und Nachricht einfach parsen zu können.\<br\>3. Einen Parser für die git log-Ausgabe schreiben.\<br\>4. Die extrahierten Commits werden unter Beachtung des SHA-Hashes (Deduplizierung) in der Commit-Tabelle gespeichert. | \- Die gesamte Commit-Historie eines Repos über alle Branches wird ausgelesen.\<br\>- Commits werden korrekt geparst und in die DB geschrieben.\<br\>- Ein erneuter Lauf für dasselbe Repo fügt keine doppelten Commits hinzu. |
| **EPIC 1.4: Behandlung von Sonderfällen** | Erfassen von Projekten ohne Git-Repo und Erkennen von uncommitteten Änderungen. | 1. Für Git-Repos die git status \--porcelain-Ausgabe prüfen.\<br\>2. Bei uncommitteten Änderungen einen "Pseudo-Commit" im Speicher erzeugen, dessen Zeitstempel dem letzten Änderungsdatum einer betroffenen Datei entspricht. (Dieser wird nicht in der DB gespeichert, sondern zur Laufzeit für die UI generiert). | \- Der Status "hat uncommittete Änderungen" wird für ein Projekt korrekt erkannt.\<br\>- Das Datum der letzten uncommitteten Änderung ist bekannt. |

### **Phase 2: Visualisierung & Auswertung**

**Ziel:** Die gesammelten Daten in einer verständlichen Web-Oberfläche darzustellen.

| EPIC | Beschreibung | Umsetzungsschritte | Akzeptanzkriterien (Beispiele) |
| :---- | :---- | :---- | :---- |
| **EPIC 2.1: Basis-UI & Projektübersicht** | Erstellen einer Blazor-Oberfläche, die alle erfassten Projekte auflistet. | 1\. Ein einfaches Layout für die Blazor-App erstellen (z.B. mit Seitenleiste und Hauptbereich).\<br\>2. Eine neue Seite "Projekte" erstellen.\<br\>3. Die Seite liest alle Projekte aus der Datenbank aus und stellt sie in einer Tabelle oder Liste dar.\<br\>4. Pro Projekt werden Basis-Infos angezeigt: Name, Pfad, letztes Commit-Datum, Status (uncommittete Änderungen ja/nein). | \- Die Web-UI ist aufrufbar.\<br\>- Alle Projekte aus der DB werden in einer Liste angezeigt.\<br\>- Die Liste zeigt den korrekten Status für uncommittete Änderungen an. |
| **EPIC 2.2: Globale Timeline-Visualisierung** | Darstellung aller Commits über die Zeit in einer interaktiven Timeline. | 1\. Eine neue Seite "Timeline" erstellen.\<br\>2. Eine einfache, horizontale Timeline-Komponente mit HTML/CSS/SVG entwickeln.\<br\>3. Die Komponente lädt alle Commits aus der DB und platziert sie chronologisch auf der Zeitachse.\<br\>4. Commits werden farblich nach Projekt gruppiert. Ein Mouse-Over zeigt Commit-Details an. | \- Die Timeline zeigt alle Commits an der korrekten chronologischen Position.\<br\>- Man kann auf der Timeline zoomen und pannen (z.B. Jahres-/Monatsansicht).\<br\>- Die Zuordnung von Commits zu Projekten ist visuell erkennbar. |
| **EPIC 2.3: Remote-Repository-Synchronisation** | Hinzufügen der Funktionalität, auch Repos von Git-URLs zu klonen/fetchen. | 1\. Die Konfiguration (appsettings.json) um eine Liste von Git-Remote-URLs erweitern.\<br\>2. Den Crawler-Service erweitern: Für jede URL wird geprüft, ob das Repo lokal schon existiert. Wenn nicht, wird es via git clone in ein zentrales Cache-Verzeichnis geklont. Wenn ja, wird git fetch ausgeführt.\<br\>3. Die Analyse-Logik wird auf diese lokalen Klone angewendet. | \- Repos aus der URL-Liste werden erfolgreich geklont/gefetcht.\<br\>- Die Commit-Historie von Remote-Repos erscheint in der Datenbank und auf der Timeline. |

### **Phase 3: Inhaltliche Code-Analyse**

**Ziel:** Ein tieferes Verständnis für den Inhalt der Projekte durch einfache, heuristische Analyse zu erlangen.

| EPIC | Beschreibung | Umsetzungsschritte | Akzeptanzkriterien (Beispiele) |
| :---- | :---- | :---- | :---- |
| **EPIC 3.1: Technologie-Stack-Erkennung** | Automatische Identifizierung von verwendeten Sprachen und Frameworks. | 1\. Den Analyse-Service erweitern, um die Dateistruktur eines Projekts zu lesen (im Stand des letzten Commits).\<br\>2. Eine regelbasierte Logik implementieren, die auf die Existenz spezifischer Dateien prüft (z.B. \*.csproj \-\> .NET, package.json \-\> Node.js, pom.xml \-\> Maven).\<br\>3. Die erkannten Technologien pro Projekt in der Datenbank speichern. | \- Für ein .NET-Projekt wird ".NET" als Technologie erkannt.\<br\>- Für ein React-Projekt wird "Node.js" und "React" (durch Analyse der package.json) erkannt.\<br\>- Die erkannten Technologien werden in der Projektübersicht angezeigt. |
| **EPIC 3.2: Projekt-Steckbrief** | Generierung und Anzeige einer zusammenfassenden Übersicht pro Projekt. | 1\. Eine Detailansicht für ein einzelnes Projekt in der UI erstellen.\<br\>2. Auf dieser Seite werden alle relevanten Daten aggregiert: Projektname, Pfad, alle Commits, erkannte Technologien.\<br\>3. Optional: Eine einfache Zusammenfassung mit einem LLM (via API-Call) auf Basis der Commit-Nachrichten generieren lassen. | \- Klickt man in der Übersicht auf ein Projekt, öffnet sich die Detailseite.\<br\>- Alle erfassten Informationen zum Projekt sind auf einen Blick sichtbar. |

### **Phase 4: Erweiterte Aufwandsanalyse**

**Ziel:** Den tatsächlichen Zeitaufwand pro Projekt durch die Integration von ManicTime-Daten zu quantifizieren.

| EPIC | Beschreibung | Umsetzungsschritte | Akzeptanzkriterien (Beispiele) |
| :---- | :---- | :---- | :---- |
| **EPIC 4.1: ManicTime-Daten-Interface** | Implementierung eines Lesers für die ManicTime-Datenbank. | 1\. Recherchieren, in welchem Format ManicTime seine Daten speichert (vermutlich eine lokale SQLite-Datenbank).\<br\>2. Einen Reader implementieren, der die relevanten Tabellen (z.B. Anwendungs- und Dokumentennutzung) aus der ManicTime-DB ausliest.\<br\>3. Den Pfad zur ManicTime-DB konfigurierbar machen. | \- Die Anwendung kann auf die ManicTime-Daten zugreifen und die Nutzungsdaten für einen bestimmten Zeitraum auslesen. |
| **EPIC 4.2: Daten-Korrelation & \-Visualisierung** | Verknüpfung der Zeitdaten mit den Projekten und Darstellung in der UI. | 1\. Eine Logik entwickeln, die die Dateipfade aus den ManicTime-Logs den entsprechenden Projektverzeichnissen aus der Code-Inventory-DB zuordnet.\<br\>2. Die aggregierte Zeit pro Projekt berechnen.\<br\>3. Die Gesamtzeit in der Projektübersicht und der Projektdetailansicht anzeigen. | \- Die in einem Projekt verbrachte Zeit (laut ManicTime) wird korrekt berechnet.\<br\>- Die UI zeigt an: "Projekt X: 15 Stunden aktive Zeit". |

