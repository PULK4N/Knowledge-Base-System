import { FeatureRecord } from '../data-access/feature.models';
import { FeatureLinkOption } from './feature-link-picker.component';

export function toRecordOptions(
  records: readonly FeatureRecord[],
): readonly FeatureLinkOption[] {
  return records.map(record => ({
    id: record.id,
    label: record.title,
    detail: record.type,
  }));
}
