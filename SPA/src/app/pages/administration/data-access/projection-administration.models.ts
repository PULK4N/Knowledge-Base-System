export interface ProjectionGroup {
  readonly stateMachineId: string;
  readonly projectionNames: readonly string[];
}

export interface ProjectionReplayQueuedResult {
  readonly status: string;
  readonly queuedAggregateCount: number;
}

export type ProjectionRunScope = 'aggregate' | 'stateMachine';

export interface RunProjectionRequest {
  readonly projectionName: string;
  readonly aggregateId?: string;
  readonly stateMachineId?: string;
}

export interface ProjectionRunResult {
  readonly status: string;
  readonly processedAggregateCount: number;
}
