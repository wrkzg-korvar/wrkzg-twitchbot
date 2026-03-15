# Branching Strategy

## Branches

| Branch | Purpose | Deploys to |
|---|---|---|
| `main` | Stable releases only | GitHub Releases (stable) |
| `develop` | Active development | CI only |

## Workflow

1. All development happens on `develop` (or feature branches merged into `develop`)
2. When ready for a stable release:
   - Merge `develop` → `main`
   - Tag `main` with `vX.Y.Z` (e.g., `v1.1.0`)
   - Push the tag → Release workflow creates a stable GitHub Release
3. For pre-releases / beta testing:
   - Tag `develop` with `vX.Y.Z-beta.N` (e.g., `v1.1.0-beta.1`)
   - Push the tag → Release workflow creates a pre-release on GitHub

## Tag Format

| Tag | Type | Example |
|---|---|---|
| `vX.Y.Z` | Stable Release | `v1.0.0`, `v1.1.0` |
| `vX.Y.Z-beta.N` | Beta Pre-Release | `v1.1.0-beta.1` |
| `vX.Y.Z-rc.N` | Release Candidate | `v1.1.0-rc.1` |

## Quick Reference

```bash
# Stable release
git checkout main
git merge develop
git tag -a v1.1.0 -m "v1.1.0 — Community Features"
git push origin main
git push origin v1.1.0

# Pre-release from develop
git checkout develop
git tag -a v1.1.0-beta.1 -m "v1.1.0-beta.1 — Chat Games preview"
git push origin develop
git push origin v1.1.0-beta.1
```
