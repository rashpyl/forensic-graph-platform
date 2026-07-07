# Specifikace bakalářské práce

## Název práce

Návrh a implementace grafové analytické aplikace pro vizualizaci, predikci a ukladání kriminalistických událostí.

---

## 1. Úvod a motivace

Kriminalistická analýza pracuje s rozsáhlými a silně propojenými daty – událostmi, osobami, lokalitami, důkazy a jejich vzájemnými vztahy. Relační databázové systémy nejsou vždy optimální pro reprezentaci komplexních vztahových struktur.

Grafový přístup umožňuje přirozenou reprezentaci vazeb mezi entitami a efektivní provádění vztahových dotazů. Tato práce se zaměřuje na návrh a implementaci prototypu systému, který využívá grafovou databázi pro analytické zpracování kriminalistických dat.

---

## 2. Cíl práce

Hlavní cíle práce:

- Navrhnout grafový datový model kriminalistických událostí
- Implementovat desktopovou aplikaci pro správu a vizualizaci těchto dat
- Realizovat analytický modul nad grafovou strukturou umožňující výpočet grafových metrik (např. degree centrality), vyhledávání cest mezi entitami, analýzu propojenosti a agregované statistické dotazy nad kriminalistickými daty.
- Implementovat a experimentálně vyhodnotit metodu predikce vazeb (link prediction)
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

Po výběru konkrétní události bude možné:

- Zobrazit lokální graf jejích vztahů
- Vizualizovat osoby, související události a další entity
- Interaktivně rozšiřovat graf

---

### 3.4 Statistický modul

Systém bude poskytovat:

- Počet událostí podle typu
- Rozložení událostí v čase
- Identifikaci nejvíce propojených uzlů (degree centrality)
- Základní agregace nad grafem

---

## 4. Datový model

Systém bude založen na Postgres databázi.

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

Součástí práce bude zdůvodnění zvoleného grafového modelu a stručné porovnání s relační reprezentací.

---

## 5. Architektura systému

Systém bude navržen jako webová aplikace:

- **Backend** – REST API
- **Datová vrstva** – Postgres databáze
- **Frontend** – webové rozhraní (mapa + graf)
- **ML modul** – samostatná část pro trénování a vyhodnocení modelů

Architektura bude dokumentována pomocí diagramů.

---

## 6. ML komponenta

V práci bude implementována úloha **predikce vazeb v grafu (link prediction)**.

Cílem je předpovědět pravděpodobnost vzniku nové vazby mezi dvěma uzly.

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

- Popis datasetu
- Rozdělení dat na trénovací a testovací část
- Srovnání metod
- Interpretaci výsledků

---

## 8. Nefunkční požadavky

- Modulární architektura
- Přehledné uživatelské rozhraní
- Reprodukovatelnost experimentů
- Dokumentovaný zdrojový kód

---

## 9. Etické aspekty

Práce bude obsahovat diskusi:

- Rizika zesilování datových biasů
- Limity prediktivní kriminalistiky
- Odpovědné použití analytických nástrojů

---

## 10. Očekávaný výstup

- Funkční prototyp aplikace
- Implementovaný a vyhodnocený model predikce vazeb
- Zdokumentovaná architektura systému
- Kompletní text bakalářské práce

## 11. zajistit bezpečnost senzitivních dat, vyhnout se možným útokům (při přidání nových dat do DB nebo změně starých, při komunikaci mezi nodami)