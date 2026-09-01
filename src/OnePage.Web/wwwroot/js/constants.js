import { getStoredUser } from './utils.js';

// ===== Constants =====
const ALL_MODULES = [
  { key: 'assets', label: 'Assets', icon: '📦', path: 'assets', endpoint: '/assets', summary: 'status', category: 'Assets Management',
    createFields: [
      { name: 'id', label: 'ID', type: 'text', required: true, placeholder: 'e.g. ASSET-001' },
      { name: 'tag', label: 'Tag', type: 'text', required: true, placeholder: 'e.g. LT-2024' },
      { name: 'name', label: 'Name', type: 'text', required: true, placeholder: 'e.g. Laptop' },
      { name: 'description', label: 'Description', type: 'text', required: false, placeholder: 'Optional' },
      { name: 'locationId', label: 'Location', type: 'text', required: false, placeholder: 'Optional' },
      { name: 'departmentId', label: 'Department', type: 'text', required: false, placeholder: 'Optional' }
    ] },
  { key: 'inventory', label: 'Inventory', icon: '🏭', path: 'inventory', endpoint: '/inventory/items', summary: 'sku', category: 'Assets Management',
    createFields: [
      { name: 'id', label: 'ID', type: 'text', required: true, placeholder: 'e.g. INV-001' },
      { name: 'sku', label: 'SKU', type: 'text', required: true, placeholder: 'e.g. SKU-001' },
      { name: 'name', label: 'Name', type: 'text', required: true, placeholder: 'e.g. Widget' },
      { name: 'quantity', label: 'Quantity', type: 'number', required: true, placeholder: 'e.g. 100' }
    ] },
  { key: 'purchaseOrders', label: 'Purchase Orders', icon: '🛒', path: 'purchase-orders', endpoint: '/procurement/purchase-orders', summary: 'status', category: 'Procurement',
    createFields: [
      { name: 'id', label: 'ID', type: 'text', required: true, placeholder: 'e.g. PO-001' },
      { name: 'supplier', label: 'Supplier', type: 'text', required: true, placeholder: 'e.g. Acme Corp' },
      { name: 'totalAmount', label: 'Total Amount', type: 'number', required: true, placeholder: 'e.g. 1500.00' }
    ] },
  { key: 'approvals', label: 'Approvals', icon: '✅', path: 'approvals', endpoint: '/approvals', summary: 'status', category: 'Workflow Management' },
  { key: 'workflowSetup', label: 'Workflow Setup', icon: '⚙️', path: 'workflow-setup', adminOnly: true, category: 'Workflow Management' },
  { key: 'employees', label: 'Employees', icon: '👥', path: 'employees', endpoint: '/hr/employees', summary: 'department', category: 'HR Management',
    createFields: [
      { name: 'id', label: 'ID', type: 'text', required: true, placeholder: 'e.g. EMP-001' },
      { name: 'firstName', label: 'First Name', type: 'text', required: true, placeholder: 'e.g. Jane' },
      { name: 'lastName', label: 'Last Name', type: 'text', required: true, placeholder: 'e.g. Smith' },
      { name: 'email', label: 'Email', type: 'text', required: true, placeholder: 'e.g. jane@company.com' },
      { name: 'position', label: 'Position', type: 'text', required: false, placeholder: 'Optional' },
      { name: 'salary', label: 'Salary', type: 'number', required: false, placeholder: 'Optional' }
    ] },
  { key: 'payroll', label: 'Payroll', icon: '💰', path: 'payroll', endpoint: '/payroll/records', summary: 'status', category: 'Finance',
    createFields: [
      { name: 'id', label: 'ID', type: 'text', required: true, placeholder: 'e.g. PAY-001' },
      { name: 'employeeId', label: 'Employee ID', type: 'text', required: true, placeholder: 'e.g. EMP-001' },
      { name: 'amount', label: 'Amount', type: 'number', required: true, placeholder: 'e.g. 5000.00' },
      { name: 'currency', label: 'Currency', type: 'text', required: true, placeholder: 'e.g. USD' },
      { name: 'periodStart', label: 'Period Start', type: 'date', required: true },
      { name: 'periodEnd', label: 'Period End', type: 'date', required: true },
      { name: 'description', label: 'Description', type: 'text', required: false, placeholder: 'Optional' }
    ] },
  { key: 'finance', label: 'Finance', icon: '📊', path: 'finance', endpoint: '/finance/journal-entries', summary: 'reference', category: 'Finance',
    createFields: [
      { name: 'id', label: 'ID', type: 'text', required: true, placeholder: 'e.g. JE-001' },
      { name: 'reference', label: 'Reference', type: 'text', required: true, placeholder: 'e.g. Month-end adjustment' }
    ] },
  { key: 'posSales', label: 'POS Sales', icon: '💳', path: 'pos-sales', endpoint: '/pos/sales', summary: 'registerId', category: 'POS',
    createFields: [
      { name: 'id', label: 'ID', type: 'text', required: true, placeholder: 'e.g. SALE-001' },
      { name: 'registerId', label: 'Register ID', type: 'text', required: true, placeholder: 'e.g. REG-01' },
      { name: 'total', label: 'Total', type: 'number', required: true, placeholder: 'e.g. 99.99' }
    ] },
  { key: 'reporting', label: 'Reporting', icon: '📈', path: 'reporting', endpoint: '/reporting/run?report=summary', summary: null, raw: true, category: 'Reporting' },
  { key: 'users', label: 'User Management', icon: '👤', path: 'users', endpoint: '/users', adminOnly: true, category: 'System' },
  { key: 'roles', label: 'Role Management', icon: '🔑', path: 'roles', endpoint: '/roles', adminOnly: true, category: 'System' },
  { key: 'settings', label: 'Settings', icon: '⚙️', path: 'settings', endpoint: '/profile', systemOnly: true, category: 'System' }
];

