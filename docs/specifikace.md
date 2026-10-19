# Specifikace bakalářské práce

## Název práce

Návrh a implementace grafové analytické aplikace pro vizualizaci, predikci a ukládání kriminalistických událostí

---

## 1. Úvod a motivace

Kriminalistická analýza pracuje s rozsáhlými a silně propojenými daty – událostmi, osobami, lokalitami, důkazy a jejich vzájemnými vztahy. Pro analytika často nejsou podstatné jednotlivé záznamy, ale vazby mezi nimi: tatáž osoba ve více případech, několik událostí na stejném místě nebo nepřímé spojení dvou osob přes společné známé.

Grafový přístup umožňuje takové vazby přirozeně reprezentovat a dotazovat se na ně. Tato práce se zaměřuje na návrh a implementaci prototypu systému, který nad kriminalistickými daty používá grafový datový model. Model je uložen v relační databázi PostgreSQL a grafové dotazy (okolí uzlu, cesty mezi entitami) jsou realizovány rekurzivními dotazy SQL. Součástí práce je zdůvodnění této volby oproti specializované grafové databázi.

---

## 2. Cíl práce

Hlavní cíle práce:

- Navrhnout grafový datový model kriminalistických událostí
- Implementovat webovou aplikaci pro správu a vizualizaci těchto dat, určenou pro více uživatelů s rozdílnými oprávněními
- Realizovat analytický modul nad grafovou strukturou umožňující výpočet grafových metrik (např. degree centrality), vyhledávání cest mezi entitami, analýzu propojenosti a agregované statistické dotazy nad kriminalistickými daty
- Implementovat a experimentálně vyhodnotit metody predikce vazeb (link prediction)
- Zajistit ochranu citlivých dat a dohledatelnost změn
- Diskutovat etické aspekty prediktivní kriminalistiky

Výstupem bude funkční prototyp systému a experimentální vyhodnocení navržených metod.

---

## 3. Funkční požadavky

### 3.1 Správa dat

Systém musí umožňovat:

- Vytváření, editaci a mazání kriminalistických událostí
- Evidenci osob, lokalit a dalších entit
- Vytváření vztahů mezi entitami
- Ukládání atributů (čas, typ události, závažnost, role osoby apod.)

---

### 3.2 Mapová vizualizace

Systém musí umožňovat:

- Zobrazení událostí na mapě
- Filtrování podle času a typu
- Agregované zobrazení (např. heatmap)
- Základní statistiky podle regionu

---

### 3.3 Grafová vizualizace

Po výběru konkrétní události nebo osoby bude možné:

- Zobrazit lokální graf jejích vztahů
- Vizualizovat osoby, související události, lokality a další entity
- Interaktivně rozšiřovat graf
- Zobrazit predikované vazby odlišeně od vazeb zaznamenaných, včetně jejich skóre a zdůvodnění

---

### 3.4 Statistický modul

Systém bude poskytovat:

- Počet událostí podle typu
- Rozložení událostí v čase
- Identifikaci nejvíce propojených uzlů (degree centrality)
- Základní agregace nad grafem

---

### 3.5 Uživatelé a oprávnění

Systém musí umožňovat:

- Přihlášení uživatelů
- Přidělení rolí s rozdílnými oprávněními (administrátor, analytik, čtenář)
- Správu uživatelských účtů administrátorem
- Souběžnou práci více uživatelů bez nechtěného přepsání cizích změn

---

## 4. Datový model

Grafový datový model bude uložen v relační databázi PostgreSQL. Uzly i vztahy budou reprezentovány tabulkami, grafové dotazy rekurzivními dotazy SQL.

### 4.1 Typy uzlů

- CrimeEvent
- Person
- Location
- Evidence (volitelné)

### 4.2 Typy vztahů

- PERSON_INVOLVED_IN_EVENT
- EVENT_LOCATED_AT
- EVENT_RELATED_TO_EVENT
- PERSON_ASSOCIATED_WITH_PERSON

Každý uzel a vztah bude mít definované atributy (časová značka, role, typ, váha vztahu apod.).

Součástí práce bude zdůvodnění zvoleného grafového modelu a porovnání uložení grafu v relační databázi se specializovanou grafovou databází.

---

## 5. Architektura systému

Systém bude navržen jako webová aplikace:

- **Backend** – REST API
- **Datová vrstva** – databáze PostgreSQL
- **Frontend** – webové rozhraní (mapa + graf)
- **ML modul** – samostatná část pro trénování a vyhodnocení modelů

Architektura bude dokumentována pomocí diagramů.

---

## 6. ML komponenta

V práci bude implementována úloha **predikce vazeb v grafu (link prediction)**: odhad pravděpodobnosti, že mezi dvěma uzly existuje vazba, která dosud není v datech zaznamenána. Výsledky slouží analytikovi jako podnět k prověření, nikoli jako zjištěný fakt.

Primární úlohou je predikce skrytých vazeb mezi osobami. Volitelně bude stejný přístup použit pro vyhledávání souvisejících událostí (např. události hlášené odděleně, ale pravděpodobně způsobené stejnými aktéry).

### Implementované metody:

- Common Neighbors
- Adamic-Adar
- Node2Vec + klasifikátor (např. logistická regrese)

### Vyhodnocení:

- ROC-AUC
- Precision
- Recall

---

## 7. Experimentální vyhodnocení

Experimentální část bude zahrnovat:

- Popis datasetu (veřejně dostupná síť kriminálních vazeb a syntetická data se strukturou odpovídající reálným sítím)
- Rozdělení dat na trénovací a testovací část
- Srovnání metod
- Interpretaci výsledků

---

## 8. Nefunkční požadavky

- Modulární architektura
- Přehledné uživatelské rozhraní
- Reprodukovatelnost experimentů
- Dokumentovaný zdrojový kód
- Automatizované testy

---

## 9. Bezpečnost

Systém musí zajistit bezpečnost citlivých dat a odolnost vůči běžným útokům, zejména:

- Autentizaci uživatelů a autorizaci každého požadavku podle role
- Ochranu přihlášení proti hádání hesel
- Ochranu komunikace mezi webovým rozhraním a serverem (HTTPS, ochrana proti CSRF a XSS)
- Validaci vstupních dat při vytváření a změně záznamů
- Omezení zobrazení citlivých osobních údajů podle role
- Záznam všech změn dat (kdo, kdy, co změnil) v auditním logu, který nelze zpětně upravit

---

## 10. Etické aspekty

Práce bude obsahovat diskusi:

- Rizika zesilování datových biasů
- Limity prediktivní kriminalistiky
- Odpovědné použití analytických nástrojů

---

## 11. Očekávaný výstup

- Funkční prototyp aplikace
- Implementovaný a vyhodnocený model predikce vazeb
- Zdokumentovaná architektura systému
- Kompletní text bakalářské práce