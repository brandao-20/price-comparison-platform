# Security notes

This is an educational portfolio application, not a claim of production readiness.

## Previously exposed credentials

Earlier revisions contained JWT signing material, development database credentials, and predictable administrator credentials. Removing them from the working tree does not remove them from Git history, existing databases, or old logs. Treat any reused values as compromised: replace the signing key, rotate database passwords, change or disable the old administrator account, and invalidate old tokens. No history rewrite or existing database modification is performed by this cleanup.

## Current protections

- JWT configuration is required and validated; there is no secret fallback.
- Administrator creation is explicit and disabled by default. Registration never grants administrator privileges.
- API responses exclude password hashes and Google credentials, including nested user entities.
- Protected writes enforce server-side roles; messages and favourites check the requesting user.
- Google login uses a short-lived, single-use code bound to a browser-generated verifier. JWTs are returned in response bodies.
- Authentication keys, tokens, claims, message contents, and browser API keys are not logged by application diagnostics.
- Request URL logging is suppressed for ASP.NET Core hosting. Proxies and hosting platforms must also redact query strings: SignalR browser transports may use `access_token` in the hub URL.
- CSV fields are escaped and formula-like text is neutralized. User input in the help widget and map information windows is treated as text.

See [configuration and validation](docs/configuration.md) for local setup, bootstrap, migration cautions, and optional integrations.

## Remaining deployment considerations

- Serve the API and frontend through HTTPS. Configure trusted proxy forwarding at the hosting boundary if TLS terminates there; no deployment topology is provided here.
- JWTs remain in browser local storage and can be stolen by successful XSS. Audit third-party scripts and keep dependencies and the installed .NET runtime patched. The Google code store is process-local.
- The UI keeps account state in memory and currently requires login again after a full page reload. Logout removes the stored JWT and closes the active client connections, but does not revoke a previously copied token on the server.
- Existing password hashes use PBKDF2-SHA256 with a random salt and 10,000 iterations. A future versioned hashing/rehashing migration should increase the work factor while preserving existing accounts.
- Login/registration do not yet have rate limiting or account lockout. Price confirmations are not deduplicated per user, and the points/credibility model is not abuse-resistant.
- Role changes and account deletion do not revoke an already-issued JWT immediately; tokens have a two-hour lifetime. Revalidate account state or implement revocation before using this for higher-trust deployments.
- UI tests and the mock email observer are demonstrations; the observer does not send email. The help widget is command-based and does not call an AI service.

## Third-party material

No project-wide license has been added or inferred. Preserve the project's academic/group attribution. Review dependency and asset licensing before redistribution or commercial use, including iText's license, the Fluent Assertions terms used by the existing tests, and retailer logo rights.

## References

- [ASP.NET Core configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0)
- [SignalR authentication and token handling](https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-8.0)
- [Microsoft.Bcl.Memory advisory and fixed version](https://github.com/advisories/GHSA-73j8-2gch-69rq)
- [Bouncy Castle advisory and fixed version](https://github.com/advisories/GHSA-8xfc-gm6g-vgpv)
