import { ParamMap, Params } from '@angular/router';

export function readPositiveInteger(
  params: ParamMap,
  key: string,
  fallback: number,
): number {
  const value = Number(params.get(key));

  return Number.isSafeInteger(value) && value > 0 ? value : fallback;
}

export function readAllowedInteger(
  params: ParamMap,
  key: string,
  allowed: readonly number[],
  fallback: number,
): number {
  const value = readPositiveInteger(params, key, fallback);

  return allowed.includes(value) ? value : fallback;
}

export function readAllowedValue<T extends string>(
  params: ParamMap,
  key: string,
  allowed: readonly T[],
  fallback: T,
): T {
  const value = params.get(key);

  return allowed.find(candidate => candidate === value) ?? fallback;
}

export function omitDefault<T extends string | number>(
  value: T,
  fallback: T,
): T | null {
  return value === fallback ? null : value;
}

export function omitEmpty(value: string): string | null {
  const normalized = value.trim();

  return normalized || null;
}

export type ListQueryParams = Params;
