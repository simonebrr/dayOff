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
     "OAuthClientId": "xxxx.apps.googleusercontent.com",
     "OAuthClientSecret": "xxxx",
     "CalendarId": "",
     "ServiceAccountKeyPath": "",
     "ServiceAccountKeyJson": "",
     "ServiceAccountEmail": "",
     "TimeZone": "Europe/Rome"
   }
   ```

   - **`OAuthClientId` / `OAuthClientSecret`** → per il collegamento del capo con
     un clic (metodo consigliato, vedi sotto).
   - **`CalendarId`** (globale) è il calendario unico della modalità A qui sotto;
     lascialo vuoto se usi OAuth o solo calendari per-azienda (modalità B).

   - Usa **`ServiceAccountKeyPath`** (percorso al file JSON) **oppure**
     **`ServiceAccountKeyJson`** (contenuto JSON inline, utile su hosting come Fly.io
     tramite secret — es. `GoogleCalendar__ServiceAccountKeyJson`).
   - **`ServiceAccountEmail`** è solo informativa: viene mostrata al capo come
     l'email con cui condividere il calendario.

### Collegamento del capo con un clic (OAuth — consigliato)

Il modo più comodo: il capo **non deve configurare nulla a mano**.

Setup (una tantum, owner/dev) in Google Cloud Console:
1. **Schermata consenso OAuth** → configurala (tipo "Esterno" va bene).
2. **Credenziali → Crea credenziali → ID client OAuth → Applicazione web**.
3. Aggiungi come **URI di reindirizzamento autorizzato**:
   `https://<dominio-app>/google/callback` (e `http://localhost:<porta>/google/callback` per i test).
4. Copia **Client ID** e **Client Secret** in `GoogleCalendar:OAuthClientId` e
   `GoogleCalendar:OAuthClientSecret`.

Poi ogni capo, dal proprio **Profilo → La mia azienda**, clicca **«Collega
account»**, autorizza una volta il suo Google, e le assenze approvate della sua
azienda finiscono automaticamente nel **suo Google Calendar personale**
(calendario `primary`). Nessun ID da copiare, nessuna condivisione manuale.

### (Alternativa) Calendario condiviso via service account: due modalità

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
