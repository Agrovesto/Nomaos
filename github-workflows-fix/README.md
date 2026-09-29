# GitHub workflow fixes — manual install

These are the fixed workflow files from the deployment-pipeline review. They
live **outside** `.github/` because the GitHub App used to author this branch
does not hold the `workflows` permission, so GitHub rejects any push that
touches `.github/workflows/*`.

## Install (after cloning)

```bash
# 1. copy the fixed workflows over the installed ones (merges into .github/workflows/)
cp -r github-workflows-fix/workflows .github/

# 2. prod.yml is 100% commented out (null YAML) — GitHub reports a
#    "workflow file issue" failure on every push until it is removed
rm .github/workflows/prod.yml

# 3. review and push (requires the `workflows` permission on your account)
git diff .github/workflows
git add .github/workflows
git commit -m "Apply deployment workflow fixes"
git push
```

## What each file fixes

### `workflows/docker-deploy.yml` — fixes the failing "Blue/green deploy to EC2" runs

- **jq operator-precedence bug (root cause):** `| @sh` bound to the *entire*
  concatenated remote script instead of only the `bash -lc` payload, so the SSM
  command arrived on the host as one single-quoted shell word and failed with
  exit 127 `command not found`. The pipe now sits inside the payload
  parentheses. Verified end-to-end: the generated command parses (`sh -n`) and
  executes all the way to the payload.
- ECR lifecycle keep-last **3 → 15** images, so blue/green rollback images
  survive several pushes.
- `aws-actions/configure-aws-credentials` **v4 → v6** (Node 20 EOL).
- A comment block documenting the jq precedence trap so it cannot be
  reintroduced.

### `workflows/dev.yml` — honest health-gate results

- The deploy health check no longer has `continue-on-error: true`: a deploy
  whose `/health` never returns 200 now **fails the job** instead of showing
  green.
- Removed dead `quality-gate` job output / summary rows that referenced a
  commented-out step (the summary used to render an empty Quality Gate row).

### `prod.yml` — delete it

Entirely commented out → invalid workflow → red "workflow file issue" run on
every push. The Docker workflow (`docker-deploy.yml`) is the production
pipeline now; `git rm .github/workflows/prod.yml`.

---

Companion changes that did **not** need this workaround (already on the
branch): restored `deploy/` tree incl. `deploy/scripts/deploy.sh`, health-gate
restored in `deploy-prod.sh`/`deploy-staging.sh`, removed junk
`.Backend.API/`, removed stale root `docker-deploy.yml` duplicate.
