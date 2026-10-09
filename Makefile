PROJECT := src/MouseJiggler.Console
ARGS ?=

.DEFAULT_GOAL := help
.PHONY: help run run-dev demo build test clean

help: ## List available targets
	@grep -E '^[a-zA-Z_-]+:.*## ' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*## "} {printf "  make %-10s %s\n", $$1, $$2}'

run: ## Run with appsettings.json defaults (Ctrl+C to stop). Extra options: make run ARGS="--interval 30"
	dotnet run --project $(PROJECT) -- $(ARGS)

run-dev: ## Same as run (alias)
	@$(MAKE) --no-print-directory run ARGS="$(ARGS)"

demo: ## Short bounded run: every 2 s for 7 s
	dotnet run --project $(PROJECT) -- --interval 2 --duration 7

build: ## Build the solution (warnings are errors)
	dotnet build

test: ## Run all tests
	dotnet test

clean: ## Remove build outputs
	dotnet clean
