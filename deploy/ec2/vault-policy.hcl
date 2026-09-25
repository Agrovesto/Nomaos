# ─────────────────────────────────────────────────────────────────────────────
# Vault policy for the NomaOS Onboarding API AppRole.
#
# Least privilege: read-only on exactly one KV path per environment, plus the
# token self-management endpoints the renderer and the app's own Vault
# configuration provider need. No list, no write, no metadata delete.
#
#   vault policy write nomaos-onboarding-prod deploy/ec2/vault-policy.hcl
#
#   vault write auth/approle/role/nomaos-onboarding-prod \
#       token_policies=nomaos-onboarding-prod \
#       secret_id_ttl=720h \
#       token_ttl=1h \
#       token_max_ttl=4h \
#       secret_id_num_uses=0 \
#       bind_secret_id=true \
#       token_bound_cidrs="10.0.0.0/16"     # the app subnet — a leaked secret_id
#                                           # is then useless outside the VPC
#
#   vault read  -field=role_id   auth/approle/role/nomaos-onboarding-prod/role-id
#   vault write -field=secret_id -f auth/approle/role/nomaos-onboarding-prod/secret-id
#
# Put those two values in /opt/nomaos/vault-approle (0600, root-owned):
#   VAULT_ROLE_ID=...
#   VAULT_SECRET_ID=...
# ─────────────────────────────────────────────────────────────────────────────

# KV v2 data read (the "/data/" segment is required for v2 mounts).
path "kv/data/nomaos/prod/appsettings" {
  capabilities = ["read"]
}

# Version metadata — lets the renderer log which version it deployed.
path "kv/metadata/nomaos/prod/appsettings" {
  capabilities = ["read"]
}

# KV v1 fallback, for mounts that have not been upgraded.
path "kv/nomaos/prod/appsettings" {
  capabilities = ["read"]
}

# Token hygiene: renew while the process lives, revoke as soon as the render
# script is done.
path "auth/token/lookup-self" {
  capabilities = ["read"]
}

path "auth/token/renew-self" {
  capabilities = ["update"]
}

path "auth/token/revoke-self" {
  capabilities = ["update"]
}

# ── Dev environment ──────────────────────────────────────────────────────────
# Use a SEPARATE role + policy in dev; never let one AppRole read both.
#
# path "kv/data/nomaos/dev/appsettings" { capabilities = ["read"] }
# path "kv/nomaos/dev/appsettings"      { capabilities = ["read"] }

# ─────────────────────────────────────────────────────────────────────────────
# Required keys at kv/nomaos/<env>/appsettings
# (flattened to <Section>__<Key> environment variables by the renderer)
#
#   ConnectionStrings:DefaultConnection   SQL Server (RDS) connection string
#   Redis:ConnectionString                ElastiCache endpoint[,password=...]
#   Jwt:Key                               HS256 signing key (>= 32 bytes)
#   Jwt:Issuer                            e.g. https://api.nomaos.com
#   Jwt:Audience                          e.g. nomaos-clients
#   AWS:AccessKey / AWS:SecretKey         S3 + Rekognition (prefer an instance role)
#   AWS:BucketName / AWS:Region
#   SendGrid:ApiKey                       transactional email
#   TermiSMS:livekey / TermiSMS:url       SMS
#   IdentityPass:preembleyurl / :apikey   KYC (Prembly)
#   AesOptions:Key / AesOptions:IV        payload encryption
#   CorsOrigins                           comma-separated origin list
#
# Anything multi-line (PEM material) stays in Vault only: the renderer skips it
# and the in-process Vault provider supplies it at startup.
# ─────────────────────────────────────────────────────────────────────────────
