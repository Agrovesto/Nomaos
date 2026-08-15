# ─────────────────────────────────────────────────────────────────────────────
# NomaOS deployment shortcuts. Run from the deploy/ directory on the EC2 host.
#   make help
# ─────────────────────────────────────────────────────────────────────────────
SHELL           := /bin/bash
STATE_DIR       ?= /opt/nomaos
COMPOSE_DIR     := compose
INFRA_ENV       := $(STATE_DIR)/env/infra.env
APP             ?= nomaos-api
APP_ENV         := $(STATE_DIR)/env/$(APP).env
INFRA           := docker compose -p nomaos-infra --env-file $(INFRA_ENV) -f $(COMPOSE_DIR)/docker-compose.infra.yml

.DEFAULT_GOAL := help
.PHONY: help bootstrap infra-up infra-down infra-logs infra-ps deploy rollback status migrate logs shell-db shell-redis backup-db validate

help: ## Show this help
	@grep -hE '^[a-zA-Z_-]+:.*?## ' $(MAKEFILE_LIST) \
	  | awk 'BEGIN{FS=":.*?## "}{printf "  \033[36m%-16s\033[0m %s\n", $$1, $$2}'

bootstrap: ## Install Docker + prepare /opt/nomaos (run once, as root)
	sudo ./scripts/bootstrap.sh

infra-up: ## Start nginx + SQL Server + Redis
	$(INFRA) up -d

infra-down: ## Stop infrastructure (volumes are preserved)
	$(INFRA) down

infra-logs: ## Tail infrastructure logs
	$(INFRA) logs -f --tail=100

infra-ps: ## Infrastructure container status
	$(INFRA) ps

deploy: ## Blue/green deploy. Usage: make deploy APP=nomaos-api IMAGE=<ref>:<tag>
	@test -n "$(IMAGE)" || { echo "IMAGE=<ecr-ref>:<tag> is required"; exit 2; }
	sudo ./scripts/deploy.sh --app $(APP) --image $(IMAGE) --commit $(or $(COMMIT),unknown)

rollback: ## Flip back to the previous slot. Usage: make rollback APP=nomaos-api
	sudo ./scripts/rollback.sh --app $(APP) $(if $(IMAGE),--image $(IMAGE),)

status: ## What is running right now
	./scripts/status.sh

migrate: ## Apply EF Core migrations against the containerised DB
	docker compose -p nomaos-migrate --env-file $(APP_ENV) \
	  -f $(COMPOSE_DIR)/docker-compose.migrate.yml run --rm migrate

logs: ## Tail the active slot. Usage: make logs APP=nomaos-api
	@slot=$$(cat $(STATE_DIR)/state/$(APP).active-slot 2>/dev/null || echo blue); \
	 echo "tailing $(APP)-$$slot"; docker logs -f --tail=100 $(APP)-$$slot

shell-db: ## sqlcmd shell into the SQL Server container
	@set -a; . $(INFRA_ENV); set +a; \
	 docker exec -it nomaos-mssql /opt/mssql-tools18/bin/sqlcmd \
	   -S localhost -U sa -P "$$MSSQL_SA_PASSWORD" -C

shell-redis: ## redis-cli into the Redis container
	docker exec -it nomaos-redis redis-cli

backup-db: ## Ad-hoc backup of the containerised database
	@set -a; . $(INFRA_ENV); set +a; \
	 ts=$$(date -u +%Y%m%d-%H%M%S); \
	 docker exec nomaos-mssql /opt/mssql-tools18/bin/sqlcmd \
	   -S localhost -U sa -P "$$MSSQL_SA_PASSWORD" -C -Q \
	   "BACKUP DATABASE [$(DB_NAME:-nomaosdb)] TO DISK=N'/var/opt/mssql/backup/adhoc-$$ts.bak' WITH COPY_ONLY, INIT, CHECKSUM, COMPRESSION, STATS=10;" \
	 && echo "backup written inside the container: /var/opt/mssql/backup/adhoc-$$ts.bak"

validate: ## Validate compose files and shell scripts
	@echo "== compose config =="
	@$(INFRA) config -q && echo "  infra OK"
	@IMAGE=x:1 APP_NAME=nomaos-api SLOT=blue VAULT_ADDR=x VAULT_ROLE_ID=x \
	 VAULT_SECRET_ID=x DB_CONNECTION_STRING=x \
	 docker compose -f $(COMPOSE_DIR)/docker-compose.app.yml config -q && echo "  app OK"
	@echo "== bash syntax =="
	@for f in scripts/*.sh; do bash -n "$$f" && echo "  $$f OK"; done
