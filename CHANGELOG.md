# Changelog

Alle nennenswerten Änderungen an Plugin und Konfigurationsprogramm.

## v1.2.0 — 12.10.2026

### Neue Auslöser

* **SELCAL-Ruf** (`AlarmOnSelcal`) — der Weg, auf dem ATC im echten Betrieb die ruhende Crew
  ruft. Nutzt `IBroker.SelcalAlertReceived`.
* **Funkspruch ans eigene Callsign** (`AlarmOnRadioMessage`) — Textfunk auf der gerasteten
  Frequenz löst Alarm aus, wenn das eigene Callsign vorkommt oder „contact me" im Text steht.
  Das eigene Callsign kommt aus `NetworkConnected`.
* **Lotse geht online** (`NotifyControllerOnline`, `ControllerFilter`, `ControllerQuietSeconds`) —
  leise Info mit Präfix-Filter und Ruhezeit nach dem Verbinden, damit die bereits besetzten
  Stationen keine Flut auslösen.
* **E-Mail-Wecker wieder integriert** — war in der ausgelieferten 1.1.0 vorhanden, im
  Arbeitsstand aber nicht mehr. Läuft jetzt gemeinsam mit Bark, nicht als Alternative.

### Behobene Fehler

* **Die Startmeldung erschien nie.** Der Code suchte per Reflection nach `PostSystemMessage`
  und `AddSystemMessage` — beide gibt es in vPilot nicht. Richtig ist `PostDebugMessage`.
  Damit landet die Meldung im Debugfenster; einen Weg in den Chat bietet vPilot Plugins nicht.
* **Der Mailversand blockierte den vPilot-Thread.** Läuft jetzt im Hintergrund mit Zeitlimit.
* **Die Sperrzeit war mit 10 Sekunden zu kurz** — eine Nachrichtenserie löste mehrere Alarme
  aus (nachweisbar in der alten `pm_hits.txt`). Standard jetzt 60 Sekunden, einstellbar.
* **„contact me" wurde zu streng erkannt.** Jetzt auch mit Komma statt Punkt (121,500),
  mit einer Nachkommastelle (121.5), bei „contact EDDL_TWR on …" und bei „monitor 118.3".
  Zusätzliche Stationskennungen: `_RMP`, `_TMU`, `_ATIS`.
* **Kein eingerichteter Benachrichtigungsweg** wird beim Start erkannt und gemeldet, statt
  stillschweigend nichts zu tun.
* Laufende Alarmwiederholungen enden beim Trennen der Verbindung und beim Schließen von vPilot.

### Geändert

* **Das Mail-Passwort wird verschlüsselt gespeichert** (Windows-DPAPI). Ein im Klartext
  vorgefundenes Passwort wird beim ersten Start automatisch ersetzt. Lesbar nur für denselben
  Windows-Benutzer auf demselben PC — die Einstellungsdatei lässt sich also nicht mehr samt
  Zugangsdaten weitergeben.
* **Wiederholungen entfernt.** Zum Wecken reicht der Dauerton (`AlarmCall=1`): Bark klingelt
  von selbst durch. `RepeatCount` steht jetzt auf 1 und ist nur noch in der Datei einstellbar.
* **Programmdatei heißt CrewRestCompanion.dll** statt ALARM.dll, Produktname und Version
  stimmen (vorher stand in jeder Version 1.0.0.0 und „ALARM").
* **Protokoll abschaltbar** (`LogLevel=off|info|debug`), eine Datei statt dreier.
* Der Quellcode ist aufgeteilt in `Plugin.cs`, `PluginConfig.cs`, `Notifier.cs`, `Log.cs`
  und `Secret.cs`.

## Konfigurationsprogramm (neu in 1.2.0)

Ein eigenes Windows-Programm, das die Einstellungen schreibt, das Plugin installiert und beide
Alarmwege testet — ohne vPilot und ohne VATSIM-Verbindung.

* **Drei Seiten**: Installation, Alarm, Ereignisse. Dazu ein Knopf für alles
  („Speichern und installieren"), „Änderungen verwerfen" und „Standardwerte".
* **Installation ohne Handarbeit**: findet den vPilot-Ordner, legt ihn an, wenn er fehlt
  (vPilot tut das nicht), hält genau eine Sicherung, legt ältere Versionen zur Seite
  (höchstens drei) und entfernt die Windows-Sperre für heruntergeladene Dateien — eine der
  häufigsten Ursachen dafür, dass vPilot ein Plugin ignoriert.
* **Testknöpfe** für normale Info, Weckalarm und Testmail.
* **Hell- und Dunkelmodus**, Standard dunkel. Der zuletzt genutzte Modus wird in
  `%AppData%\CrewRestCompanion\ui.txt` gemerkt. Fensterrahmen, Bildlaufleisten, Auswahl- und
  Zahlenfelder sind mitgefärbt.
* **Eigenes Logo** in heller und dunkler Fassung; das Fenstersymbol wechselt mit dem Modus.
* **Update-Prüfung** beim Start gegen `version.json` im Repository; eine neue Version
  erscheint links in der Leiste. Schlägt die Abfrage fehl, passiert nichts.
* Texte in der Oberfläche kommen ohne Fachbegriffe aus.

## Projektstruktur und Verteilung (neu in 1.2.0)

* Plugin und Konfigurationsprogramm liegen in einem Repository mit `plugin/`, `configurator/`,
  `installer/`, `lib/`, `docs/` und `.github/`.
* **Inno-Setup-Skript** für ein Setup ohne Administratorrechte. Beim Deinstallieren wird
  gefragt, ob Plugin und Einstellungen mit sollen.
* **GitHub-Workflow**, der bei jedem Push baut, Issue-Vorlagen, MIT-Lizenz,
  README und vollständige Anleitung.

## Offen

* Funktionstest im Flug: SELCAL, Funkspruch und Lotsen-Meldung sind eingebaut, aber noch nie
  live ausgelöst worden.
* Zweiter Push-Weg (ntfy oder Pushover) für Android.
* Signierte Setup-Datei, damit Windows nicht vor „Unbekannter Herausgeber" warnt.
* ~~Automatisches Rasten bei „contact me"~~ — gestrichen: die Plugin-Schnittstelle von vPilot
  meldet keine eigenen Frequenzwechsel und kann die Funkgeräte nicht stellen.

## v1.1.0 — 13.12.2025

* E-Mail-Alarm mit Betreff `ALARM-VPILOT` als Auslöser für den iPhone-Kurzbefehl.
* Bark-Alarm mit Critical Alert.
* Dokumentation als PDF und Word-Datei.

## v1.0.0 — 19.09.2025

* Erste Fassung: Bark-Push bei Supervisor, Lotsen-Nachricht und „please contact me on …".