const CATEGORY_ORDER = ['Assets Management', 'Procurement', 'Workflow Management', 'Reporting', 'HR Management', 'Finance', 'POS', 'System'];

// MODULES is mutated in-place by filterModulesByAccess so all importers see updates
const MODULES = [...ALL_MODULES];

function filterModulesByAccess(accessibleModules) {
  const accessibleKeys = accessibleModules.map(m => m.key);
  const user = getStoredUser();
  const isSuperAdmin = user?.isSuperAdmin || (user?.roles || []).includes('SuperAdmin') || false;
  const isAdmin = isSuperAdmin || user?.roles?.includes('Admin') || user?.roles?.includes('admin') || false;
  const filtered = ALL_MODULES.filter(m => {
    if (m.systemOnly) return true;
    if (m.adminOnly && !isSuperAdmin && !isAdmin) return false;
    if (m.key === 'users' || m.key === 'roles') return isSuperAdmin || isAdmin;
    if (isSuperAdmin || isAdmin) return true;
    return accessibleKeys.includes(m.key);
  });
  MODULES.length = 0;
  MODULES.push(...filtered);
}

function canViewModule(user, key) {
  if (!user) return true;
  const isSA = user?.isSuperAdmin || (user?.roles || []).includes('SuperAdmin') || false;
  const isAdmin = isSA || user?.roles?.includes('Admin') || user?.roles?.includes('admin') || false;
  if (isSA || isAdmin) return true;
  return (user?.accessibleModules || user?.modules || []).some(m => m.key === key);
}

const ALL_METRIC_SPECS = [
  { key: 'totalAssets', label: 'Assets', icon: 'box-seam', module: 'assets', endpoint: '/assets' },
  { key: 'activeEmployees', label: 'Active Employees', icon: 'people', module: 'employees', endpoint: '/hr/employees' },
  { key: 'pendingApprovals', label: 'Pending Approvals', icon: 'clock', module: 'approvals', endpoint: '/approvals' },
  { key: 'totalSales', label: 'Total Sales', icon: 'currency-dollar', money: true, module: 'posSales', endpoint: '/pos/sales' },
  { key: 'inventoryItemCount', label: 'Inventory Items', icon: 'boxes', module: 'inventory', endpoint: '/inventory/items' },
  { key: 'totalInventoryQuantity', label: 'Total Stock', icon: 'archive', module: 'inventory', endpoint: '/inventory/items' },
  { key: 'totalPurchaseOrders', label: 'Purchase Orders', icon: 'file-text', module: 'purchaseOrders', endpoint: '/procurement/purchase-orders' },
  { key: 'totalJournalEntries', label: 'Journal Entries', icon: 'journal', module: 'finance', endpoint: '/finance/journal-entries' }
];

// ===== Analytics (rendered on the dashboard for modules the user can access) =====
const ANALYTICS_CHART_SPECS = [
  { key: 'assetStatus', label: 'Assets by Status', module: 'assets' },
  { key: 'inventoryBySku', label: 'Inventory by SKU (Top)', module: 'inventory', labelField: 'sku', valueField: 'quantity' },
  { key: 'salesTrend', label: 'Sales Trend', module: 'posSales', labelField: 'period', valueField: 'total', money: true },
  { key: 'payrollByDepartment', label: 'Payroll by Department', module: 'payroll', money: true },
  { key: 'employeeByDepartment', label: 'Employees by Department', module: 'employees' },
  { key: 'employeeStatus', label: 'Employees by Status', module: 'employees' },
  { key: 'purchaseOrderByStatus', label: 'Purchase Orders by Status', module: 'purchaseOrders' },
  { key: 'auditActivity', label: 'Audit Activity (Last 30d)', module: null, labelField: 'date', valueField: 'count' }
];

const CATEGORY_ICONS = {
  'Assets Management': '📦', 'Procurement': '🛒', 'Workflow Management': '✅',
  'HR Management': '👥', 'Finance': '💰', 'POS': '💳',
  'Reporting': '📈', 'System': '⚙️'
};

const MONTH_NAMES = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

export {
  ALL_MODULES, CATEGORY_ORDER, MODULES, filterModulesByAccess, canViewModule,
  ALL_METRIC_SPECS, ANALYTICS_CHART_SPECS, CATEGORY_ICONS, MONTH_NAMES
};
