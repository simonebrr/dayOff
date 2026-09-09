# dayOff

Gestionale ferie / permessi / malattie (Blazor Server + EF Core + SQLite).

## Integrazione Google Calendar (multi-azienda)

App multi-azienda: **ogni azienda ha il proprio Google Calendar**. Un **unico
service account** (globale) scrive sui calendari di tutte le aziende; ogni
azienda condivide il proprio calendario con quel service account.

Le assenze **approvate** (ferie, permessi) e le **malattie** vengono sincronizzate
sul Google Calendar dell'azienda del dipendente. I dipendenti vedono solo le
proprie assenze nella vista calendario interna all'app; il **capo** vede il
Google Calendar della propria azienda incorporato (con overlay delle festività
italiane, in rosso, sempre da Google).

### Setup globale (una tantum, superadmin/dev)

1. **Google Cloud Console** → crea un progetto e abilita la **Google Calendar API**.
2. Crea un **Service Account** e genera una **chiave JSON**. Annota la sua email
   (`...@...gserviceaccount.com`).
3. Configura l'app (`appsettings.json` o variabili d'ambiente):

   ```json
   "GoogleCalendar": {
     "CalendarId": "xxxxxxxx@group.calendar.google.com",
     "ServiceAccountKeyPath": "/data/google-sa.json",
     "ServiceAccountKeyJson": "",
     "ServiceAccountEmail": "...@...gserviceaccount.com",
     "TimeZone": "Europe/Rome"
   }
   ```

   - **`CalendarId`** (globale) è il calendario unico della modalità A qui sotto;
     lascialo vuoto se usi solo calendari per-azienda (modalità B).

   - Usa **`ServiceAccountKeyPath`** (percorso al file JSON) **oppure**
     **`ServiceAccountKeyJson`** (contenuto JSON inline, utile su hosting come Fly.io
     tramite secret — es. `GoogleCalendar__ServiceAccountKeyJson`).
   - **`ServiceAccountEmail`** è solo informativa: viene mostrata al capo come
     l'email con cui condividere il calendario.

### Scelta del calendario: due modalità

**A) Un solo calendario (semplice, consigliata).** Crei UN calendario Google,
lo condividi una volta con il service account e ne metti l'ID nel campo globale
`GoogleCalendar:CalendarId`. Le ferie di **tutte** le aziende ci finiscono: tu
(owner) hai la visione completa in Google. Nessun lavoro per le singole aziende.

> Nota privacy: questo calendario unico contiene tutte le aziende, quindi **non**
> viene incorporato nell'app per i capi (vedrebbero anche le altre aziende). Ogni
> capo vede la **sua** azienda nella vista calendario interna dell'app, già
> filtrata e con i festivi in rosso.

**B) Un calendario per azienda (embed Google isolato per il capo).** Se vuoi che
ogni capo veda un vero Google Calendar incorporato con solo la sua azienda:

1. Il capo crea un **calendario Google** per il team.
2. Lo **condivide** con l'email del service account, permesso
   **"Apportare modifiche agli eventi"**.
3. Copia l'**ID del calendario** (Impostazioni calendario → "Integra calendario"
   → ID calendario) e lo incolla in **Profilo → La mia azienda → ID Google
   Calendar aziendale** (oppure lo imposta il superadmin dall'area Admin).

L'ID per-azienda, se presente, ha **precedenza** sul calendario globale.

Se il service account non è configurato (o non c'è né un calendario aziendale né
quello globale), l'app funziona regolarmente: la sincronizzazione è disattivata e
il capo vede la vista calendario interna. Le festività restano comunque lette da
Google quando le credenziali sono presenti (fallback locale altrimenti).
