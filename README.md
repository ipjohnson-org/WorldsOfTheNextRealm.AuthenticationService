# Worlds of the Next Realm — Authentication Service

Handles user registration, login, JWT token issuance, token refresh, and token revocation. Deployed as an ASP.NET Core Lambda function behind API Gateway.

---

## Repository Structure

```
.
├── src/
│   └── WorldsOfTheNextRealm.AuthenticationService/
│       ├── Endpoints/          HTTP request handlers
│       ├── Services/           Business logic (tokens, signing keys, password hashing)
│       ├── Entities/           DynamoDB data models
│       ├── Models/             Request/response DTOs
│       ├── Configuration/      Settings classes
│       └── Program.cs          Entry point
├── tests/
│   └── WorldsOfTheNextRealm.AuthenticationService.Tests/
│       ├── Endpoints/
│       ├── Services/
│       └── TestHelpers/
├── cdk/
│   ├── cdk.json
│   └── src/Cdk/
│       └── Program.cs          CDK app (LambdaApiStack + Secrets Manager)
├── .github/workflows/
│   └── build.yml
├── nuget.config
└── WorldsOfTheNextRealm.AuthenticationService.sln
```

---

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| POST | `/auth/register` | Register a new user |
| POST | `/auth/login` | Authenticate and return token pair |
| POST | `/auth/token/refresh` | Exchange refresh token for new access token |
| POST | `/auth/token/revoke` | Revoke a refresh token family |
| GET | `/auth/.well-known/jwks.json` | Public JWKS endpoint |

---

## Security

- **Passwords** — Argon2id hashing (64 MB memory, 3 iterations)
- **Access tokens** — RS256 JWT, 6-hour lifetime
- **Refresh tokens** — Family-based rotation with replay detection, 60-day lifetime
- **Signing keys** — RSA 2048-bit, AES-256 encrypted at rest via Secrets Manager
- **Account lockout** — Configurable failed attempt tracking

---

## Data Storage

Three DynamoDB tables: `AuthMain` (email lookup, token families, signing keys), `AuthCredentials` (password hashes, lockout state), `AuthSigningKeys` (encrypted RSA keys).

---

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Runtime | .NET 8.0 |
| Framework | ASP.NET Core (minimal APIs) |
| Hosting | AWS Lambda via API Gateway HTTP API |
| Infrastructure | AWS CDK v2 (.NET) |
| Password hashing | Konscious.Security.Cryptography.Argon2 |
| JWT | Microsoft.IdentityModel.JsonWebTokens |
| Secrets | AWS Secrets Manager |
| Shared | WorldsOfTheNextRealm.BackendCommon NuGet package |

---

## Build & Deploy

```bash
# Build
dotnet build --configuration Release

# Test
dotnet test --configuration Release

# Publish
dotnet publish src/WorldsOfTheNextRealm.AuthenticationService/WorldsOfTheNextRealm.AuthenticationService.csproj --configuration Release

# CDK deploy (from cdk/ directory)
cd cdk
cdk deploy --all --require-approval never -c env=beta
```

---

## CI/CD

GitHub Actions workflow (`.github/workflows/build.yml`):

1. **build** — Restore, build, and test on all pushes/PRs
2. **deploy-beta** — Publish and CDK deploy to beta (main branch only)
3. **deploy-prod** — CDK deploy to prod (after beta succeeds)

---

## Related Repos

All repos under `~/WorldsOfTheNextRealm/WorldsOfTheNextRealm.<name>`. See [Documentation](https://github.com/ipjohnson-org/WorldsOfTheNextRealm.Documentation) for the full list.
