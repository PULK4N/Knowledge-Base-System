import { permissionLabel, togglePermission } from './agent-permissions';

describe('togglePermission', () => {
  it.each([
    [[], 'skills.read', true, ['skills.read']],
    [[], 'skills.write', true, ['skills.read', 'skills.write']],
    [[], 'policies.suggest', true, ['policies.suggest']],
    [['skills.read', 'skills.write'], 'skills.write', false, ['skills.read']],
    [['skills.read', 'skills.write'], 'skills.read', false, []],
    [['knowledge.read', 'memory.write'], 'memory.read', true, ['memory.read', 'memory.write', 'knowledge.read']],
    [['memory.read'], 'memory.read', true, ['memory.read']],
  ] as [string[], string, boolean, string[]][])(
    'changes %j by setting %s to %s',
    (selected, permission, checked, expected) => {
      expect(togglePermission(selected, permission, checked)).toEqual(expected);
    },
  );

  it('does not change the selected list it was given', () => {
    const selected = ['skills.read'];

    togglePermission(selected, 'skills.write', true);

    expect(selected).toEqual(['skills.read']);
  });
});

describe('permissionLabel', () => {
  it.each([
    ['memory.write', 'write'],
    ['policies.suggest', 'suggest'],
  ])('labels %s as %s', (permission, label) => {
    expect(permissionLabel(permission)).toBe(label);
  });
});
