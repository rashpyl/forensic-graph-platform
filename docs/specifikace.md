# Specifikace bakalářské práce

## Název práce

Návrh a implementace grafové analytické aplikace pro vizualizaci, predikci a ukladání kriminalistických událostí.

---

## 1. Úvod a motivace

Kriminalistická analýza pracuje s rozsáhlými a silně propojenými daty – událostmi, osobami, lokalitami, důkazy a jejich vzájemnými vztahy. Relační databázové systémy nejsou vždy optimální pro reprezentaci komplexních vztahových struktur.

Grafový přístup umožňuje přirozenou reprezentaci vazeb mezi entitami a efektivní provádění vztahových dotazů. Tato práce se zaměřuje na návrh a implementaci prototypu systému, který modeluje grafovou strukturu kriminalistických dat nad relační databází (PostgreSQL) – při daném rozsahu dat zvolené řešení umožňuje provádět potřebné vztahové a analytické dotazy při nižší architektonické složitosti.

---

## 2. Cíl práce

Hlavní cíle práce:

- Navrhnout grafový datový model kriminalistických událostí
- Implementovat webovou aplikaci pro správu a vizualizaci těchto dat
- Realizovat analytický modul nad grafovou strukturou umožňující výpočet grafových metrik (např. degree centrality), vyhledávání cest mezi entitami, analýzu propojenosti a agregované statistické dotazy nad kriminalistickými daty.
- Implementovat a experimentálně vyhodnotit metodu predikce vazeb (link prediction)
- Formou rozhovorů a dotazníkového šetření s potenciálními uživateli (policisty/analytiky) ověřit užitečnost aplikace jako celku i jednotlivých funkcí a zapracovat jejich připomínky a požadavky do návrhu systému
- Diskutovat etické aspekty prediktivní kriminalistiky

Výstupem bude funkční prototyp systému a experimentální vyhodnocení navržených metod.

---

## 3. Funkční požadavky

### 3.1 Správa dat

Systém umožňuje:

- Evidenci kriminalistických událostí s atributy: název, popis, adresa, čas spáchání, závažnost (na škále 1–5) a GPS souřadnice
- Vytváření, editaci a mazání těchto událostí
- Evidenci osob s atributy: jméno a příjmení, telefonní kontakt, fyzický popis, seznam občanství a seznam čísel dokladů (pas apod.)
- Vytváření, editaci a mazání osob a jejich vyhledávání podle jména (autocomplete)
- Přiřazení osoby ke konkrétní události s určením její role (oběť, podezřelý, svědek, pachatel, oznamovatel, policista) a odebrání tohoto přiřazení
- Vzájemné propojení událostí (např. souvislost, návaznost) s volitelnou textovou poznámkou k danému vztahu a jeho zrušení
- Stránkovaný výpis událostí s možností filtrování podle časového rozmezí a rozmezí závažnosti
- Stránkovaný výpis osob s fulltextovým vyhledáváním

---

### 3.2 Mapová vizualizace

Systém umožňuje:

- Zobrazení všech georeferencovaných událostí na interaktivní mapě
- Barevné odlišení markerů podle závažnosti události a jejich shlukování (clustering) při oddálení mapy pro přehlednost ve vysoce zatížených oblastech
- Zobrazení náhledové bubliny (tooltip) s názvem, adresou a závažností při najetí na marker
- Založení nové události na konkrétní poloze kliknutím přímo do mapy
- Otevření detailního postranního panelu události po kliknutí na marker
- Legendu barevného odlišení závažnosti
- Filtrování zobrazených událostí podle časového rozmezí a typu/závažnosti
- Agregované zobrazení hustoty událostí v dané oblasti (heatmap)
- Zobrazení statistik za vybraný region (např. počet a průměrná závažnost událostí)

---

### 3.3 Vztahová (grafová) vizualizace

Po výběru konkrétní události systém umožňuje:

