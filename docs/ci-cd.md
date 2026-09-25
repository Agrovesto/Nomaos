# CI/CD — `dotnet-api` pipeline

> **Install step required.** The pipeline lives at
> [`deploy/ci/dotnet-api.yml`](../deploy/ci/dotnet-api.yml) and a ready-to-use
> copy is in the working tree at `.github/workflows/dotnet-api.yml`, but it is
> **not committed** there: this branch was pushed by a GitHub App whose token
> does not carry the `workflows` permission, and GitHub rejects any push that
> creates or edits a file under `.github/workflows/`. A maintainer installs it
> with one command:
>
> ```bash
> cp deploy/ci/dotnet-api.yml .github/workflows/dotnet-api.yml
> git add .github/workflows/dotnet-api.yml && git commit -m "Install dotnet-api workflow"
> ```
>
> (`.github/workflows/dotnet-api.yml` is listed in `.gitignore` for exactly this
> reason — remove that entry when you install it.)

## Triggers

| Event | What runs |
|---|---|
| PR → `dev` / `main` | metadata, tests + coverage gate, SonarQube, Checkov + Trivy, image build + smoke test |
| Push → `dev` | everything above, then ECR push and deploy to the **dev** host |
| Push → `main` | everything above, then ECR push and deploy to the **prod** host |
| `workflow_dispatch` | the gates; deploys only when the `deploy` input is `true` |

`concurrency` is keyed on the ref, so two deploys never race for the same host;
PR runs cancel their own superseded runs.

## Jobs

1. **Git metadata** — computes the build identity once: full and short SHA
   (the PR *head* SHA, not the ephemeral merge commit), branch, sanitised branch
   slug, PR number, author, run URL, UTC timestamp, the target environment and
   the tag list `sha-<short>`, `branch-<slug>`, `build-<run_number>` plus the
   floating `dev`/`prod`/`latest` on deployable pushes.
2. **Tests & coverage gate** — `./NomaOS.Test/run-tests-with-coverage.sh`.
   Fails on a test failure or on <95% line/branch. Uploads the coverage
   directory and the `.trx`.
3. **SonarQube** — `sonarsource/sonarqube-scan-action`, fed the OpenCover report
   from the test artifact. Skipped with a warning when `SONAR_HOST_URL` /
   `SONAR_TOKEN` are not configured.
4. **Checkov & Trivy (filesystem)** — Checkov over
   `Agrovesto.Backend.API/Dockerfile` (SARIF uploaded to code scanning), Trivy
   `fs` at HIGH,CRITICAL with `exit-code: 1`.
5. **Image build & smoke test** — buildx builds `linux/amd64` and loads it
   locally, Trivy scans the image (HIGH,CRITICAL), then the container is run and
   `/health/live` is polled 30×2s. The assertion is on the body
   (`"status":"Healthy"`), not merely on a 200. Container logs and the Docker
   health log are dumped on failure. **No image reaches ECR before this passes.**
6. **Push to ECR** — OIDC via `aws-actions/configure-aws-credentials`, then
   `amazon-ecr-login` and a buildx push of every tag for `linux/amd64`
   (EC2's architecture; arm64 needs no QEMU but is not pushed).
7. **Deploy to EC2** — rsyncs `deploy/ec2/` to the host and runs
   `./deploy.sh --tag sha-<short>` over **SSH**, or over **SSM** when no SSH host
   is configured. `deploy.sh` owns the health gate and the rollback to
   `PREVIOUS_IMAGE_TAG`; the workflow adds a belt-and-braces `rollback.sh` call
   if the channel itself dies mid-deploy.

There is no ECS API call anywhere in the pipeline.

## Secrets and variables

| Name | Kind | Purpose |
|---|---|---|
| `AWS_ROLE_ARN_DEV` / `AWS_ROLE_ARN_PROD` | secret | OIDC role assumed per environment |
| `AWS_REGION` | var | default `eu-west-1` |
| `ECR_REPOSITORY` | var | default `nomaos` |
| `EC2_SSH_HOST`, `EC2_SSH_USER`, `EC2_SSH_PRIVATE_KEY` | secret | SSH deploy channel |
| `EC2_INSTANCE_ID` | var | SSM deploy channel (used when no SSH host is set) |
| `COMPOSE_PATH`, `COMPOSE_SERVICE` | var | default `/opt/nomaos`, `api` |
| `SONAR_HOST_URL` | var | SonarQube server |
| `SONAR_TOKEN` | secret | SonarQube token |
| `VAULT_ADDR`, `VAULT_ROLE_ID`, `VAULT_SECRET_ID` | secret *(optional)* | only forwarded when the host has no `/opt/nomaos/vault-approle`; host-side credentials are the preferred model |

Both GitHub Environments (`dev`, `prod`) should exist so required reviewers can
gate the production deploy.

## Local equivalents

```bash
./NomaOS.Test/run-tests-with-coverage.sh                        # job 2
docker build -f Agrovesto.Backend.API/Dockerfile -t nomaos-api:dev .   # job 5
sudo /opt/nomaos/deploy.sh --tag sha-1a2b3c4                    # job 7
```
