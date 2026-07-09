export type UserRole = 'MANAGER' | 'STAFF' | 'CUSTOMER';

export const getDefaultRoute = (role: string): string => {
  const normalizedRole = role.toUpperCase();
  switch (normalizedRole) {
    case 'STAFF':
      return '/staff-dashboard';
    case 'CUSTOMER':
      return '/menu';
    case 'MANAGER':
    default:
      return '/dashboard';
  }
};

export const hasPermission = (role: string, path: string): boolean => {
  const normalizedRole = role.toUpperCase();
  const cleanPath = path.replace(/^\//, ''); // remove leading slash

  if (normalizedRole === 'MANAGER') {
    return true; // Manager can access everything
  }

  if (normalizedRole === 'STAFF') {
    // Staff can only access staff kitchen/orders dashboard and settings
    return ['staff-dashboard', 'settings'].includes(cleanPath);
  }

  if (normalizedRole === 'CUSTOMER') {
    // Customer can only view menu
    return cleanPath === 'menu';
  }

  return false;
};

