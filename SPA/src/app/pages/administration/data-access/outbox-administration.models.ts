import { PagedResult } from '../../../core/store/entity-store.service';
import { ListSortDirection } from '../../../shared/list-state/list-state';

export interface OutboxPayloadDto {
  readonly id: number;
  readonly state: string;
  readonly retryCount: number;
  readonly errorMessage: string | null;
  readonly stateMachineId: string;
  readonly aggregateId: string;
  readonly orderNumber: number;
  readonly eventName: string;
  readonly timestamp: string;
  readonly executionInfoJson: string;
  readonly eventDataJson: string;
}

export interface OutboxPayload {
  readonly id: string;
  readonly payloadId: number;
  readonly state: string;
  readonly retryCount: number;
  readonly errorMessage: string | null;
  readonly stateMachineId: string;
  readonly aggregateId: string;
  readonly orderNumber: number;
  readonly eventName: string;
  readonly timestamp: string;
  readonly executionInfoJson: string;
  readonly eventDataJson: string;
}

export type OutboxPayloadSortField =
  | 'Id'
  | 'State'
  | 'RetryCount'
  | 'AggregateId';

export interface OutboxPayloadSearchRequest {
  readonly page: number;
  readonly pageSize: number;
  readonly search: string;
  readonly onlyIncomplete: boolean;
  readonly state: string;
  readonly aggregateId: string;
  readonly sortBy: OutboxPayloadSortField;
  readonly sortDirection: ListSortDirection;
}

export type OutboxPayloadSearchResult = PagedResult<OutboxPayload>;
