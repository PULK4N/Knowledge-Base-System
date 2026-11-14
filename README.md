# Knowledge Base System

A self-hosted knowledge base for AI-assisted development. Store reusable skills, scoped policies, projects, feature plans, research records, and conversation memories in one place, then access them through a web interface, REST API, or Model Context Protocol (MCP) tools.

The application combines an Angular frontend, an ASP.NET Core API, an event-sourced backend, and local semantic search powered by Ollama and PostgreSQL with pgvector.

## Contents

- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [MCP and agent integrations](#mcp-and-agent-integrations)
  - [Claude Code plugin](#claude-code-plugin)
  - [Codex plugin](#codex-plugin)
  - [Verify the connection](#verify-the-connection)
- [License](#license)

## Features

- **Skills:** reusable instructions, references, attachments, use scenarios, and project associations.
- **Policies:** rules scoped to general use, projects, topics, or agent families.
- **Projects:** repository mappings and links to relevant skills and policies.
- **Features:** feature information, plans, and linked records such as research and planning conversations.
- **Memory:** captured conversations, summaries, and attachments for later retrieval.
- **Search:** keyword and semantic retrieval across stored knowledge, plus focused search tools for individual content types.
- **Administration:** projection replay, outbox inspection, unique-constraint rebuilding, and code-review settings.
- **Agent integrations:** MCP tools and repository-provided hooks for Claude and Codex.

## Requirements

### Running the Docker stack

| Requirement | Purpose |
| --- | --- |
| Git | Clone this repository and its event-sourcing submodule. |
| Docker with Docker Compose v2 | Build and run the application and infrastructure containers. |
| NVIDIA GPU with a working driver and Docker GPU support | Required by the supplied Ollama service. On Linux, configure NVIDIA Container Toolkit for Docker. |
| Internet access during initial setup | Download container images, build dependencies, the Git submodule, and the embedding model. |
| Available host ports | `4200`, `5231`, `5432`, `5673`, `8162`, `8081`, and `11435`. |
| Persistent storage | Store PostgreSQL data, broker data, container images, and the downloaded model. |

The default Compose configuration **requires CUDA and deliberately refuses CPU fallback**. Its Ollama entrypoint also stops the service if GPU access is lost or a model runs on the CPU. A CPU-only machine or a machine without NVIDIA GPU passthrough cannot run this configuration unchanged.

The commands below use Bash. On Windows, run them in a WSL environment with Docker integration and NVIDIA GPU access configured. Native macOS does not satisfy the supplied stack's NVIDIA requirement.

The examples use `docker compose`. If Compose v2 is installed as the standalone `docker-compose` executable, replace `docker compose` with `docker-compose` throughout the commands. The rebuild scripts support either form.

## Installation

Run the following from the directory where you want the repository checkout. This creates or reuses the checkout and persistent volumes, initializes the required submodule over HTTPS, and starts the full stack.

```bash
set -euo pipefail

if [ ! -d Knowledge-base-system ]; then
    git clone https://github.com/PULK4N/MCP-Skill-System.git Knowledge-base-system
fi
cd Knowledge-base-system

# The committed submodule URL uses SSH. Use HTTPS for this checkout so
# initialization does not require an SSH key (repository access is still required).
git submodule init
git config submodule.API/EventSourcing.url https://github.com/PULK4N/EventSourcing.git
git submodule update --init --recursive

docker compose version

# These volumes are declared external and are not created by Compose.
# Creating an existing volume reuses it without clearing its contents.
docker volume create mcp-skill-system_skill-memory-postgres-data
docker volume create mcp-skill-system_skill-memory-ollama-data

docker compose up --detach --build
docker compose ps --all
```

The first startup downloads the embedding model. The API waits for PostgreSQL and Artemis health checks and for `ollama-model-init` to finish successfully. The API then applies its database migrations automatically. No separate migration command is needed for normal startup.

`docker compose up --detach` returning does not guarantee that the application is ready. Check the container status and logs if the UI is not yet available. The model-init container exiting with code `0` is expected.

Open **[http://localhost:4200](http://localhost:4200)** for the web interface and **[http://localhost:5231/swagger](http://localhost:5231/swagger)** for REST API documentation.

The supplied Compose file is a local development configuration: it uses default database and broker credentials, exposes infrastructure ports, and runs the API in Development mode. The API startup does not configure authentication. Add access control, TLS, appropriate network restrictions, and deployment-specific credentials before exposing it beyond a trusted environment.

## MCP and agent integrations

Configure an MCP-compatible client to connect to `http://localhost:5231/mcp` using Streamable HTTP. Client configuration syntax varies; this is a server endpoint, not a browser page. A client running in another container or on another machine needs an address that reaches the API from that environment.

The MCP server exposes skills, policies, projects, features, memories, and knowledge-search tools. Focused tools such as `skill_search` and `feature_record_search` retrieve particular content types, while `knowledge_search` searches across sources.

### Before installing a plugin

Start the Knowledge Base stack using the installation steps above. Install the client you use, and make sure **Python 3 is available as `python`** in that client's environment: the plugin hooks invoke that exact command. Git must also be available. No additional Python packages are required by the hooks.

The plugins include the MCP connection and hooks for loading policies and saving conversation memory. **The policy hooks replace the target repository's `CLAUDE.md` (Claude) or `AGENTS.md` (Codex) with policies from the Knowledge Base.** Preserve any existing instructions you need before enabling the plugin.

### Claude Code plugin

Run this in a terminal from the root of this repository checkout, with Claude Code installed and signed in:

```bash
set -euo pipefail
python --version
claude plugin marketplace add .
claude plugin install mcp-knowledge-base@knowledge-base-claude --scope user
claude plugin list
```

This registers [the Claude marketplace](.claude-plugin/marketplace.json) and installs the plugin for your user. If `knowledge-base-claude` is already registered for this checkout, skip the marketplace-add command.

Restart Claude Code in the repository you want to work on. Complete any plugin or hook trust prompts, then run `/mcp` to check that the plugin's Knowledge Base server is connected. See [Claude Code's plugin installation guide](https://code.claude.com/docs/en/discover-plugins) for installation scopes and plugin management.

### Codex plugin

Use a Codex CLI version that supports `codex plugin`. Run this in a terminal from the root of this repository checkout, with Codex installed and signed in:

```bash
set -euo pipefail
python --version
codex plugin marketplace add .
codex plugin add mcp-knowledge-base@knowledge-base-codex
codex plugin list --marketplace knowledge-base-codex
```

This registers [the Codex marketplace](.agents/plugins/marketplace.json) and installs its Codex-specific plugin. If `knowledge-base-codex` is already registered for this checkout, skip the marketplace-add command.

Restart your Codex client and start a new session in the repository you want to work on. Complete any plugin or hook trust prompts. In the ChatGPT desktop app's Codex interface, local marketplaces can also be selected in the Plugins Directory; choose **Knowledge Base Codex** and install **MCP Knowledge Base**. See [OpenAI's local plugin setup guide](https://developers.openai.com/plugins/build/plugins) for marketplace and client details.

### Verify the connection

Ask your agent: **“Use the Knowledge Base MCP to list the available skills.”** It should call a Knowledge Base tool and return a result; an empty list is valid for a new installation.

For project-specific policies and skills, create a project in the web interface, add the repository path used by your agent, and link the relevant policies and skills. Start a new agent session in that repository to load them.

If setup fails:

- **Python not found:** ensure `python` resolves to Python 3 in the environment that launches the client, including desktop or IDE clients.
- **Connection refused:** check that the API is running at `http://localhost:5231`. `localhost` refers to the agent's environment, which matters when using containers, WSL, or a remote host.
- **Different API address:** change the URL in the relevant plugin's `.mcp.json` before installing it. The hooks read that configuration too; reinstall or refresh the installed plugin after changing its source files.
- **Tools work but policies or memory do not:** check that the full plugin is enabled and its hooks are trusted. Adding only an MCP server does not install the hooks.

### Plugin sources

Agent plugin sources live in:

- [`integrations/claude/plugins/mcp-knowledge-base/`](integrations/claude/plugins/mcp-knowledge-base/)
- [`integrations/codex/plugins/mcp-knowledge-base/`](integrations/codex/plugins/mcp-knowledge-base/)

Both include MCP configuration pointing to the local API. Hook integrations provide additional agent-specific behavior, including conversation capture. See the [Codex memory attachment notes](integrations/codex/plugins/mcp-knowledge-base/README.md) and [prompt usage tooling](scripts/PROMPT_USAGE.md) for the corresponding repository tools.

## License

This repository is licensed under the [MIT License](LICENSE).
