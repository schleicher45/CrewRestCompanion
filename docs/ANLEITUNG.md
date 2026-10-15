# Anleitung

> Diese Seite ist die Kurzfassung zum Nachschlagen.
> Die ausführliche, bebilderte Anleitung gibt es als **[PDF](Anleitung.pdf)** –
> mit Bildschirmfotos, allen Einstellungen im Einzelnen und einer Fehlersuche,
> die Schritt für Schritt zur Ursache führt.

Diese Anleitung führt von null bis zum ersten erfolgreichen Testalarm. Rechne mit
15 Minuten, der längste Teil ist das Mailkonto.

## Inhalt

1. [Was du brauchst](#1-was-du-brauchst)
2. [Installation am PC](#2-installation-am-pc)
3. [Bark einrichten (empfohlen)](#3-bark-einrichten-empfohlen)
4. [E-Mail-Wecker einrichten (optional, aber sicher)](#4-e-mail-wecker-einrichten-optional-aber-sicher)
5. [Testen](#5-testen)
6. [Einstellungen im Überblick](#6-einstellungen-im-überblick)
7. [Wenn etwas nicht klappt](#7-wenn-etwas-nicht-klappt)

---

## 1. Was du brauchst

* vPilot 3.x unter Windows
* ein iPhone (für den Mail-Wecker iOS 17 oder neuer) oder ein Android-Gerät für Bark
* mindestens einen der beiden Wege: Bark-App **oder** ein Mailkonto mit Push

Beide Wege lassen sich kombinieren. Bark allein reicht den meisten; der Mail-Wecker ist der
Gürtel zum Hosenträger, weil er auch dann klingelt, wenn Push-Nachrichten unterdrückt werden.

## 2. Installation am PC

1. [Neueste Version herunterladen](https://github.com/schleicher45/CrewRestCompanion/releases/latest).
2. `CrewRestCompanion-Setup-x.y.z.exe` ausführen.
   Windows meldet „Unbekannter Herausgeber", weil das Setup nicht signiert ist:
   **Weitere Informationen → Trotzdem ausführen**.
3. Am Ende startet der **Configurator**. Lass ihn offen, du brauchst ihn gleich.

Die DLL wird noch nicht in vPilot installiert – das passiert erst, wenn du im Configurator
auf **Speichern und installieren** klickst.

## 3. Bark einrichten (empfohlen)

1. **Bark – Push Notifications** aus dem App Store laden (kostenlos).
2. App öffnen, auf das Wolkensymbol tippen. Dort steht eine Adresse der Form
   `api.day.app/DEINKEY`.
3. Nur den Teil **hinter dem Schrägstrich** kopieren – das ist der Device Key.
4. Im Configurator unter **Alarm** in das Feld *Device Key* einfügen.
5. **Speichern und installieren** klicken, dann vPilot neu starten.

Wichtig für den Weckeffekt: In den iOS-Einstellungen bei Bark unter *Mitteilungen* die Option
**Kritische Hinweise** erlauben. Ohne sie bleibt das Handy bei „Nicht stören" stumm.

## 4. E-Mail-Wecker einrichten (optional, aber sicher)

Die Idee: Das Plugin schickt eine Mail mit dem Betreff `ALARM-VPILOT`. Ein Kurzbefehl auf dem
iPhone erkennt sie und startet einen Timer, der klingelt, bis du ihn ausschaltest.

### 4.1 Mailkonto vorbereiten

Lege am besten ein **eigenes Outlook-Konto** nur dafür an, zum Beispiel
`mein-vpilot-alarm@outlook.de`. Grund: Du musst ein App-Passwort erzeugen, und das möchte man
nicht für sein Hauptkonto in einer Textdatei liegen haben.

1. Auf [account.microsoft.com](https://account.microsoft.com) anmelden.
2. **Sicherheit → Erweiterte Sicherheitsoptionen**.
3. **Zweistufige Überprüfung einschalten** (zwingend nötig).
4. Weiter unten **App-Kennwörter → Neues App-Kennwort erstellen**.
5. Das angezeigte Passwort kopieren.

### 4.2 Im Configurator eintragen

Unter **Alarm** die Adresse und das **App-Passwort** eintragen (nicht das Login-Passwort).
Host und Port stimmen für Outlook bereits. Dann **Speichern und installieren**.

Das Passwort wird verschlüsselt gespeichert und ist nur auf diesem PC unter diesem
Windows-Benutzer lesbar.

### 4.3 iPhone: Konto hinzufügen

1. **Einstellungen → Mail → Accounts → Account hinzufügen → Outlook.com**, Konto hinzufügen.
2. **Einstellungen → Mail → Accounts → Datenabgleich**: den Schalter **Push** ganz oben
   einschalten.
3. Darunter den Account antippen und **Push** auswählen (nicht „Abrufen", nicht „Manuell").

Dieser Schritt ist der häufigste Stolperstein. Ohne Push holt das iPhone Mails nur alle
15 Minuten – dann nützt der schönste Wecker nichts.

### 4.4 iPhone: Kurzbefehl anlegen

1. App **Kurzbefehle** öffnen → Reiter **Automation** → oben rechts **+**.
2. **E-Mail** auswählen.
3. *Absender*: die Alarm-Adresse. *Betreff enthält*: `ALARM-VPILOT` (genau so).
4. Unten **Sofort ausführen** wählen – steht dort „Nach Bestätigung fragen", klingelt nichts.
5. **Weiter** → als Aktion **Timer starten** mit **1 Sekunde**.

Optional lassen sich weitere Aktionen anhängen, etwa Licht einschalten.

## 5. Testen

Im Configurator, Seite **Alarm**:

* **Test DM** – so sieht eine leise Info aus.
* **Test ALARM** – so klingt der Weckruf.
* **Testmail** – verschickt eine echte Mail mit dem Betreff `ALARM-VPILOT`.

Das funktioniert ohne vPilot und ohne VATSIM-Verbindung. Kommt hier etwas an, kommt es auch im
Flug an.

## 6. Einstellungen im Überblick

**Installation** – Ordner, DLL, installierte Version, Logdatei.

**Alarm** – wie du geweckt wirst: Bark (Klingelton, Stufe, Lautstärke, Dauerton), Mail-Wecker,
Sperrzeit, Start-Push.

**Ereignisse** – wann es anschlägt. Supervisor, Lotsen-Nachrichten und „contact me" sind immer
aktiv. Dazu kommen SELCAL, Funkspruch ans eigene Callsign, Piloten-DMs und die Meldung, wenn ein
Lotse online geht.

Ein Hinweis zur **Sperrzeit**: Sie verhindert, dass eine Nachrichtenserie mehrfach weckt.
60 Sekunden sind ein guter Wert.

## 7. Wenn etwas nicht klappt

**Der Schnelltest.** Schreib von einem anderen Gerät eine Mail mit dem Betreff `ALARM-VPILOT` an
deine Alarm-Adresse.

* Das iPhone klingelt sofort → Handy ist richtig eingerichtet, der Fehler liegt am PC.
* Die Mail kommt an, aber es bleibt still → iOS-Einstellungen (Push oder Kurzbefehl).
* Die Mail kommt gar nicht an → Mailserver oder Spamfilter.

**Das Plugin erscheint nicht in vPilot.**
Liegt die DLL direkt im Plugins-Ordner? In vPilot unter *Settings → Plugins* nachsehen, ob
„Crew Rest Companion" dort steht.

**Es passiert gar nichts.**
Logdatei im Configurator auf **ausführlich** stellen, einmal fliegen, danach die `plugin.log`
im Plugins-Ordner ansehen – dort steht, was erkannt und was unterdrückt wurde.

Bleibt es dabei: ein [Issue](https://github.com/schleicher45/CrewRestCompanion/issues) mit Version,
Beschreibung und Logauszug.