- Zobrazit seznam osob přiřazených k dané události včetně jejich role
- Zobrazit seznam událostí propojených s danou událostí včetně poznámky k vztahu a tato propojení spravovat (přidávat/odebírat)
- Zobrazit vztahy dané události a jejích přímo souvisejících entit (osob a propojených událostí) formou interaktivního grafu (uzly a hrany)
- Interaktivně rozšiřovat zobrazený graf o další úroveň vztahů (např. rozkliknutím sousedního uzlu)
- Zvýraznit v grafu entity s predikovaným vztahem na základě ML modulu (viz kapitola 6)

---

### 3.4 Statistický modul

Systém poskytuje souhrnné analytické přehledy nad evidovanými daty, zejména:

- Počet a rozložení událostí podle typu a závažnosti
- Vývoj počtu událostí v čase (trendy, sezónnost)
- Identifikaci lokalit s nejvyšším výskytem událostí (hotspoty)
- Identifikaci nejvíce propojených osob a událostí v síti (degree centrality)
- Velikost a počet souvislých komponent grafu (izolované případy vs. rozsáhlé propojené sítě)
- Celkové souhrnné počty evidovaných entit (osoby, události, vztahy)

Konkrétní rozsah a prioritizace jednotlivých přehledů bude upřesněna na základě konzultace s odborníkem z praxe (policejním analytikem).

---

### 3.5 Prediktivní vrstva

Nad vybudovaným grafovým modelem systém umožňuje:

- Zobrazit odhadovanou pravděpodobnost dosud neevidovaného vztahu mezi dvěma entitami (link prediction)
- Zobrazit odhad pravděpodobnosti budoucí kriminální události na základě struktury a vývoje sítě
- Prezentovat tyto predikce jako doplňkovou informaci v rámci grafové vizualizace a detailu entity, nikoli jako automatické rozhodnutí (viz etické aspekty, kapitola 9)

---

## 4. Datový model

Systém bude založen na Postgres databázi.

### 4.1 Typy uzlů

- CrimeEvent – nese i atributy polohy (adresa, GPS souřadnice); samostatný uzel typu Location se nezavádí

- Person

### 4.2 Typy vztahů

- PERSON_INVOLVED_IN_EVENT
- EVENT_RELATED_TO_EVENT
- PERSON_ASSOCIATED_WITH_PERSON (plánováno – vztah mezi dvěma osobami, využívaný zejména predikční vrstvou pro odhad skrytých sociálních vazeb)

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

### 7.1 Vyhodnocení prediktivního modelu

Experimentální část bude zahrnovat:

- Popis datasetu
- Rozdělení dat na trénovací a testovací část
- Srovnání metod
- Interpretaci výsledků

### 7.2 Uživatelský výzkum

Za účelem ověření praktické užitečnosti aplikace proběhne kvalitativní i kvantitativní šetření mezi potenciálními uživateli (policisty, kriminalistickými analytiky):

- Polostrukturované rozhovory s jedním nebo více policejními experty zaměřené na reálné potřeby při vyšetřování a na zpětnou vazbu k navrženým funkcím (zejména statistický modul a predikční vrstva, viz kapitoly 3.4 a 3.5)
- Dotazníkové šetření hodnotící vnímanou užitečnost a prioritu jednotlivých funkcí aplikace (např. formou škálových otázek)
- Shrnutí získaných zjištění a jejich promítnutí do finální podoby funkčních požadavků a prioritizace implementovaných funkcí
- Diskuse omezení tohoto šetření (velikost a reprezentativnost vzorku respondentů)

---

## 8. Nefunkční požadavky

- Modulární architektura
- Přehledné uživatelské rozhraní
- Reprodukovatelnost experimentů
- Dokumentovaný zdrojový kód
- Zajištění bezpečnosti citlivých dat a odolnosti vůči útokům – zejména při zápisu nových dat či úpravě stávajících záznamů a při komunikaci mezi jednotlivými komponentami systému

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