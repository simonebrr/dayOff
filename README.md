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
     "ServiceAccountKeyPath": "/data/google-sa.json",
     "ServiceAccountKeyJson": "",
     "ServiceAccountEmail": "...@...gserviceaccount.com",
     "TimeZone": "Europe/Rome"
   }
   ```

   - Usa **`ServiceAccountKeyPath`** (percorso al file JSON) **oppure**
     **`ServiceAccountKeyJson`** (contenuto JSON inline, utile su hosting come Fly.io
     tramite secret — es. `GoogleCalendar__ServiceAccountKeyJson`).
   - **`ServiceAccountEmail`** è solo informativa: viene mostrata al capo come
     l'email con cui condividere il calendario.

### Setup per azienda (il capo, dal proprio Profilo)

1. Il capo crea un **calendario Google** per il team.
2. Lo **condivide** con l'email del service account, permesso
   **"Apportare modifiche agli eventi"**.
3. Copia l'**ID del calendario** (Impostazioni calendario → "Integra calendario"
   → ID calendario) e lo incolla in **Profilo → La mia azienda → ID Google
   Calendar aziendale**. In alternativa lo imposta il superadmin dall'area Admin.

Se il service account non è configurato, o un'azienda non ha un calendario
impostato, l'app funziona regolarmente: la sincronizzazione per quell'azienda è
disattivata e il capo vede la vista calendario interna. Le festività restano
comunque lette da Google quando le credenziali sono presenti (fallback locale
altrimenti).
