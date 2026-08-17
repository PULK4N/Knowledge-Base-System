export interface ProjectionGroup {
  readonly stateMachineId: string;
  readonly projectionNames: readonly string[];
}

export interface ProjectionReplayQueuedResult {
  readonly status: string;
  readonly queuedAggregateCount: number;
}
