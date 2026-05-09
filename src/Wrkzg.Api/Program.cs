// ============================================================
// Wrkzg.Api – not a stand-alone application!
// This file only registers services and middleware.
// The actual entry point is Wrkzg.Host/Program.cs.
//
// This file is intentionally EMPTY – registrations happen
// through extension methods in the respective subfolders:
//
//   builder.Services.AddApiServices()  → invoked from Host
//   app.UseApiMiddleware()             → invoked from Host
//
// Rider/VS marks this project as "not runnable" –
// that is correct and intended.
// ============================================================
