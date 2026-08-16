# Knowledge Base System Frontend Handoff

This file is the source of truth for continuing the Angular frontend in a new Codex session.

## Repository and current state

- Live repository: `/home/nikola/Documents/github/Knowledge-base-system`
- The former path `/home/nikola/Documents/github/MCP-Skill-System` is stale and should not be used.
- Frontend location: `SPA/`
- `SPA/` was empty when implementation started.
- A preliminary `SPA/package.json` is currently present and untracked. It is only a partial scaffold; inspect and correct it before continuing.
- No Angular application files, REST client, Dockerfile, nginx configuration, or Compose frontend service have been completed yet.
- The unrelated untracked file `persinalization.md` belongs to the user and must not be changed.

## Product name and approved navigation

The product is called **Knowledge Base System**. Do not call it MCP Skills System or MCP-Skills-System in the UI.

The left navigation contains exactly:

1. Skills
2. Policies
3. Topics
4. Projects
5. Memories

Do not add Overview, Event History, Status, MCP Status, System Health, Hook Health, or Local Repository sections.

The home page is neutral: none of the five navigation items is selected while the user is on `/`.

## Critical transport decision

The browser frontend communicates **only with the existing REST controllers**.

- Use Angular `HttpClient` with same-origin `/api/...` URLs.
- Do not implement an MCP client.
- Do not initialize MCP sessions or call MCP tools.
- Do not proxy `/mcp` from the frontend container.
- Do not show MCP connection or health status in the UI.
- The production nginx container should proxy `/api` to `http://api:8080` on the Docker Compose network.

## Approved screens

The three approved reference images are in `SPA/design/images/`:

- `home-page.png`
- `skills-list-page.png`
- `skill-details-page.png`

### Home page

- Dark navy left sidebar and a warm off-white workspace.
- Product mark and `Knowledge Base System` label in the sidebar.
- No navigation item selected.
- Simple overview content with useful Skills, Policies, and Memories widgets.
- No event history, repository details, health information, or operational status.

### Skills list page

- Skills is selected in the sidebar.
- Header contains `Skills` and a short explanatory subtitle.
- Search input and total skill count.
- Table/list columns are Skill and Skill ID.
- Each row navigates to its own details route.
- Use numbered, directly selectable pagination, including previous and next controls.
- List and details must not be combined into one split-pane page.
- Do not add Add Skill or Import Skill actions.

### Skill details page

- This is a standalone route, separate from the list.
- Include a `Back to skills` link.
- Show skill name, ID, description, and tags.
- Use tabs for Content, References, and Attachments, with counts where available.
- Render server Markdown safely. Prefer a text-to-block renderer or a sanitizing Markdown library; never use a generic unsafe `innerHTML` pipe.
- The reference image includes an Edit button, but the current REST controller has no update endpoint. Keep the implemented page read-only unless a REST update endpoint is added. Do not fall back to MCP for editing.

## Current REST contract

Controller: `API/DomainsModules/SkillsModule/API/Controllers/SkillsController.cs`

### Search skills

`GET /api/skills?page={page}&pageSize={pageSize}&search={optionalSearch}`

Response shape:

```ts
interface PagedResult<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
  readonly totalPages: number;
  readonly hasPreviousPage: boolean;
  readonly hasNextPage: boolean;
}

interface SkillSummaryDto {
  readonly skillId: string;
  readonly name: string;
}
```

Server pagination is one-based. The backend default page size is 25 and the maximum page size is 100. The approved mockup visually used approximately five rows per page, so a small explicit page size is appropriate for this screen.

### Get one skill

`GET /api/skills/{skillId}`

Optional historical query parameter: `orderNumber`. Omit it for the latest state.

Response shape:

```ts
interface SkillDto {
  readonly id: string;
  readonly isDeleted: boolean;
  readonly name: string;
  readonly description: string;
  readonly content: string;
  readonly tags: readonly string[];
  readonly references: Readonly<Record<string, { readonly content: string }>>;
  readonly attachments: Readonly<Record<string, {
    readonly id: string;
    readonly name: string;
    readonly size: number;
    readonly fileType: string;
    readonly extension: string;
  }>>;
}
```

The current controller also exposes create, attachment upload, and reference creation endpoints. They are not required by the approved UI. It currently exposes no REST update or delete endpoint.

## Angular implementation direction

Use the installed `angular-code-writter` skill before implementing.

- A registry check on 2026-08-16 found Angular core `22.1.2` and Angular CLI `22.1.4` as the current stable releases.
- Angular 22.1 requires TypeScript `>=6.0 <6.1`.
- The current local Node version is `24.10.0`, while Angular CLI 22.1.4 requires Node `^22.22.3`, `^24.15.0`, or `>=26`. Use a compatible Node image for Docker and a compatible local Node version for CLI verification.
- Prefer a current standalone Angular application with lazy feature routes and built-in template control flow.
- Route/page components are UI controllers.
- Use OnPush change detection.
- Keep data access in a typed `SkillService` that owns `/api/skills` paths and response mapping.
- Use a normalized observable Entity Store for skill summaries/details and cached searches.
- Successful list and detail reads must update the same store.
- Keep writable subjects private and expose read-only observables.
- Compose loading, success, empty, and error states explicitly.
- Bind streams with `AsyncPipe`; do not manually subscribe in components.
- Search and pagination changes should use replaceable reads (`switchMap`).
- Keep reusable rendering components presentational and emit user intent.
- Exclude Formly, form-generator, and smart-engine code.

Suggested feature layout:

```text
SPA/src/app/
  core/store/entity-store.service.ts
  layout/app-shell.component.*
  pages/home/home.page.*
  pages/skills/
    data-access/skill.models.ts
    data-access/skill.service.ts
    feature/skills-list.page.*
    feature/skill-details.page.*
    skills.routes.ts
```

Only implement the approved home and Skills screens in this slice. Keep Policies, Topics, Projects, and Memories visible in navigation, but do not invent their page designs before they are requested.

## Visual language

- Canvas reference: approximately 1536 by 1024.
- Sidebar width: approximately 250px.
- Sidebar: deep navy with subtle indigo accent.
- Workspace: warm off-white.
- Cards: white, subtle gray border, soft shadow, 12-18px radius.
- Accents: indigo, teal, and restrained amber.
- Typography: clean system sans-serif, strong compact headings, muted secondary copy.
- Responsive behavior: desktop sidebar becomes a compact top navigation on smaller screens; tables must remain usable without clipping important content.
- Use inline SVG or CSS icons to avoid a heavy icon dependency.

## Docker Compose integration

Current Compose file: `docker-compose.yml`.

- Existing API service name: `api`.
- API listens on port `8080` inside Compose and is currently published as host port `5231`.
- Add a frontend service built from `SPA/Dockerfile`.
- Use a multi-stage build with a Node version compatible with Angular 22.1, then serve the production output with nginx.
- nginx must serve Angular route fallbacks with `try_files ... /index.html`.
- nginx must proxy `/api/` to `http://api:8080/api/`.
- The browser should use same-origin URLs; do not add CORS as a workaround.
- Add `depends_on: api` for startup ordering, but do not invent health checks.

## Verification expected

1. Install dependencies and commit the generated lockfile.
2. Run focused unit tests for store normalization/cache synchronization and pure content transformations.
3. Run the Angular production build.
4. Search changed components for manual `.subscribe(` calls.
5. Validate `docker compose config` after adding the frontend service.
6. If feasible, build the frontend image with the compatible Node image.
7. Report exactly which checks passed and which were not run.

## User working preference

Work step by step. Complete and verify the requested frontend slice before designing or implementing Policies, Topics, Projects, or Memories pages.
