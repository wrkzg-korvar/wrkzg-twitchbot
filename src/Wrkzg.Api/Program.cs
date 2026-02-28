// ============================================================
// Wrkzg.Api – Keine eigenständige Anwendung!
// Diese Datei registriert nur Services und Middleware.
// Der eigentliche Entry Point ist Wrkzg.Host/Program.cs.
//
// Diese Datei ist bewusst LEER – die Registrierungen erfolgen
// über Extension Methods in den jeweiligen Unterordnern:
//
//   builder.Services.AddApiServices()  → wird von Host aufgerufen
//   app.UseApiMiddleware()             → wird von Host aufgerufen
//
// Rider/VS zeigt dieses Projekt als "nicht startbar" an –
// das ist korrekt und gewollt.
// ============================================================
