import { Pipe, PipeTransform } from '@angular/core';
import { Feature, ItemOrigin } from '../data-access/feature.models';
import { OriginLink, originLink } from './specification-origin';

/** Turns a feature copy's origin into the link to the original plan item. */
@Pipe({ name: 'originLink' })
export class OriginLinkPipe implements PipeTransform {
  transform(origin: ItemOrigin | null | undefined, feature: Feature | null): OriginLink | null {
    return feature ? originLink(feature, origin) : null;
  }
}
