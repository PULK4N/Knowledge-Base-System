export interface Agent {
  readonly id: string;
  readonly userName: string;
  readonly displayName: string;
  readonly isAdministrator: boolean;
  readonly permissions: readonly string[];
}

export interface AgentPermissions {
  readonly isAdministrator: boolean;
  readonly permissions: readonly string[];
}

export interface AgentDraft extends AgentPermissions {
  readonly userName: string;
  readonly displayName: string;
}
