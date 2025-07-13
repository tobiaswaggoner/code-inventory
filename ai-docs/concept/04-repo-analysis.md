# Repository Analyse

## Übersicht

### Schritt 1: Gesamte Code Basis konsolidieren

In dieser Phase wollen wir feststellen um was es in diesem Repo geht.

Dazu erzeugen wir zuerst mit Hilfe von repomix eine zentrale Code Datei

Doku zu Repomix hier: https://github.com/yamadashy/repomix?tab=readme-ov-file

Wichtig: Repomix berücksichtigt nur .gitignore auf der Root Ebene des Repositories. 

Wir müssen daher eine .repomixignore Datei erstellen, die alle .gitgnore Dateien kombiniert.

Zusätzlich wollen wir "**/*.css,**/*.css.map" ignorieren.

Wir rufen das Tool mit --compress auf.

### Schritt 2: Zusammenfassung erstellen.

Danach wollen wir mit Gemini (https://ai.google.dev/gemini-api/docs?hl=de#rest) eine Zusammenfassung des Repos erstellen:

Dazu laden wir das gesamte File in Gemini hoch mit diesem System Prompt:

```text
Based on the codebase in this file, please generate a detailed response in Markdown format.

This file should be structured as:

# <PROJECT TITLE>

## Executive Summary
<A brief summary of the following sections>

## Purpose
<What ist the project intended to do? What ist the project about to solve?>

## Tech Stack
<What technologies where used? Programming Language. Frameworks, Versions, Infrastructure like Docker, Databases, Cloud Resources>

## State
<Given the purpose: How far ist the implementation? Just Begun? Partially implemented? Feature complete? Give a reason for your opinion>

## Quality
<Be opinonated: Does this look like production code or like a throw away prototype? How current are the used frameworks and code constructs? What needs to be improved?

Create only the markdown - nothing else.
```

Wir speichern das Ergebnis dann in README-GENERATED.MD

### Schritt 3: Snappy one-liner

Wir nehmen jetzt dieses Markdown und lassen Gemini einen One-Line erstellen:

```text
This is the description of a development project I have been working on.

Please generate a snappy one liner that captures the intent of this project.

Return nothing else. Only the one-liner.

```

Wir speichern das als One-Liner.txt im Root des Repos

### Schritt 4: Image Prompt

Wir nehmen wieder das Markdown und lassen uns jetzt noch ein Hero Image in zwei Schritten erstellen:

```text
Please complete the prompt which created an image for this project by replacing the <description> in the follwowing

---
A minimalist flat design illustration, intended as a professional hero image for a tech blog article.
The overall style is clean and symbolic, using simple geometric shapes and avoiding distracting details, reminiscent of a high-end modern infographic.

<description>

The color palette is corporate and modern, using deep blues (#0B2447) and grays, with a single bright accent color like amber or orange (#FFC700) used sparingly..
The image must be in a wide 16:9 aspect ratio, suitable for a web banner.
---

Create only the prompt and nothing else
```

Wir speichern diesen Prompt dann in Hero-Image-Prompt.txt


### Schritt 5: Image Erzeugung

Jetzt erzeugen wir das Bild: API Doku ist hier: https://ai.google.dev/gemini-api/docs/image-generation?hl=de#rest

Wir verwenden den gerade erzeugten Prompt und speichern das Bild in Hero-Image.png

### Schritt 6: Speicherung in der Dattenbank

Wir erweitern unser "Project" Modell um die Felder: Headline, Description und Image (Byte Array).
Wir speichern die gerade erzeugten Artefakte in der Datenbank (Bild, One-Liner, Description)
