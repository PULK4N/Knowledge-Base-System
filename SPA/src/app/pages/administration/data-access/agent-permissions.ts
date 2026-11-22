export interface PermissionArea {
  readonly name: string;
  readonly permissions: readonly string[];
}

/** The permissions the API accepts, grouped by area for the checkboxes. */
export const PERMISSION_AREAS: readonly PermissionArea[] = [
  { name: 'Memory', permissions: ['memory.read', 'memory.write'] },
  { name: 'Skills', permissions: ['skills.read', 'skills.write'] },
  { name: 'Features', permissions: ['features.read', 'features.write'] },
  { name: 'Policies', permissions: ['policies.read', 'policies.suggest', 'policies.write'] },
  { name: 'Projects', permissions: ['projects.read', 'projects.write'] },
  { name: 'Attachments', permissions: ['attachments.read', 'attachments.write'] },
  { name: 'Knowledge search', permissions: ['knowledge.read'] },
];

const allPermissions = PERMISSION_AREAS.flatMap(area => area.permissions);

function areaOf(permission: string): string {
  return permission.slice(0, permission.lastIndexOf('.'));
}

/**
 * Ticks or unticks one permission. Write includes read, so ticking a write
 * permission ticks its read permission, and unticking read unticks write.
 * The result keeps the order of PERMISSION_AREAS.
 */
export function togglePermission(
  selected: readonly string[],
  permission: string,
  checked: boolean,
): string[] {
  const next = new Set(selected);
  const area = areaOf(permission);

  if (checked) {
    next.add(permission);
    if (permission.endsWith('.write')) next.add(`${area}.read`);
  } else {
    next.delete(permission);
    if (permission.endsWith('.read')) next.delete(`${area}.write`);
  }

  return allPermissions.filter(value => next.has(value));
}

/** The short label shown next to a checkbox, such as "write". */
export function permissionLabel(permission: string): string {
  return permission.slice(permission.lastIndexOf('.') + 1);
}
