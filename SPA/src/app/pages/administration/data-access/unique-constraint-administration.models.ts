export interface UniqueConstraintConflict {
  readonly constraintName: string;
  readonly aggregateIds: readonly string[];
}

export interface UniqueConstraintRebuildResult {
  readonly status: 'Completed' | 'Conflicts';
  readonly processedAggregateCount: number;
  readonly constraintCount: number;
  readonly conflicts: readonly UniqueConstraintConflict[];
}
