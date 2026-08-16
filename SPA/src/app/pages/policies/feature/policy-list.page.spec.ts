import { convertToParamMap } from '@angular/router';
import { policyScopeFromRoute } from './policy-list.page';

describe('policyScopeFromRoute', () => {
  it('creates the general policy scope without route parameters', () => {
    expect(policyScopeFromRoute('general', convertToParamMap({}))).toEqual({
      kind: 'general',
    });
  });

  it('creates topic and project scopes from their route parameters', () => {
    expect(
      policyScopeFromRoute(
        'topic',
        convertToParamMap({ topicName: 'Web Design' }),
      ),
    ).toEqual({ kind: 'topic', topicName: 'Web Design' });
    expect(
      policyScopeFromRoute(
        'project',
        convertToParamMap({ projectId: 'project-1' }),
      ),
    ).toEqual({ kind: 'project', projectId: 'project-1' });
  });

  it('rejects missing or unknown route contexts', () => {
    expect(policyScopeFromRoute('topic', convertToParamMap({}))).toBeNull();
    expect(policyScopeFromRoute('unknown', convertToParamMap({}))).toBeNull();
  });
});
