export type UserRole = 'MANAGER' | 'CHEF' | 'STAFF' | 'CUSTOMER';

export const getDefaultRoute = (role: string): string => {
  const normalizedRole = role.toUpperCase();
  switch (normalizedRole) {
    case 'CHEF':
      return '/chef';
    case 'STAFF':
      return '/tables';
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

  if (normalizedRole === 'CHEF') {
    // Chef can only access kitchen queue and settings
    return cleanPath === 'chef' || cleanPath === 'settings';
  }

  if (normalizedRole === 'STAFF') {
    // Staff can access tables, menu, transactions, and settings
    return ['tables', 'menu', 'transactions', 'settings'].includes(cleanPath);
  }

  if (normalizedRole === 'CUSTOMER') {
    // Customer can only view menu
    return cleanPath === 'menu';
  }

  return false;
};
