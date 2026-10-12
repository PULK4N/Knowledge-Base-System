import { Pipe, PipeTransform } from '@angular/core';
import { FeaturePlanStatus } from '../data-access/feature.models';

export const FEATURE_PLAN_STATUS_OPTIONS: readonly {
  readonly value: FeaturePlanStatus;
  readonly label: string;
}[] = [
  { value: 'NotStarted', label: 'Not started' },
  { value: 'InProgress', label: 'In progress' },
  { value: 'Delayed', label: 'Delayed' },
  { value: 'Completed', label: 'Completed' },
];

export function featurePlanStatusLabel(status: FeaturePlanStatus): string {
  return (
    FEATURE_PLAN_STATUS_OPTIONS.find(option => option.value === status)
      ?.label ?? status
  );
}

@Pipe({ name: 'featurePlanStatus' })
export class FeaturePlanStatusPipe implements PipeTransform {
  transform(status: FeaturePlanStatus): string {
    return featurePlanStatusLabel(status);
  }
}
