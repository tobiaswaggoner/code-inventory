### **Projekttitel (Arbeitstitel): Code Inventory**

Dieses Dokument dient als strukturiertes Brainstorming und bildet die Grundlage für die Konzeption einer Anwendung zur Inventarisierung und Analyse von Softwareprojekten.

#### **1. Kernidee & Vision**

Entwicklung einer zentralen Anwendung, die eine vollständige, zeitlich geordnete Übersicht über alle Softwareprojekte einer Person oder eines Teams erstellt. Die Anwendung soll Daten aus verschiedenen Quellen (lokale Verzeichnisse, Git-Remotes) aggregieren, um eine einheitliche Timeline aller Entwicklungsaktivitäten zu schaffen. Langfristig soll die Anwendung nicht nur zeigen, *wann* an etwas gearbeitet wurde, sondern auch *was* der Inhalt der Projekte ist und *wie viel* Aufwand investiert wurde.

#### **2. Problemstellung**

Über Jahre und Jahrzehnte der Entwicklung entsteht eine unübersichtliche Menge an Projekten, Prototypen und Code-Schnipseln. Diese sind oft an verschiedenen Orten verstreut:
* Auf mehreren Festplatten und Rechnern (z.B. Desktop, Notebook).
* In verschiedenen Remote-Repositories (GitHub, GitLab).
* In unterschiedlichen Zuständen (aktive Entwicklung, archiviert, abgebrochen).

Dies führt zu einem Verlust des Überblicks:
* An welchen Projekten habe ich wann gearbeitet?
* Welche Projekte haben ungespeicherte, lokale Änderungen?
* Wo befindet sich der aktuellste Stand eines Projekts?
* Welche Technologien und Frameworks wurden in alten Projekten verwendet?

#### **3. Ziele & Phasen (Roadmap)**

Das Projekt lässt sich in mehrere aufeinander aufbauende Phasen unterteilen:

**Phase 1: Datenerfassung & Konsolidierung (MVP)**
* **Ziel:** Alle Git-Historien und Projekt-Metadaten in einer zentralen Datenbank zusammenführen.
* **Funktionen:**
    * Crawlen von lokalen Dateisystemen zur Identifizierung von Git-Repositories.
    * Abrufen der vollständigen Commit-Historie aus allen Branches lokaler Repos.
    * Klonen/Fetchen von Git-Historien über Remote-URLs (z.B. GitHub).
    * **Deduplizierung:** Commits werden global über ihren SHA-Hash eindeutig identifiziert und gespeichert, um Redundanzen über verschiedene Quellen hinweg zu vermeiden.
    * Erfassen von Projekten **ohne** Git-Repository als einfache Einträge.
    * Identifizieren von lokalen, uncommitteten Änderungen.

**Phase 2: Visualisierung & Auswertung**
* **Ziel:** Die gesammelten Daten in einer verständlichen Form darzustellen.
* **Funktionen:**
    * Eine Web-Oberfläche (Frontend) zur Darstellung der Projekte.
    * Eine globale Timeline, die anzeigt, an welchem Projekt zu welcher Zeit gearbeitet wurde (basierend auf Commit-Daten).
    * Eine Projektübersicht mit Statusanzeige (z.B. letzte Aktivität, uncommittete Änderungen).

**Phase 3: Inhaltliche Code-Analyse (KI-gestützt)**
* **Ziel:** Ein tieferes Verständnis für den Inhalt und Status der Projekte zu erlangen.
* **Funktionen:**
    * Automatische Analyse der Codebasis zur Identifizierung von:
        * Verwendeten Programmiersprachen und Frameworks.
        * Architektur-Stacks (z.B. Frontend, Backend, Datenbank).
        * Abhängigkeiten und Tooling.
    * Generierung einer Zusammenfassung oder eines "Steckbriefs" pro Projekt.
    * Bewertung des Projektfortschritts oder der Komplexität.

**Phase 4: Erweiterte Aufwandsanalyse**
* **Ziel:** Den tatsächlichen Zeitaufwand pro Projekt zu quantifizieren.
* **Funktionen:**
    * Integration von externen Tracking-Tools (explizit genannt: **ManicTime**).
    * Verknüpfung von Zeitdaten (z.B. "Datei X in VS Code geöffnet") mit den entsprechenden Projekten und Commits.
    * Detaillierte Auswertung des investierten Zeitaufwands.

#### **4. Kernanforderungen & technische Konzepte**

**Datenerfassung:**
* **Input-Quellen:** Das System muss eine konfigurierbare Liste von lokalen Root-Verzeichnissen und Git-Remote-URLs als Input akzeptieren.
* **Crawler:** Ein robuster Crawler, der rekursiv Verzeichnisse durchsucht und `.git`-Ordner identifiziert.
* **Git-Interaktion:** Muss die komplette Commit-Historie über alle Branches extrahieren (Autor, Datum, Commit-Nachricht, SHA). Der eigentliche Code-Delta ist zunächst sekundär. Dies kann über Kommandozeilen-Aufrufe von `git` oder eine Bibliothek wie `LibGit2Sharp` erfolgen.
* **Umgang mit uncommitteten Änderungen:**
    * Lokale Änderungen sollen als eine Art **"Pseudo-Commit"** pro Projekt erfasst werden.
    * Der Zeitstempel dieses Pseudo-Commits ist das Änderungsdatum der zuletzt modifizierten Datei im Repository.
    * Dies stellt sicher, dass auch aktuellste, noch nicht versionierte Arbeit auf der Timeline sichtbar ist.

**Datenmodell & Speicherung:**
* **Datenbank:** PostgreSQL wird als zentraler Speicher für alle Metadaten, Commits und Projektinformationen verwendet.
* **Zentrale Commit-Tabelle:** Eine einzige Tabelle für alle Commits aller Projekte. Der Commit-SHA ist der Primärschlüssel zur Deduplizierung.
* **Projekt-Tabelle:** Eine Tabelle, die jedes einzigartige Projekt (definiert durch seinen Ursprungspfad oder seine Remote-URL) speichert und mit den Commits verknüpft.

**Prozess & Ausführung:**
* **Backend-Service:** Eine zentrale Logik, die als Backend-Anwendung (z.B. als geplanter Task oder Background Worker) läuft.
* **Idempotenz:** Ein erneuter Lauf des Analyse-Prozesses darf keine Duplikaten erzeugen. Das System soll nur neue Commits und geänderte Status (wie neue uncommittete Änderungen) erkennen und die Datenbank entsprechend aktualisieren.
* **Plattformunabhängigkeit:** Der Collector/Crawler muss sowohl auf **Windows** als auch auf **Linux** lauffähig sein.
* **Trigger:** Die Analyse soll manuell (per CLI oder UI-Button) oder zeitgesteuert (z.B. täglich) ausgelöst werden können.

#### **5. Architekturvorstellung**

* **Backend:** **.NET 9 Web API** mit Background-Workern für die rechenintensiven Analyse- und Crawling-Aufgaben.
* **Frontend:** **.NET 9 Blazor** für eine interaktive Weboberfläche zur Visualisierung der Daten.
* **Datenbank:** **PostgreSQL** als robustes und erweiterbares Datenbanksystem.

---