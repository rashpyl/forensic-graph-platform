# User Guide

**Audience:** Police officer / analyst using the Forensic Graph Platform.

**This document covers** every action available in the web UI, in the
order you would naturally discover them.

If you are looking for how to *install* the app, see [`../README.md`](../README.md).
If you are looking for how the app is *built*, see [`DEVELOPER_GUIDE.md`](./DEVELOPER_GUIDE.md).

---

## 1. Overview

The Forensic Graph Platform is an interactive world map for pinning crime
events, describing them, attaching the people involved, and manually
recording which events you believe are connected.

Two pages are available from the navbar:

| Page       | Purpose                                                       |
|------------|---------------------------------------------------------------|
| **Map**    | Primary workspace. All events are pins on a world map.        |
| **Persons**| Searchable catalogue of every person known to the system.     |

Three navbar actions are always available:

| Button        | Effect                                                     |
|---------------|------------------------------------------------------------|
| ➕ New Event  | Enters *location-picking mode* on the map.                 |
| 👤 New Person | Opens the person-creation dialog from anywhere.            |
| ℹ  About      | Shows a short summary of the project.                      |

> **Note.** *New Event* is only enabled on the Map page — placing an event
> requires clicking the map to fix its coordinates.

---

## 2. Reading the map

- **Zoom** with the mouse wheel, `+` / `-` buttons, or pinch on a trackpad.
- **Pan** by dragging.
- **Marker clusters.** When many events overlap at your current zoom level
  they collapse into a numbered circle. Click a cluster to zoom into
  it, or keep zooming in — clusters break apart automatically.
- **Marker colour** encodes severity (1 = green, 2 = light green,
  3 = yellow, 4 = orange, 5 = red). A legend is anchored to the bottom
  right corner of the map.
- **Hover a marker** to see a compact tooltip with the event's title and
  address.
- **Click a marker** to open the full detail panel on the right.

---

## 3. Creating a new event

1. Click **➕ New Event** in the navbar.
2. A banner appears at the top of the map:
   *"🎯 Click on the map to place the new event — cancel"*.
   Your cursor becomes a crosshair.
3. Click the exact spot on the map where the event happened.
4. The dialog opens with the coordinates pre-filled. Complete the form:

   | Field         | Notes                                                      |
   |---------------|------------------------------------------------------------|
   | Title         | Required. Max 200 characters.                              |
   | Description   | Optional free text. Max 4000 characters.                   |
   | Address       | Optional human-readable address. Max 500 characters.       |
   | Occurred at   | Date and time in **local** (European) time. Required.      |
   | Severity      | 1–5. The colour swatch updates live.                       |

5. Click **Create event**. On success the dialog closes, the new pin is
   added to the map, and its detail panel opens automatically.

**To cancel** location-picking press `Esc` or click *cancel* in the banner.

---

## 4. Working with an event's detail panel

Clicking any marker slides a panel in from the right with four sections:

### 4.1 Summary
Title, severity chip, address, occurred-at (formatted `dd.mm.yyyy HH:mm`
in your local time zone), and description.

### 4.2 Persons
The list of persons already attached to this event, each with their role.

**Add a person**
1. Type at least two letters of a name into the *Add person* box.
   Suggestions appear after.
2. Click a suggestion.
3. Pick a role from the dropdown: *victim / suspect / witness /
   perpetrator / reporter / officer*.
4. Click **Add**.

**Remove a person from this event**
Click the ✕ next to their row. This only detaches them from *this* event;
the person record itself is preserved.

**Person doesn't exist yet?**
Open **👤 New Person** in the navbar, create them, then return to the
event and add them via the autocomplete.

### 4.3 Linked events
Events another officer (or you) has manually marked as related.

**Link another event**
1. Start typing the other event's title in the *Link event* box.
2. Optionally enter a short note (max 500 chars) describing the reason.
3. Click **Link**.

**Unlink**
Click the ✕ next to an existing link.

Links are directional in the database (they have a "from" and a "to"),
but the detail panel shows the connection on both events — open either
event and the peer appears in its **Linked events** list.

### 4.4 Danger zone
Deletes the event permanently. A confirmation prompt appears first.
Deletion cascades — all person-attachments and links referring to this
event are removed automatically, but the persons themselves survive.

**Close the panel** with the ✕ in its header or by clicking a different
marker.

---

## 5. Persons page

Navigate via the **Persons** link in the navbar.

- The table lists every person, with their citizenships, passport numbers
  (all shown as chips), phone, and — under their name — physical
  description in small grey text.
- The **search box** filters client-side across name, citizenship,
  passport number, and phone as you type.
- The counter next to the search shows how many rows match the filter
  out of the total.
- The **✕** at the end of each row deletes the person after a
  confirmation. This cascades: every event assignment referencing that
  person is removed as well.

### Creating a person

Click **👤 New Person**.

| Field                | Notes                                                    |
|----------------------|----------------------------------------------------------|
| First name           | Required. Max 100 characters.                            |
| Last name            | Required. Max 100 characters.                            |
| Citizenships         | Multi-value. Type a value, press **Enter** or **comma**  |
|                      | to commit; **Backspace** in the empty field removes the  |
|                      | last chip; the **×** on a chip removes just that one.    |
| Passport numbers     | Same chip mechanics as citizenships.                     |
| Phone                | Optional. Max 40 characters.                             |
| Physical description | Optional multi-line notes. Max 2000 characters.          |

Click **Create person**. On success the dialog closes and the new person
becomes immediately findable in the autocomplete on any event's detail
panel and in the Persons page.

---

## 6. Understanding what you see on first launch

The app ships with **seeded demo data** in Development mode: around 50
persons and 30 events spread across Prague, Kyiv, Lviv, and Berlin, plus
some role assignments and links between events. This is regenerated
deterministically from a fixed random seed, so everyone testing the app
sees the same picture. If the database is empty the first `dotnet run`
inserts the demo data automatically.
---

## 7. Troubleshooting

**"I don't see any pins."**
- Zoom out — you may be looking at an empty region.
- Check the top-right corner: if a red banner reads *"Could not load
  events"* the backend isn't reachable. See the README's Setup section
  for the run order.

**"The autocomplete finds no one."**
- The person doesn't exist yet — use **New Person**.
- Or the substring you typed doesn't match. Try the last name only.

**"My time shows up wrong on the pin."**
- All times are stored in UTC on the server and rendered in your
  browser's local time in the European `dd.mm.yyyy HH:mm` format.
  If your machine's time zone is misconfigured, the display will be
  off — fix it in your OS settings.

**"I created an event but the pin is in the wrong spot."**
- The coordinates are captured the moment you click the map — the dialog only shows them, it doesn't let you edit them. Delete the event in the Danger zone and create it again, this time clicking the correct spot.