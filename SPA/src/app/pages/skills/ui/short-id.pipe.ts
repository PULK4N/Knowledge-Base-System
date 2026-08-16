import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'shortId',
  standalone: true,
})
export class ShortIdPipe implements PipeTransform {
  transform(value: string, edgeLength = 6): string {
    if (value.length <= edgeLength * 2 + 1) {
      return value;
    }

    return `${value.slice(0, edgeLength)}…${value.slice(-edgeLength)}`;
  }
}
