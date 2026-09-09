# dayOff

Gestionale ferie / permessi / malattie (Blazor Server + EF Core + SQLite).

## Integrazione Google Calendar (condiviso)

Le assenze **approvate** (ferie, permessi) e le **malattie** vengono sincronizzate
su un unico **Google Calendar condiviso**. I dipendenti vedono solo le proprie
assenze nella vista calendario interna all'app; il **capo** vede il Google
Calendar condiviso incorporato con tutte le assenze del team.

### Setup (una tantum)

1. **Google Cloud Console** → crea un progetto e abilita la **Google Calendar API**.
2. Crea un **Service Account** e genera una **chiave JSON**.
3. Crea (o scegli) un **calendario Google** dedicato al team e **condividilo** con
   l'email del service account (`...@...gserviceaccount.com`) con permesso
   **"Apportare modifiche agli eventi"**.
4. Recupera l'**ID del calendario** (Impostazioni calendario → "Integra calendario" → ID calendario).
5. Configura l'app (`appsettings.json` o variabili d'ambiente):

   ```json
   "GoogleCalendar": {
     "CalendarId": "xxxxxxxx@group.calendar.google.com",
     "ServiceAccountKeyPath": "/data/google-sa.json",
     "ServiceAccountKeyJson": "",
     "EmbedSrc": "",
     "TimeZone": "Europe/Rome"
   }
   ```

   - Usa **`ServiceAccountKeyPath`** (percorso al file JSON) **oppure**
     **`ServiceAccountKeyJson`** (contenuto JSON inline, utile su hosting come Fly.io
     tramite secret).
   - **`EmbedSrc`** è opzionale: se vuoto, l'app costruisce l'URL di embed dal
     `CalendarId`. Puoi incollare qui l'URL completo di "Integra calendario" se
     vuoi opzioni di visualizzazione specifiche.

Se la sezione non è configurata, l'app funziona regolarmente: la sincronizzazione
è disattivata e il capo vede la vista calendario interna.
