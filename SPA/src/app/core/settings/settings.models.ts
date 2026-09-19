export type Theme = 'Light' | 'Dark';

export interface Settings {
  readonly id: number;
  readonly theme: Theme;
}
