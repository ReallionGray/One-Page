const e = React.createElement;
const root = document.getElementById('app');

// ===== Utilities =====
const formatMoney = (n) => new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD', minimumFractionDigits: 0, maximumFractionDigits: 2 }).format(n || 0);
const formatNum = (n) => new Intl.NumberFormat(undefined, { maximumFractionDigits: 2 }).format(n || 0);
const STATUS_COLORS = { pending: '#fbbf24', approved: '#6ee7b7', rejected: '#f87171' };

// ===== Audit Event Humanization =====
const HUMANIZE_ACTIONS = {
  'hr.employee.create':            { label: 'Created employee',              color: '#38bdf8' },
  'hr.employee.offboard':          { label: 'Offboarded employee',            color: '#f59e0b' },
  'hr.employment.create':          { label: 'Created employment',             color: '#34d399' },
  'hr.checklist.create':           { label: 'Created checklist item',         color: '#a78bfa' },
  'hr.checklist.complete':         { label: 'Completed checklist item',       color: '#4ade80' },
  'hr.leave.request.create':       { label: 'Requested leave',                color: '#fbbf24' },
  'hr.leave.request.decide':       { label: 'Reviewed leave request',         color: '#10b981' },
  'hr.leave.policy.create':        { label: 'Created leave policy',           color: '#8b5cf6' },
  'hr.leave.balance.create':       { label: 'Set leave balance',              color: '#8b5cf6' },
  'hr.document.create':            { label: 'Uploaded document',              color: '#06b6d4' },
  'hr.time.entry.create':          { label: 'Recorded time entry',            color: '#14b8a8' },
  'hr.time.entry.clockout':        { label: 'Recorded clock-out',             color: '#0d9488' },
  'hr.overtime.request.create':    { label: 'Requested overtime',             color: '#f97316' },
  'hr.overtime.request.approve':   { label: 'Approved overtime',              color: '#10b981' },
  'hr.payroll.record.create':      { label: 'Created payroll record',        color: '#84cc16' },
  'hr.payroll.record.process':     { label: 'Processed payroll',              color: '#22c55e' },
  'hr.payroll.record.process.calculations': { label: 'Calculated payroll',   color: '#22c55e' },
  'hr.payroll.record.bonus':       { label: 'Added bonus',                    color: '#22c55e' },
  'hr.payroll.record.deduction':   { label: 'Applied deduction',              color: '#ef4444' },
  'hr.payroll.record.tax':         { label: 'Applied tax',                    color: '#f59e0b' },
  'hr.payroll.record.pension':     { label: 'Applied pension',                color: '#3b82f6' },
  'hr.payroll.record.pay':         { label: 'Processed payroll payment',      color: '#22c55e' },
  'hr.payroll.record.payslip':     { label: 'Generated payslip',              color: '#06b6d4' },
  'hr.loan.create':                { label: 'Created employee loan',          color: '#a78bfa' },
  'hr.loan.repay':                 { label: 'Recorded loan repayment',        color: '#4ade80' },
  'hr.disciplinary.create':        { label: 'Opened disciplinary case',       color: '#ef4444' },
  'hr.disciplinary.resolve':       { label: 'Resolved disciplinary case',     color: '#10b981' },
  'hr.disciplinary.expunge':       { label: 'Expunged disciplinary record',   color: '#6b7280' },
  'hr.disciplinary.cancel':        { label: 'Cancelled disciplinary case',    color: '#6b7280' },
  'hr.performance.review.create':  { label: 'Created performance review',     color: '#8b5cf6' },
  'hr.performance.review.start':   { label: 'Started performance review',     color: '#8b5cf6' },
  'hr.performance.review.submit':  { label: 'Submitted performance review',     color: '#10b981' },
  'hr.performance.review.complete':{ label: 'Completed performance review',   color: '#22c55e' },
  'hr.performance.review.comments':{ label: 'Added review comments',           color: '#38bdf8' },
  'hr.performance.goal.create':    { label: 'Created performance goal',        color: '#a78bfa' },
  'hr.performance.goal.progress':  { label: 'Updated goal progress',           color: '#34d399' },
  'hr.performance.feedback.create':{ label: 'Provided 360 feedback',           color: '#06b6d4' },
  'hr.performance.competency.create': { label: 'Assessed competency',         color: '#84cc16' },
  'hr.performance.appraisalcommittee.create': { label: 'Created appraisal committee', color: '#a78bfa' },
  'hr.recruitment.job.create':     { label: 'Created job posting',            color: '#a78bfa' },
  'hr.recruitment.job.publish':    { label: 'Published job posting',          color: '#22c55e' },
  'hr.recruitment.job.close':      { label: 'Closed job posting',             color: '#6b7280' },
  'hr.recruitment.application.create': { label: 'New job application',       color: '#38bdf8' },
  'hr.recruitment.application.status': { label: 'Updated application status', color: '#34d399' },
  'hr.recruitment.interview.create': { label: 'Scheduled interview',         color: '#38bdf8' },
  'hr.recruitment.interview.complete': { label: 'Completed interview',       color: '#10b981' },
  'hr.recruitment.interview.cancel': { label: 'Cancelled interview',          color: '#f59e0b' },
  'hr.recruitment.offer.create':   { label: 'Created job offer',              color: '#8b5cf6' },
  'hr.recruitment.offer.send':     { label: 'Sent job offer',                 color: '#38bdf8' },
  'hr.recruitment.offer.accept':   { label: 'Offer accepted',                 color: '#22c55e' },
  'hr.recruitment.offer.reject':   { label: 'Offer rejected',                 color: '#ef4444' },
  'hr.recruitment.offer.withdraw': { label: 'Offer withdrawn',                color: '#f59e0b' },
  'asset.create':                  { label: 'Created asset',                  color: '#38bdf8' },
  'asset.assign':                  { label: 'Assigned asset',                 color: '#34d399' },
  'asset.dispose.request':       { label: 'Requested asset disposal',         color: '#fbbf24' },
  'asset.dispose':               { label: 'Disposed asset',                 color: '#ef4444' },
  'pos.sale.create':             { label: 'Created sale',                   color: '#22c55e' },
  'finance.journal.create':      { label: 'Created journal entry',          color: '#38bdf8' },
  'inventory.adjust':            { label: 'Adjusted inventory',             color: '#f59e0b' },
  'inventory.adjust.request':    { label: 'Requested inventory adjustment', color: '#fbbf24' },
  'approval.step.approve':       { label: 'Approved workflow step',         color: '#22c55e' },
};

function humanizeAuditEvent(ev) {
  if (!ev) return { title: 'Event', desc: '', user: 'System', color: '#94a3b8', createdAt: null };
  var fullAction = ev.action || '';
  var colonIdx = fullAction.indexOf(':');
  var actionKey = colonIdx >= 0 ? fullAction.substring(0, colonIdx) : fullAction;
  var entityId = colonIdx >= 0 ? fullAction.substring(colonIdx + 1) : (ev.resourceId || '');
  var mapping = HUMANIZE_ACTIONS[actionKey] || { label: actionKey.replace(/^\w+\./, ''), color: '#94a3b8' };
  return {
    title: mapping.label,
    desc: entityId,
    user: ev.actorUserId || 'System',
    color: mapping.color,
    createdAt: ev.createdAt,
  };
}

// ===== Chart color palette =====
const CHART_COLORS = ['#38bdf8', '#34d399', '#a78bfa', '#fbbf24', '#ef4444', '#10b981', '#f59e0b', '#06b6d4', '#84cc16', '#f97316', '#8b5cf6', '#ec4899'];

const prettifyKey = (k) => {
  if (!k) return '';
  const map = { id: 'ID', sku: 'SKU', qty: 'Qty', status: 'Status', createdAt: 'Created', updatedAt: 'Updated', registerId: 'Register', totalAmount: 'Total Amount', total: 'Total', reference: 'Reference', name: 'Name', tag: 'Tag', firstName: 'First Name', lastName: 'Last Name', email: 'Email', department: 'Department', jobTitle: 'Job Title', phoneNumber: 'Phone', position: 'Position', salary: 'Salary', hireDate: 'Hire Date', departmentId: 'Department', locationId: 'Location', legalEntityId: 'Legal Entity', branchId: 'Branch', custodianEmployeeId: 'Custodian', reason: 'Reason', resourceId: 'Resource ID', resourceType: 'Type', requestedBy: 'Requester', workflowDefinitionId: 'Workflow', currentStep: 'Step', decisions: 'Decisions', approverType: 'Approver Type', approverValue: 'Approver Value', canSkip: 'Can Skip', amount: 'Amount', quantity: 'Quantity', periodStart: 'Period Start', periodEnd: 'Period End', currency: 'Currency', employeeId: 'Employee ID', description: 'Description' };
  return map[k] || k.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase());
};

const formatValue = (v) => {
  if (v === null || v === undefined) return '—';
  if (typeof v === 'boolean') return v ? 'Yes' : 'No';
  if (typeof v === 'string') {
    const d = new Date(v);
    if (!isNaN(d.getTime()) && (v.includes('T') || v.includes('-'))) return d.toLocaleString();
  }
  if (typeof v === 'number') return v;
  return String(v);
};

const getStoredUser = () => {
  try {
    const raw = localStorage.getItem('onepage_user');
    return raw ? JSON.parse(raw) : null;
  } catch { return null; }
};

// ===== API Client =====
function ApiClient() {
  const base = window.__ONEPAGE_API_BASE__ || 'http://localhost:5001/api/v1';
  const getUser = () => { try { return JSON.parse(localStorage.getItem('onepage_user') || '{}'); } catch { return {}; } };
  async function request(path, method = 'GET', body) {
    const user = getUser();
    const headers = { 'Content-Type': 'application/json', 'X-Tenant-Id': user.tenantId || 'demo-tenant' };
    if (user.accessToken) headers['Authorization'] = 'Bearer ' + user.accessToken;
    const opts = { method, headers };
    if (body) opts.body = JSON.stringify(body);
    const res = await fetch(base + path, opts);
    const text = await res.text();
    let data; try { data = text ? JSON.parse(text) : null; } catch { data = null; }
    return { ok: res.ok, status: res.status, data };
  }
  return {
    get: (path) => request(path, 'GET'),
    post: (path, body) => request(path, 'POST', body),
    put: (path, body) => request(path, 'PUT', body),
    del: (path) => request(path, 'DELETE')
  };
}

// ===== Auth Context =====
const AuthContext = React.createContext();
function useAuth() { return React.useContext(AuthContext); }

function AuthProvider({ children }) {
  const [user, setUser] = React.useState(() => getStoredUser());
  const login = React.useCallback((userData) => {
    localStorage.setItem('onepage_user', JSON.stringify(userData));
    setUser(userData);
  }, []);
  const logout = React.useCallback(() => {
    localStorage.removeItem('onepage_user');
    setUser(null);
    window.location.hash = '#login';
  }, []);
  const value = React.useMemo(() => ({ user, login, logout, isAuthenticated: !!user }), [user, login, logout]);
  return e(AuthContext.Provider, { value }, children);
}

function useToast() {
  const [toasts, setToasts] = React.useState([]);
  const show = React.useCallback((message, type) => {
    const id = Date.now() + Math.random();
    setToasts(t => [...t, { id, message, type }]);
    setTimeout(() => setToasts(t => t.filter(x => x.id !== id)), 4000);
  }, []);
  const ToastHost = () => e(React.Fragment, null,
    toasts.map(t => e('div', {
      key: t.id, className: 'alert-toast ' + (t.type === 'err' ? 'err' : 'ok'),
      onClick: () => setToasts(x => x.filter(y => y.id !== t.id))
    }, t.message)));
  return { toast: show, ToastHost };
}

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
  { key: 'leaveManagement', label: 'Leave Management', icon: '🏖️', path: 'leave-management', endpoint: '/hr/leave', customComponent: 'LeaveManagement', category: 'HR Management' },
  { key: 'performanceManagement', label: 'Performance', icon: '📊', path: 'performance-management', endpoint: '/hr/performance', customComponent: 'PerformanceManagement', category: 'HR Management' },
  { key: 'recruitment', label: 'Recruitment', icon: '🎯', path: 'recruitment', endpoint: '/hr/recruitment', customComponent: 'Recruitment', category: 'HR Management' },
  { key: 'timeAttendance', label: 'Time & Attendance', icon: '⏰', path: 'time-attendance', endpoint: '/hr/time', customComponent: 'TimeAttendance', category: 'HR Management' },
  { key: 'disciplinary', label: 'Disciplinary', icon: '⚠️', path: 'disciplinary', endpoint: '/hr/disciplinary', customComponent: 'DisciplinaryManagement', category: 'HR Management' },
  { key: 'onboarding', label: 'Onboarding', icon: '👋', path: 'onboarding', endpoint: '/hr/onboarding', customComponent: 'OnboardingOffboarding', category: 'HR Management' },
  { key: 'employeeDocuments', label: 'Documents', icon: '📄', path: 'employee-documents', endpoint: '/hr/documents', customComponent: 'EmployeeDocuments', category: 'HR Management' },
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

let MODULES = [...ALL_MODULES];

function filterModulesByAccess(accessibleModules) {
  const accessibleKeys = accessibleModules.map(m => m.key);
  const user = getStoredUser();
  const isSuperAdmin = user?.isSuperAdmin || (user?.roles || []).includes('SuperAdmin') || false;
  const isAdmin = isSuperAdmin || user?.roles?.includes('Admin') || user?.roles?.includes('admin') || false;
  MODULES = ALL_MODULES.filter(m => {
    if (m.systemOnly) return true;
    if (m.adminOnly && !isSuperAdmin && !isAdmin) return false;
    if (m.key === 'users' || m.key === 'roles') return isSuperAdmin || isAdmin;
    if (isSuperAdmin || isAdmin) return true;
    return accessibleKeys.includes(m.key);
  });
}

function canViewModule(user, key) {
  // Charts/analytics not tied to a specific module (e.g. the cross-cutting
  // "Audit Activity" timeline) are visible to any authenticated user.
  if (!key) return true;
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
  { key: 'totalPayroll', label: 'Payroll (Paid)', icon: 'wallet2', module: 'payroll', endpoint: '/payroll/records', money: true },
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

// ===== Create Item Modal =====
function CreateItemModal({ module, onClose, onCreate }) {
  const [formData, setFormData] = React.useState({});
  const [saving, setSaving] = React.useState(false);
  const [error, setError] = React.useState(null);
  const fields = module.createFields || [];

  const handleChange = (name, value) => setFormData({ ...formData, [name]: value });

  const handleSubmit = async () => {
    for (const f of fields) {
      if (f.required && (formData[f.name] === undefined || formData[f.name] === null || formData[f.name] === '')) {
        setError(f.label + ' is required');
        return;
      }
    }
    setError(null);
    setSaving(true);
    try {
      const payload = {};
      fields.forEach(f => {
        if (formData[f.name] !== undefined && formData[f.name] !== '') {
          if (f.type === 'number') payload[f.name] = Number(formData[f.name]);
          else payload[f.name] = formData[f.name];
        }
      });
      await onCreate(payload);
    } catch (err) { setError(err.message || 'Create failed'); }
    finally { setSaving(false); }
  };

  return e('div', { className: 'detail-overlay' },
    e('div', { className: 'detail-panel card-ghost' },
      e('div', { className: 'd-flex justify-content-between align-items-center mb-3' },
        e('h5', { className: 'mb-0' }, 'New ' + module.label),
        e('button', { className: 'btn btn-sm btn-outline-light', onClick: onClose, disabled: saving }, '×')),
      error ? e('div', { className: 'alert alert-danger' }, error) : null,
      e('div', { className: 'detail-grid' },
        fields.map(f => e('div', { className: 'detail-row', key: f.name },
          e('span', { className: 'detail-label' }, f.label + (f.required ? ' *' : '')),
          e('span', { className: 'detail-value' },
            f.type === 'number'
              ? e('input', { type: 'number', className: 'form-control form-control-sm', placeholder: f.placeholder || '', value: formData[f.name] ?? '', onChange: ev => handleChange(f.name, ev.target.value), disabled: saving })
              : f.type === 'date'
                ? e('input', { type: 'date', className: 'form-control form-control-sm', value: formData[f.name] ?? '', onChange: ev => handleChange(f.name, ev.target.value), disabled: saving })
                : e('input', { type: 'text', className: 'form-control form-control-sm', placeholder: f.placeholder || '', value: formData[f.name] ?? '', onChange: ev => handleChange(f.name, ev.target.value), disabled: saving }))))),
      e('div', { className: 'd-flex justify-content-end gap-2 mt-3' },
        e('button', { className: 'btn btn-sm btn-outline-light', onClick: onClose, disabled: saving }, 'Cancel'),
        e('button', { className: 'btn btn-sm btn-accent', onClick: handleSubmit, disabled: saving },
          saving ? e('span', null, e('span', { className: 'spinner-border spinner-border-sm' }), ' Creating...') : 'Create'))));
}

// ===== Item Card =====
function ItemCard({ item }) {
  const entries = Object.entries(item || {}).filter(([k]) => !['id', 'tenantId', 'createdAt', 'updatedAt'].includes(k));
  const title = item.id || item.tag || item.name || item.reference || item.sku || '#item';
  return e('div', { className: 'module-card card-ghost h-100' },
    e('div', { className: 'module-card-head' }, e('h6', { className: 'module-card-title' }, String(title).slice(0, 36))),
    e('div', { className: 'module-card-body' }, entries.map(([k, v]) =>
      e('div', { className: 'module-card-field' },
        e('span', { className: 'module-field-label' }, prettifyKey(k) + ':'),
        e('span', { className: 'module-field-value' }, formatValue(v))))));
}

// ===== Module Chart =====
function ModuleChart({ items, field, label }) {
  const canvasRef = React.useRef(null);
  React.useEffect(() => {
    const ctx = canvasRef.current;
    if (!ctx) return;
    if (ctx.chartInstance) ctx.chartInstance.destroy();
    const counts = {};
    items.forEach(it => { const v = it[field] || 'unknown'; counts[v] = (counts[v] || 0) + 1; });
    const labels = Object.keys(counts);
    const dataValues = labels.map(l => counts[l]);
    ctx.chartInstance = new Chart(ctx, {
      type: 'bar',
      data: { labels, datasets: [{ label: label || 'Count', data: dataValues, backgroundColor: 'rgba(110, 231, 183, .35)', borderColor: '#6ee7b7', borderWidth: 1 }] },
      options: { plugins: { legend: { display: false } }, responsive: true, maintainAspectRatio: false, scales: { y: { ticks: { color: '#94a3b8' }, grid: { color: 'rgba(255,255,255,.06)' } }, x: { ticks: { color: '#94a3b8' }, grid: { display: false } } } }
    });
    return () => { if (ctx.chartInstance) ctx.chartInstance.destroy(); ctx.chartInstance = null; };
  }, [items, field, label]);
  return e('canvas', { ref: canvasRef, height: 130 });
}

// ===== MODULE PAGE =====
function ModulePage({ moduleKey }) {
  const api = React.useMemo(() => ApiClient(), []); const { logout } = useAuth(); const { toast, ToastHost } = useToast();
  const mod = MODULES.find(m => m.key === moduleKey) || MODULES[0];
  
  // Check if this module uses a custom component
  if (mod.customComponent) {
    const CustomComponent = window[mod.customComponent];
    if (CustomComponent) {
      return e('div', { className: 'module-page' },
        e('div', { className: 'd-flex justify-content-between align-items-center mb-3 flex-wrap gap-2' },
          e('div', { className: 'd-flex align-items-center gap-2' },
            e('a', { href: '#dashboard', className: 'btn btn-sm btn-outline-light' }, e('i', { className: 'bi bi-arrow-left' }), ' Dashboard'),
            e('h4', { className: 'mb-0 hero-title' }, mod.icon + ' ' + mod.label))),
        e(CustomComponent, { api, toast }),
        e(ToastHost)
      );
    }
  }
  
  const [items, setItems] = React.useState(null);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState(null);
  const [showCreateModal, setShowCreateModal] = React.useState(false);
  const load = React.useCallback(async () => {
    setLoading(true); setError(null);
    try {
      const r = await api.get(mod.endpoint);
      if (r.status === 401) { logout(); return; }
      if (!r.ok) throw new Error((r.data && (r.data.title || r.data.detail)) || 'Failed to load ' + mod.label);
      setItems(r.data);
    } catch (err) { setError(err.message || ('Failed to load ' + mod.label)); }
    finally { setLoading(false); }
  }, [api, mod, logout]);
  React.useEffect(() => { load(); }, [load]);

  const handleCreate = React.useCallback(async (formData) => {
    const r = await api.post(mod.endpoint, formData);
    if (r.status === 401) { logout(); return; }
    if (!r.ok) throw new Error((r.data && (r.data.title || r.data.detail)) || 'Create failed');
    toast(mod.label + ' created', 'ok');
    setShowCreateModal(false);
    await load();
  }, [api, mod, load, toast, logout]);

  const list = mod.raw ? null : (Array.isArray(items) ? items : (items ? [items] : []));
  const summaryField = mod.summary || (list && list[0] ? Object.keys(list[0]).find(k => !['id', 'tenantId'].includes(k)) || '' : '');
  return e('div', { className: 'module-page' },
    e('div', { className: 'd-flex justify-content-between align-items-center mb-3 flex-wrap gap-2' },
      e('div', { className: 'd-flex align-items-center gap-2' },
        e('a', { href: '#dashboard', className: 'btn btn-sm btn-outline-light' }, e('i', { className: 'bi bi-arrow-left' }), ' Dashboard'),
        e('h4', { className: 'mb-0 hero-title' }, mod.icon + ' ' + mod.label)),
      e('div', { className: 'd-flex gap-2' },
        e('button', { className: 'btn btn-sm btn-light', onClick: load, disabled: loading }, loading ? e('span', { className: 'spinner-border spinner-border-sm' }) : e('i', { className: 'bi bi-arrow-clockwise' })),
        mod.createFields && mod.createFields.length > 0
          ? e('button', { className: 'btn btn-sm btn-accent', onClick: () => setShowCreateModal(true) }, e('i', { className: 'bi bi-plus-lg' }), ' New ' + mod.label)
          : null)),
    error ? e('div', { className: 'alert alert-danger' }, 'Error: ' + error) : null,
    (loading && !list) ? e('div', { className: 'module-grid' }, Array.from({ length: 6 }).map((_, i) => e('div', { key: i, className: 'module-card card-ghost skeleton' }, { style: { height: 110 } })))
      : mod.raw
        ? e('div', { className: 'card-ghost' }, e('pre', { className: 'result-box' }, JSON.stringify(items, null, 2)))
        : list.length === 0
          ? e('div', { className: 'card-ghost' }, e('div', { className: 'empty-state' }, 'No records found for ' + mod.label + '.'),
              e('div', { className: 'small-muted mt-2' }, mod.createFields ? 'Click "New ' + mod.label + '" above to create your first record.' : 'Try creating records via the API, or switch to the super-admin demo user in the sidebar.'))
          : e(React.Fragment, null,
              e('div', { className: 'chart-card card-ghost h-100 mb-3' },
                e('h6', { className: 'chart-card-title' }, 'By ' + prettifyKey(summaryField || 'field')),
                e('div', { className: 'chart-wrap' }, e(ModuleChart, { items: list, field: summaryField, label: mod.label }))),
              e('div', { className: 'module-grid' }, list.map((item, i) => e(ItemCard, { key: (item && (item.id || item.tag || item.sku || item.reference)) || 'm' + i, item: item })))
    ,
    mod.createFields && showCreateModal
      ? e(CreateItemModal, { module: mod, onClose: () => setShowCreateModal(false), onCreate: handleCreate })
      : null,
    e(ToastHost)));
}

// ===== DASHBOARD PAGE =====
function AssetForm({ onCreated }) {
  const [tag, setTag] = React.useState('ASSET-' + Math.floor(Math.random() * 9000 + 1000));
  const [name, setName] = React.useState('Demo Laptop');
  const [location, setLocation] = React.useState('HQ');
  const [submitting, setSubmitting] = React.useState(false);
  const api = React.useMemo(() => ApiClient(), []);
  const { toast, ToastHost } = useToast();
  async function submit(ev) {
    ev.preventDefault(); setSubmitting(true);
    try {
      const id = 'asset-' + Date.now();
      const payload = { Id: id, Tag: tag, Name: name, Description: 'Demo asset', LocationId: location };
      const r = await api.post('/assets', payload);
      if (r.ok) { toast('Asset created', 'ok'); if (onCreated) onCreated(r.data); setTag('ASSET-' + Math.floor(Math.random() * 9000 + 1000)); setName('Demo Laptop'); setLocation('HQ'); }
      else { toast('Create failed', 'err'); }
    } catch (err) { toast('Create failed', 'err'); } finally { setSubmitting(false); }
  }
  return e('form', { className: 'quick-form', onSubmit: submit },
    e('div', { className: 'quick-form-head' }, e('h6', null, 'Quick Add Asset'), e('i', { className: 'bi bi-plus-square' })),
    e('div', { className: 'row g-2' },
      e('div', { className: 'col-12 col-sm-4 field' }, e('label', null, 'Tag'), e('input', { className: 'form-control form-control-sm', value: tag, onChange: ev => setTag(ev.target.value), required: true })),
      e('div', { className: 'col-12 col-sm-8 field' }, e('label', null, 'Name'), e('input', { className: 'form-control form-control-sm', value: name, onChange: ev => setName(ev.target.value), required: true }))),
    e('div', { className: 'row g-2' },
      e('div', { className: 'col-12 col-sm-8 field' }, e('label', null, 'Location'), e('input', { className: 'form-control form-control-sm', value: location, onChange: ev => setLocation(ev.target.value), required: true })),
      e('div', { className: 'col-12 col-sm-4 field d-flex align-items-end' }, e('button', { type: 'submit', className: 'btn btn-accent w-100', disabled: submitting }, submitting ? e('span', { className: 'spinner-border spinner-border-sm' }) : 'Create Asset'))));
}

function pad2(n) { return String(n).padStart(2, '0'); }

function DigitalClock() {
  const [now, setNow] = React.useState(() => new Date());
  React.useEffect(() => {
    const id = setInterval(() => setNow(new Date()), 1000);
    return () => clearInterval(id);
  }, []);
  const hours = pad2(now.getHours());
  const minutes = pad2(now.getMinutes());
  const seconds = pad2(now.getSeconds());
  const dateLabel = now.toLocaleDateString(undefined, { weekday: 'short', day: 'numeric', month: 'short', year: 'numeric' });
  return e('div', { className: 'digital-clock', role: 'timer', 'aria-label': 'Current time ' + hours + ':' + minutes + ':' + seconds + ', ' + dateLabel },
    e('div', { className: 'digital-clock-time', 'aria-hidden': 'true' },
      e('span', null, hours),
      e('span', { className: 'digital-clock-colon' }, ':'),
      e('span', null, minutes),
      e('span', { className: 'digital-clock-colon' }, ':'),
      e('span', null, seconds)),
    e('div', { className: 'digital-clock-date' }, dateLabel));
}

function DashboardHeader({ loading, onRefresh }) {
  return e('div', { className: 'hero-banner' },
    e('div', { className: 'brand' },
      e('div', { className: 'logo', 'aria-hidden': 'true' }),
      e('div', null,
        e('h1', { className: 'hero-title' }, 'One Page'),
        e('p', { className: 'hero-tagline' }, 'Operations dashboard'))),
    e('div', { className: 'hero-banner-right' },
      e(DigitalClock),
      e('button', { className: 'btn btn-sm btn-light', onClick: onRefresh, disabled: loading, title: 'Refresh dashboard', 'aria-label': 'Refresh dashboard' },
        loading ? e('span', { className: 'spinner-border spinner-border-sm' }) : e('i', { className: 'bi bi-arrow-clockwise' }))));
}

const MONTH_NAMES = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

// "Activity by Type": a bar chart grouping recent (meaningful) events by their
// humanized action type, so the dashboard reflects actual business events
// ("Created employee", "Processed payroll", …) rather than raw API request logs.
function ChartFromEvents({ events }) {
  const byType = {};
  (events || []).forEach(ev => {
    const h = humanizeAuditEvent(ev);
    const label = h.title || 'Event';
    byType[label] = (byType[label] || 0) + 1;
  });
  const segments = Object.keys(byType).length
    ? Object.entries(byType).map(([label, value]) => ({ label, value }))
    : [];
  return segments.length > 0 ? e(SegmentedChart, { segments: segments, money: false })
    : e('div', { className: 'chart-placeholder' }, 'No activity by type');
}

// ===== Segmented Bar Chart (per-module analytics from /dashboard) =====
function SegmentedChart({ segments, money }) {
  const canvasRef = React.useRef(null);
  React.useEffect(() => {
    const ctx = canvasRef.current;
    if (!ctx) return;
    if (ctx.chartInstance) ctx.chartInstance.destroy();
    var labels = (segments || []).map(x => x.label);
    var dataValues = (segments || []).map(x => Number(x.value) || 0);
    var bgColors = labels.map((_, i) => CHART_COLORS[i % CHART_COLORS.length]);
    ctx.chartInstance = new Chart(ctx, {
      type: 'bar',
      data: { labels, datasets: [{ label: 'Value', data: dataValues, backgroundColor: bgColors, borderColor: bgColors, borderWidth: 1, borderRadius: 4, barPercentage: .7 }] },
      options: {
        plugin: { legend: { display: false }, tooltip: {
          backgroundColor: 'rgba(15, 23, 42, .9)', borderColor: 'rgba(255,255,255,.1)',
          titleColor: '#e6eef8', bodyColor: '#cbd5e1', displayColors: false,
          callbacks: {
            label: (c) => money ? formatMoney(c.formattedValue) : c.formattedValue,
            labelValue: (ctx) => money ? formatMoney(ctx.dataset.data[ctx.dataIndex]) : ctx.dataset.data[ctx.dataIndex]
          }
        }},
        responsive: true, maintainAspectRatio: false,
        animation: { duration: 700, easing: 'easeOutQuart' },
        scales: {
          y: { beginAtZero: true, ticks: { color: '#94a3b8', precision: 0 }, grid: { color: 'rgba(255,255,255,.06)' } },
          x: { ticks: { color: '#94a3b8', maxRotation: 30 }, grid: { display: false } }
        }
      }
    });
    return () => { if (ctx.chartInstance) ctx.chartInstance.destroy(); ctx.chartInstance = null; };
  }, [segments, money]);
  return e('canvas', { ref: canvasRef, height: 130 });
}

// ===== Analytics Chart Card =====
function AnalyticsChartCard({ spec, segments }) {
  const data = Array.isArray(segments) ? segments : [];
  const norm = data.map(x => ({ label: x.label != null ? x.label : (x[spec.labelField] != null ? x[spec.labelField] : ''), value: x.value != null ? x.value : (x[spec.valueField] != null ? x[spec.valueField] : 0) }));
  return e('div', { className: 'chart-card card-ghost h-100' },
    e('div', { className: 'chart-card-head' }, e('h6', { className: 'chart-card-title' }, spec.label)),
    e('div', { className: 'chart-wrap' }, norm.length > 0 ? e(SegmentedChart, { segments: norm, money: spec.money }) : e('div', { className: 'chart-placeholder' }, 'No data yet')));
}

function CalendarGrid({ month, year, events }) {
  const firstDay = new Date(year, month, 1);
  const startDay = firstDay.getDay();
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  const days = [];
  for (let i = 0; i < startDay; i++) days.push({ day: null, current: false });
  // Accepts schedule events (date: yyyy-MM-dd) and activity events
  // (createdAt: ISO string). Date-only strings parse as UTC, so split and
  // rebuild as local to avoid timezone off-by-one on the calendar grid.
  // Mechanical API-call entries are excluded upstream, so every event
  // here is a meaningful business event.
  const toLocalDate = (ev) => {
    const raw = ev.date || ev.createdAt;
    if (!raw) return null;
    const parts = raw.split ? raw.split('-') : null;
    let d;
    if (parts && parts.length === 3) d = new Date(parseInt(parts[0]), parseInt(parts[1]) - 1, parseInt(parts[2]));
    else d = new Date(raw);
    return isNaN(d.getTime()) ? null : d;
  };
  for (let d = 1; d <= daysInMonth; d++) {
    const date = new Date(year, month, d);
    const dayEvents = (events || []).filter(ev => { const ed = toLocalDate(ev); return ed && ed.toDateString() === date.toDateString(); });
    days.push({ day: d, current: true, events: dayEvents });
  }
  const today = new Date();
  return e('div', { className: 'calendar-grid' },
    ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].map(d => e('div', { key: d, className: 'calendar-weekday' }, d)),
    days.map((d, i) => e('div', {
      key: i, className: 'calendar-day ' + (d.current && d.day === today.getDate() && month === today.getMonth() && year === today.getFullYear() ? 'today' : '') + (d.events && d.events.length > 0 ? ' has-events' : '') + (!d.current ? ' out-of-month' : ''),
      style: { height: 44 }
    }, d.current ? e('div', { className: 'calendar-day-num' },
      d.day,
      d.events && d.events.length > 0 ? e('span', { className: 'calendar-event-count', title: d.events.length + ' event(s)' }, d.events.length) : null
    ) : '' ))
  );
}

function EventBadge({ event, className }) {
  var h = humanizeAuditEvent(event);
  var badgeClass = 'event-badge' + (className ? ' ' + className : '');
  return e('div', { className: badgeClass },
    e('span', { className: 'event-dot', style: { color: h.color } }, '●'),
    e('div', { className: 'event-badge-body' },
      e('div', { className: 'event-title' }, h.title),
      e('div', { className: 'event-desc' }, h.desc || ' '),
      e('div', { className: 'event-meta' },
        e('span', { className: 'event-type' }, h.user !== 'System' ? 'By ' + h.user : 'System'),
        e('span', { className: 'event-time' }, h.createdAt ? new Date(h.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : ''))));
}

// ===== Activity News Carousel (top-of-dashboard, horizontal scroll feed) =====
function ActivityCarousel({ events }) {
  const sorted = events.length > 0
    ? events.slice().sort((a, b) => new Date(b.createdAt || '') - new Date(a.createdAt || '')).slice(0, 12)
    : [];
  const trackRef = React.useRef(null);
  const [hasOverflow, setHasOverflow] = React.useState(false);
  const [showPrev, setShowPrev] = React.useState(false);
  const [showNext, setShowNext] = React.useState(false);
  const [paused, setPaused] = React.useState(false);
  const STEP = 280; // px to slide per tick (~one 280px item + gap)

  const updateNav = React.useCallback(() => {
    const t = trackRef.current;
    if (!t) { setHasOverflow(false); setShowPrev(false); setShowNext(false); return; }
    const overflow = t.scrollWidth > t.clientWidth + 1;
    setHasOverflow(overflow);
    setShowPrev(overflow && t.scrollLeft > 8);
    setShowNext(overflow && t.scrollLeft + t.clientWidth < t.scrollWidth - 8);
  }, []);

  React.useEffect(() => {
    if (!sorted.length) return;
    const t = trackRef.current;
    if (!t) return;
    updateNav();
    t.addEventListener('scroll', updateNav);
    return () => t.removeEventListener('scroll', updateNav);
  }, [sorted, updateNav]);

  // Auto-slide (one item every 4s) when there is overflow and the user isn't hovering
  React.useEffect(() => {
    if (!hasOverflow || paused) return;
    const tick = () => {
      const t = trackRef.current;
      if (!t || paused) return;
      if (t.scrollWidth <= t.clientWidth + 1) return; // overflow vanished (e.g. resize)
      if (t.scrollLeft + t.clientWidth >= t.scrollWidth - 24) {
        t.scrollTo({ left: 0, behavior: 'smooth' }); // loop back to start
      } else {
        t.scrollBy({ left: STEP, behavior: 'smooth' });
      }
    };
    const id = setInterval(tick, 4000);
    return () => clearInterval(id);
  }, [hasOverflow, paused]);

  const scrollPrev = () => { const t = trackRef.current; if (t) t.scrollBy({ left: -STEP, behavior: 'smooth' }); };
  const scrollNext = () => { const t = trackRef.current; if (t) t.scrollBy({ left: STEP, behavior: 'smooth' }); };

  const navButtons = e('div', { className: 'carousel-nav' },
    e('button', { className: 'carousel-nav-btn', onClick: scrollPrev, disabled: !showPrev, title: 'Previous', 'aria-label': 'Previous activity' }, e('i', { className: 'bi bi-chevron-left' })),
    e('button', { className: 'carousel-nav-btn', onClick: scrollNext, disabled: !showNext, title: 'Next', 'aria-label': 'Next activity' }, e('i', { className: 'bi-chevron-right' })));

  return e('div', { className: 'activity-carousel chart-card card-ghost mb-3' },
    e('div', { className: 'activity-carousel-head' },
      e('span', { className: 'chart-card-title' }, 'Recent Activity'),
      navButtons),
    sorted.length > 0
      ? e('div', { className: 'activity-carousel-track', ref: trackRef, onMouseEnter: () => setPaused(true), onMouseLeave: () => setPaused(false) }, sorted.map(ev => e(EventBadge, { key: ev.id || ev.createdAt, event: ev, className: 'carousel-item' })))
      : e('div', { className: 'chart-placeholder' }, 'No recent activity'));
}

function DashboardPage() {
  const api = React.useMemo(() => ApiClient(), []); const { user, logout } = useAuth(); const { toast, ToastHost } = useToast();
  const [metrics, setMetrics] = React.useState({});
  const [analytics, setAnalytics] = React.useState({});
  const [schedule, setSchedule] = React.useState([]);
  const [loading, setLoading] = React.useState(false);
  const [events, setEvents] = React.useState([]);

  // Calendar month navigation state — starts at the current month
  const now = new Date();
  const [calendarMonth, setCalendarMonth] = React.useState(now.getMonth());
  const [calendarYear, setCalendarYear] = React.useState(now.getFullYear());

  const prevMonth = () => {
    if (calendarMonth === 0) { setCalendarMonth(11); setCalendarYear(y => y - 1); }
    else { setCalendarMonth(m => m - 1); }
  };
  const nextMonth = () => {
    if (calendarMonth === 11) { setCalendarMonth(0); setCalendarYear(y => y + 1); }
    else { setCalendarMonth(m => m + 1); }
  };

  const fetchDashboard = React.useCallback(async () => {
    setLoading(true);
    try {
      const r = await api.get('/dashboard');
      if (r.status === 401) { logout(); return; }
      if (!r.ok) throw new Error((r.data && (r.data.title || r.data.detail)) || 'Failed to load dashboard');
      const d = r.data || {};
      setMetrics(d.metrics || {});
      setAnalytics(d.analytics || {});
      setSchedule(d.schedule || []);
      // Use the dashboard's curated activity feed (semantic events only — no
      // mechanical API-call entries) instead of a separate audit export call,
      // which also avoids requiring ReportExport permission on the dashboard.
      setEvents(Array.isArray(d.activityEvents) ? d.activityEvents : []);
    } catch (err) { toast(err.message || 'Dashboard error', 'err'); }
    finally { setLoading(false); }
  }, [api, toast, logout]);

  React.useEffect(() => { fetchDashboard(); }, [fetchDashboard]);

  const MetricCard = ({ spec }) => {
    const val = metrics[spec.key];
    const numVal = typeof val === 'number' ? val : 0;
    return e('div', { className: 'metric-card' },
      e('span', { className: 'metric-icon' }, e('i', { className: 'bi bi-' + spec.icon })),
      e('div', { className: 'metric-body' },
        e('span', { className: 'metric-value' }, spec.money ? formatMoney(numVal) : formatNum(numVal)),
        e('span', { className: 'metric-label' }, spec.label)));
  };

  return e('div', { className: 'module-page' },
    e(DashboardHeader, { loading, onRefresh: fetchDashboard }),
    e(ActivityCarousel, { events }),
    e('div', { className: 'metrics-grid' }, ALL_METRIC_SPECS.filter(spec => canViewModule(user, spec.module)).map(spec => e(MetricCard, { key: spec.key, spec }))),
    e('div', { className: 'charts-grid' },
      e('div', { className: 'chart-card card-ghost h-100' },
        e('div', { className: 'chart-card-head' }, e('h6', { className: 'chart-card-title' }, 'Activity by Type' )),
        e('div', { className: 'chart-wrap' },
          events.length > 0 ? e(ChartFromEvents, { events }) : e('div', { className: 'chart-placeholder' }, 'No recent activity'))),
      ANALYTICS_CHART_SPECS.map(c => canViewModule(user, c.module)
        ? e(AnalyticsChartCard, { key: c.key, spec: c, segments: analytics[c.key] || [] })
        : null),
    schedule.length > 0 && e('div', { className: 'chart-card chart-card-full card-ghost mb-3' },
      e('div', { className: 'chart-card-head' }, e('h6', { className: 'chart-card-title' }, 'Schedule')),
      e('div', { className: 'calendar-section' },
        e('div', { className: 'calendar-header' },
          e('button', { className: 'btn btn-sm btn-outline-light', onClick: prevMonth, title: 'Previous month' }, '‹'),
          e('div', { className: 'calendar-title' }, MONTH_NAMES[calendarMonth] + ' ' + calendarYear),
          e('button', { className: 'btn btn-sm btn-outline-light', onClick: nextMonth, title: 'Next month' }, '›')),
        e(CalendarGrid, { month: calendarMonth, year: calendarYear, events: schedule })))),

    e(ToastHost)
  );
}

// ===== LOGIN PAGE =====
function LoginPage({ onLogin }) {
  const api = React.useMemo(() => ApiClient(), []);
  const { toast, ToastHost } = useToast();
  const [step, setStep] = React.useState('credentials');
  const [username, setUsername] = React.useState('');
  const [password, setPassword] = React.useState('');
  const [orgs, setOrgs] = React.useState([]);
  const [selectedOrg, setSelectedOrg] = React.useState(null);
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState(null);

  const submit = async (ev) => {
    ev.preventDefault(); setLoading(true); setError(null);
    try {
      const tenantId = selectedOrg ? selectedOrg.tenantId : null;
      const r = await api.post('/auth/login', { username, password, tenantId });
      if (!r.ok) { setError((r.data && (r.data.title || r.data.detail)) || 'Login failed'); return; }
      if (r.data.requiresOrganizationSelection) {
        setOrgs(r.data.organizations || []); setStep('org');
      } else if (r.data.accessToken) {
        // Store the token first so the ApiClient can authenticate the follow-up
        // call, then resolve the user's accessible modules so dashboard charts are
        // gated by each module's real permission (login responses don't carry them).
        const baseUser = { accessToken: r.data.accessToken, username: r.data.username || username, tenantId: r.data.tenantId, roles: r.data.roles || [], isSuperAdmin: r.data.isSuperAdmin || (r.data.roles || []).includes('SuperAdmin') || false, accessibleModules: [] };
        onLogin(baseUser);
        if (!baseUser.isSuperAdmin) {
          try {
            const m = await api.get('/modules/accessible');
            if (m.ok && m.data && Array.isArray(m.data.modules)) {
              onLogin({ ...baseUser, accessibleModules: m.data.modules });
            }
          } catch { }
        }
      }
    } catch (err) { setError(err.message || 'Login failed'); }
    finally { setLoading(false); }
  };

  const handleOrgSelect = (org) => { setSelectedOrg(org); setStep('credentials'); };

  if (step === 'org') {
    return e('div', { className: 'login-page' },
      e('div', { className: 'login-container login-card' },
        e('div', { className: 'login-header' }, e('div', { className: 'login-logo' }), e('h4', { className: 'mt-3' }, 'Select Organization')),
        e('div', { className: 'org-list' }, orgs.map(org => e('button', { key: org.tenantId, className: 'org-option', onClick: () => handleOrgSelect(org) },
          e('div', { className: 'cred-header' }, e('span', { className: 'cred-role' }, org.tenantName)),
          e('div', { className: 'cred-body' }, e('small', null, 'User: ' + username)))))));
  }

  return e('div', { className: 'login-page' },
    e('div', { className: 'login-container login-card' },
      e('div', { className: 'login-header' },
        e('div', { className: 'login-logo' }),
        e('h4', { className: 'mt-3' }, 'Welcome to OnePage'),
        e('div', { className: 'small-muted' }, 'Sign in to your account')),
      error ? e('div', { className: 'alert alert-danger' }, error) : null,
      e('form', { onSubmit: submit, className: 'credentials-box' },
        e('div', { className: 'field' }, e('label', null, 'Username'), e('input', { type: 'email', className: 'form-control', value: username, onChange: ev => setUsername(ev.target.value), required: true, autoFocus: true })),
        e('div', { className: 'field' }, e('label', null, 'Password'), e('input', { type: 'password', className: 'form-control', value: password, onChange: ev => setPassword(ev.target.value), required: true })),
        e('button', { type: 'submit', className: 'btn btn-accent w-100 mt-2', disabled: loading }, loading ? e('span', { className: 'spinner-border spinner-border-sm' }) : 'Sign In')),
    ),
    e(ToastHost)
  );
}

// ===== USER MANAGEMENT PAGE =====
function UserManagementPage() {
  const api = React.useMemo(() => ApiClient(), []); const { logout } = useAuth(); const { toast, ToastHost } = useToast();
  const [users, setUsers] = React.useState([]);
  const [loading, setLoading] = React.useState(false);
  const [showCreateModal, setShowCreateModal] = React.useState(false);
  const [editingUser, setEditingUser] = React.useState(null);

  const load = React.useCallback(async () => {
    setLoading(true);
    try {
      const r = await api.get('/users');
      if (r.status === 401) { logout(); return; }
      if (r.ok) setUsers(Array.isArray(r.data) ? r.data : []);
    } catch { }
    finally { setLoading(false); }
  }, [api, logout]);
  React.useEffect(() => { load(); }, [load]);

  const handleCreateUser = async (userData) => {
    const r = await api.post('/users', userData);
    if (!r.ok) throw new Error((r.data && (r.data.title || r.data.detail)) || 'Create failed');
    toast('User created', 'ok'); setShowCreateModal(false); await load();
  };
  const handleUpdateUser = async (userId, userData) => {
    const r = await api.put('/users/' + userId, userData);
    if (!r.ok) throw new Error((r.data && (r.data.title || r.data.detail)) || 'Update failed');
    toast('User updated', 'ok'); setEditingUser(null); await load();
  };

  const columns = [{ key: 'username', label: 'User' }, { key: 'email', label: 'Email' }, { key: 'fullName', label: 'Full Name' }, { key: 'isSuperAdmin', label: 'Super Admin' }, { key: 'status', label: 'Status' }];
  const renderRow = (item) => e('tr', { key: item.userId || item.id },
    e('td', { className: 'mono-cell' }, item.username || item.id),
    e('td', null, item.email || '—'),
    e('td', null, item.fullName || (item.firstName ? item.firstName + ' ' + item.lastName : '') || '—'),
    e('td', null, item.isSuperAdmin ? e('span', { className: 'badge-status', style: { color: '#6ee7b7' } }, 'Yes') : e('span', { className: 'small-muted' }, 'No')),
    e('td', null, e('span', { className: 'badge-status', style: { color: item.isActive ? '#6ee7b7' : '#94a3b8' } }, item.isActive ? 'Active' : 'Inactive')),
    e('td', { className: 'actions-cell' }, e('button', { className: 'btn btn-sm btn-outline-light', onClick: () => setEditingUser(item) }, 'Edit')));

  const renderTable = (data) => e('div', { className: 'table-responsive' },
    e('table', { className: 'table table-sm table-hover align-middle' },
      e('thead', null, e('tr', null, columns.map(c => e('th', { key: c.key }, c.label)))),
      e('tbody', null, data.length === 0 ? e('tr', null, e('td', { colSpan: columns.length + 1, className: 'text-center small-muted py-4' }, 'No users found')) : data.map(item => renderRow(item)))));

  return e('div', { className: 'module-page' },
    e('div', { className: 'd-flex justify-content-between align-items-center mb-3 flex-wrap gap-2' },
      e('div', { className: 'd-flex align-items-center gap-2' },
        e('a', { href: '#dashboard', className: 'btn btn-sm btn-outline-light' }, e('i', { className: 'bi bi-arrow-left' }), ' Dashboard'),
        e('h4', { className: 'mb-0 hero-title' }, '👤 User Management')),
      e('button', { className: 'btn btn-sm btn-accent', onClick: () => setShowCreateModal(true) }, e('i', { className: 'bi bi-plus-lg' }), ' Create User')),
    (loading && users.length === 0) ? e('div', { className: 'module-card card-ghost skeleton', style: { height: 200 } }) : renderTable(users),
    showCreateModal && e(UserModal, { onClose: () => setShowCreateModal(false), onSave: handleCreateUser, isSuperAdmin: false }),
    editingUser && e(UserModal, { user: editingUser, onClose: () => setEditingUser(null), onSave: (data) => handleUpdateUser(editingUser.userId || editingUser.id, data), isSuperAdmin: false }),
    e(ToastHost)
  );
}

function UserModal({ user, onClose, onSave, isSuperAdmin }) {
  const [formData, setFormData] = React.useState(user || { firstName: '', lastName: '', email: '', jobTitle: '', phoneNumber: '' });
  const [loading, setLoading] = React.useState(false);
  const { toast } = useToast();
  const handleChange = (field, value) => setFormData({ ...formData, [field]: value });
  const handleSubmit = async (ev) => {
    ev.preventDefault(); setLoading(true);
    try { await onSave(formData); toast(user ? 'User updated' : 'User created', 'ok'); onClose(); }
    catch (err) { toast(err.message || 'Save failed', 'err'); }
    finally { setLoading(false); }
  };
  return e('div', { className: 'detail-overlay' },
    e('div', { className: 'modal-dialog modal-lg' },
      e('div', { className: 'modal-content card-ghost' },
        e('div', { className: 'modal-header' },
          e('h5', { className: 'modal-title' }, user ? 'Edit User' : 'Create User'),
          e('button', { className: 'btn btn-sm btn-outline-light', onClick: onClose }, '×')),
        e('form', { onSubmit: handleSubmit },
          e('div', { className: 'modal-body' },
            e('div', { className: 'row g-2' },
              e('div', { className: 'col-6 field' }, e('label', null, 'First Name'), e('input', { className: 'form-control form-control-sm', value: formData.firstName || '', onChange: ev => handleChange('firstName', ev.target.value), required: true })),
              e('div', { className: 'col-6 field' }, e('label', null, 'Last Name'), e('input', { className: 'form-control form-control-sm', value: formData.lastName || '', onChange: ev => handleChange('lastName', ev.target.value), required: true })),
              e('div', { className: 'col-6 field' }, e('label', null, 'Email'), e('input', { type: 'email', className: 'form-control form-control-sm', value: formData.email || '', onChange: ev => handleChange('email', ev.target.value), required: true })),
              e('div', { className: 'col-6 field' }, e('label', null, 'Job Title'), e('input', { className: 'form-control form-control-sm', value: formData.jobTitle || '', onChange: ev => handleChange('jobTitle', ev.target.value) })),
              e('div', { className: 'col-6 field' }, e('label', null, 'Phone Number'), e('input', { className: 'form-control form-control-sm', value: formData.phoneNumber || '', onChange: ev => handleChange('phoneNumber', ev.target.value) })),
              isSuperAdmin && e('div', { className: 'col-6 field d-flex align-items-end' },
                e('div', { className: 'form-check' },
                  e('input', { type: 'checkbox', className: 'form-check-input', id: 'superadmin', checked: formData.isSuperAdmin || false, onChange: ev => handleChange('isSuperAdmin', ev.target.checked) }),
                  e('label', { className: 'form-check-label', htmlFor: 'superadmin' }, ' Super Admin')))),
            e('div', { className: 'modal-footer' },
              e('button', { type: 'button', className: 'btn btn-sm btn-outline-light', onClick: onClose, disabled: loading }, 'Cancel'),
              e('button', { type: 'submit', className: 'btn btn-sm btn-accent', disabled: loading }, loading ? e('span', null, e('span', { className: 'spinner-border spinner-border-sm' }), ' Saving...') : (user ? 'Update User' : 'Create User'))))))))
}

// ===== ROLE MANAGEMENT PAGE =====
function RoleManagementPage() {
  const api = React.useMemo(() => ApiClient(), []); const { logout } = useAuth(); const { toast, ToastHost } = useToast();
  const [roles, setRoles] = React.useState([]);
  const [loading, setLoading] = React.useState(false);
  const [showCreateModal, setShowCreateModal] = React.useState(false);
  const [editingRole, setEditingRole] = React.useState(null);

  const load = React.useCallback(async () => {
    setLoading(true);
    try {
      const r = await api.get('/roles');
      if (r.status === 401) { logout(); return; }
      if (r.ok) setRoles(Array.isArray(r.data) ? r.data : []);
    } catch { }
    finally { setLoading(false); }
  }, [api, logout]);
  React.useEffect(() => { load(); }, [load]);

  const handleCreateRole = async (roleData) => {
    const r = await api.post('/roles', roleData);
    if (!r.ok) throw new Error((r.data && (r.data.title || r.data.detail)) || 'Create failed');
    toast('Role created', 'ok'); setShowCreateModal(false); await load();
  };
  const handleUpdateRole = async (roleId, roleData) => {
    const r = await api.put('/roles/' + roleId, roleData);
    if (!r.ok) throw new Error((r.data && (r.data.title || r.data.detail)) || 'Update failed');
    toast('Role updated', 'ok'); setEditingRole(null); await load();
  };

  const columns = [{ key: 'name', label: 'Name' }, { key: 'description', label: 'Description' }, { key: 'permissions', label: 'Permissions' }];
  const renderRow = (item) => e('tr', { key: item.id || item.name },
    e('td', null, item.name || '—'),
    e('td', null, item.description || '—'),
    e('td', null, e('span', { className: 'small-muted' }, Array.isArray(item.permissions) ? item.permissions.join(', ') : (item.permissions || '') || '—')),
    e('td', { className: 'actions-cell' }, e('button', { className: 'btn btn-sm btn-outline-light', onClick: () => setEditingRole(item) }, 'Edit')));

  const renderTable = (data) => e('div', { className: 'table-responsive' },
    e('table', { className: 'table table-sm table-hover align-middle' },
      e('thead', null, e('tr', null, columns.map(c => e('th', { key: c.key }, c.label)))),
      e('tbody', null, data.length === 0 ? e('tr', null, e('td', { colSpan: columns.length + 1, className: 'text-center small-muted py-4' }, 'No roles found')) : data.map(item => renderRow(item)))));

  return e('div', { className: 'module-page' },
    e('div', { className: 'd-flex justify-content-between align-items-center mb-3 flex-wrap gap-2' },
      e('div', { className: 'd-flex align-items-center gap-2' },
        e('a', { href: '#dashboard', className: 'btn btn-sm btn-outline-light' }, e('i', { className: 'bi bi-arrow-left' }), ' Dashboard'),
        e('h4', { className: 'mb-0 hero-title' }, '🔑 Role Management')),
      e('button', { className: 'btn btn-sm btn-accent', onClick: () => setShowCreateModal(true) }, e('i', { className: 'bi bi-plus-lg' }), ' Create Role')),
    (loading && roles.length === 0) ? e('div', { className: 'module-card card-ghost skeleton', style: { height: 200 } }) : renderTable(roles),
    showCreateModal && e(RoleModal, { onClose: () => setShowCreateModal(false), onSave: handleCreateRole }),
    editingRole && e(RoleModal, { role: editingRole, onClose: () => setEditingRole(null), onSave: (data) => handleUpdateRole(editingRole.id || editingRole.name, data) }),
    e(ToastHost)
  );
}

function RoleModal({ role, onClose, onSave }) {
  const [formData, setFormData] = React.useState(role || { name: '', description: '', permissions: [] });
  const [loading, setLoading] = React.useState(false);
  const [customPerm, setCustomPerm] = React.useState('');
  const { toast } = useToast();
  const handleChange = (field, value) => setFormData({ ...formData, [field]: value });
  const togglePerm = (perm) => {
    const perms = formData.permissions || [];
    if (perms.includes(perm)) handleChange('permissions', perms.filter(p => p !== perm));
    else handleChange('permissions', [...perms, perm]);
  };
  const addCustomPerm = () => { if (customPerm && !(formData.permissions || []).includes(customPerm)) { handleChange('permissions', [...(formData.permissions || []), customPerm]); setCustomPerm(''); } };
  const handleSubmit = async (ev) => {
    ev.preventDefault(); setLoading(true);
    try { await onSave({ name: formData.name, description: formData.description, permissions: formData.permissions }); toast(role ? 'Role updated' : 'Role created', 'ok'); onClose(); }
    catch (err) { toast(err.message || 'Save failed', 'err'); }
    finally { setLoading(false); }
  };
  return e('div', { className: 'detail-overlay' },
    e('div', { className: 'modal-dialog modal-lg' },
      e('div', { className: 'modal-content card-ghost' },
        e('div', { className: 'modal-header' },
          e('h5', { className: 'modal-title' }, role ? 'Edit Role' : 'Create Role'),
          e('button', { className: 'btn btn-sm btn-outline-light', onClick: onClose }, '×')),
        e('form', { onSubmit: handleSubmit },
          e('div', { className: 'modal-body' },
            e('div', { className: 'field' }, e('label', null, 'Name'), e('input', { className: 'form-control form-control-sm', value: formData.name || '', onChange: ev => handleChange('name', ev.target.value), required: true })),
            e('div', { className: 'field' }, e('label', null, 'Description'), e('textarea', { className: 'form-control form-control-sm', rows: 2, value: formData.description || '', onChange: ev => handleChange('description', ev.target.value) })),
            e('div', { className: 'field' }, e('label', null, 'Permissions'), e('input', { className: 'form-control form-control-sm', value: customPerm, onChange: ev => setCustomPerm(ev.target.value), placeholder: 'Add permission', onKeyDown: ev => { if (ev.key === 'Enter') { ev.preventDefault(); addCustomPerm(); } } }), e('button', { type: 'button', className: 'btn btn-sm btn-outline-light mt-1', onClick: addCustomPerm, disabled: !customPerm }, 'Add')),
            e('div', { className: 'mt-2' }, (formData.permissions || []).map(p => e('span', { key: p, className: 'badge-status', style: { color: '#38bdf8' } }, p))),
            e('div', { className: 'form-text small-muted mt-1' }, 'Click a permission to remove. Permissions are dot-separated strings like asset.create.')),
          e('div', { className: 'modal-footer' },
            e('button', { type: 'button', className: 'btn btn-sm btn-outline-light', onClick: onClose, disabled: loading }, 'Cancel'),
            e('button', { type: 'submit', className: 'btn btn-sm btn-accent', disabled: loading }, loading ? e('span', null, e('span', { className: 'spinner-border spinner-border-sm' }), ' Saving...') : (role ? 'Update Role' : 'Create Role')))))))
}

// ===== PROFILE SETTINGS PAGE =====
function ProfileSettingsPage() {
  const api = React.useMemo(() => ApiClient(), []); const { logout } = useAuth(); const { toast, ToastHost } = useToast();
  const [profile, setProfile] = React.useState(null);
  const [loading, setLoading] = React.useState(true);
  const [showPasswordModal, setShowPasswordModal] = React.useState(false);

  const load = React.useCallback(async () => {
    setLoading(true);
    try {
      const r = await api.get('/profile');
      if (r.status === 401) { logout(); return; }
      if (r.ok) setProfile(r.data);
    } catch { }
    finally { setLoading(false); }
  }, [api, logout]);
  React.useEffect(() => { load(); }, [load]);

  return e('div', { className: 'module-page' },
    e('div', { className: 'd-flex justify-content-between align-items-center mb-3 flex-wrap gap-2' },
      e('div', { className: 'd-flex align-items-center gap-2' },
        e('a', { href: '#dashboard', className: 'btn btn-sm btn-outline-light' }, e('i', { className: 'bi bi-arrow-left' }), ' Dashboard'),
        e('h4', { className: 'mb-0 hero-title' }, '⚙️ Settings')),
      e('button', { className: 'btn btn-sm btn-light', onClick: load, disabled: loading }, loading ? e('span', { className: 'spinner-border spinner-border-sm' }) : e('i', { className: 'bi bi-arrow-clockwise' }))),
    (loading && !profile) ? e('div', { className: 'module-card card-ghost skeleton', style: { height: 200 } }) :
    profile ? e('div', { className: 'card-ghost' },
      e('div', { className: 'detail-grid' },
        e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Full Name'), e('span', { className: 'detail-value' }, profile.fullName || profile.name || '—')),
        e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Email'), e('span', { className: 'detail-value' }, profile.email || '—')),
        e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Timezone'), e('span', { className: 'detail-value' }, profile.timezone || 'UTC')),
        e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Roles'), e('span', { className: 'detail-value' }, (profile.roles || []).join(', ') || '—')),
        e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Department'), e('span', { className: 'detail-value' }, profile.department || '—')),
        e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Position'), e('span', { className: 'detail-value' }, profile.position || '—'))),
      e('div', { className: 'mt-3' },
        e('button', { className: 'btn btn-sm btn-outline-light', onClick: () => setShowPasswordModal(true) }, e('i', { className: 'bi bi-key' }), 'Change Password'))) :
    e('div', { className: 'card-ghost' }, e('div', { className: 'empty-state' }, 'No profile data available.')),
    showPasswordModal && e(PasswordModal, { onClose: () => setShowPasswordModal(false) }),
    e(ToastHost)
  );
}

function PasswordModal({ onClose }) {
  const [newPassword, setNewPassword] = React.useState('');
  const [confirmPassword, setConfirmPassword] = React.useState('');
  const [loading, setLoading] = React.useState(false);
  const { toast, ToastHost } = useToast();
  const api = React.useMemo(() => ApiClient(), []);
  const handleSubmit = async (ev) => {
    ev.preventDefault();
    if (newPassword !== confirmPassword) { toast('Passwords do not match', 'err'); return; }
    setLoading(true);
    try {
      const r = await api.post('/profile/change-password', { currentPassword: '', newPassword });
      if (r.ok) { toast('Password changed', 'ok'); onClose(); } else { toast('Failed to change password', 'err'); }
    } catch { toast('Failed to change password', 'err'); }
    finally { setLoading(false); }
  };
  const timezones = ['America/New_York', 'America/Chicago', 'America/Denver', 'America/Los_Angeles', 'Europe/London', 'Europe/Paris', 'Asia/Tokyo', 'UTC'];
  return e('div', { className: 'detail-overlay' },
    e('div', { className: 'modal-dialog' },
      e('div', { className: 'modal-content card-ghost' },
        e('div', { className: 'modal-header' },
          e('h5', { className: 'modal-title' }, 'Change Password'),
          e('button', { className: 'btn btn-sm btn-outline-light', onClick: onClose }, '×')),
        e('form', { onSubmit: handleSubmit },
          e('div', { className: 'modal-body' },
            e('div', { className: 'field' }, e('label', { className: 'form-label' }, 'New Password'), e('input', { type: 'password', className: 'form-control', value: newPassword, onChange: ev => setNewPassword(ev.target.value), required: true })),
            e('div', { className: 'field' }, e('label', { className: 'form-label' }, 'Confirm New Password'), e('input', { type: 'password', className: 'form-control', value: confirmPassword, onChange: ev => setConfirmPassword(ev.target.value), required: true }))),
          e('div', { className: 'modal-footer' },
            e('button', { type: 'button', className: 'btn btn-sm btn-outline-light', onClick: onClose }, 'Cancel'),
            e('button', { type: 'submit', className: 'btn btn-sm btn-accent', disabled: loading }, loading ? e('span', null, e('span', { className: 'spinner-border spinner-border-sm' }), ' Saving...') : 'Save Password'))))),
    e(ToastHost)
  );
}

// ===== APPROVALS PAGE =====
function ApprovalsPage() {
  const api = React.useMemo(() => ApiClient(), []);
  const { logout } = useAuth(); const { toast, ToastHost } = useToast();
  const [tab, setTab] = React.useState('pending');
  const [items, setItems] = React.useState([]);
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState(null);
  const [actionLoading, setActionLoading] = React.useState(null);
  const [selectedApproval, setSelectedApproval] = React.useState(null);
  const [detailLoading, setDetailLoading] = React.useState(false);

  const load = React.useCallback(async () => {
    setLoading(true); setError(null);
    try {
      const endpoint = tab === 'pending' ? '/approvals' : '/approvals/completed';
      const r = await api.get(endpoint);
      if (r.status === 401) { logout(); return; }
      if (!r.ok) throw new Error((r.data && (r.data.title || r.data.detail)) || ('Failed to load ' + tab + ' approvals'));
      setItems(Array.isArray(r.data) ? r.data : []);
    } catch (err) { setError(err.message || 'Failed to load approvals'); }
    finally { setLoading(false); }
  }, [api, tab, logout]);

  React.useEffect(() => { load(); }, [load]);

  const handleDecide = React.useCallback(async (approvalId, approve, comment) => {
    setActionLoading(approvalId + (approve ? ':approve' : ':reject'));
    try {
      const r = await api.post('/approvals/' + approvalId + '/decide', { approve: approve, comment: comment || '' });
      if (r.status === 401) { logout(); return; }
      if (!r.ok) throw new Error((r.data && (r.data.title || r.data.detail)) || 'Action failed');
      toast(approve ? 'Approval step recorded' : 'Approval rejected', 'ok');
      await load();
      if (selectedApproval && selectedApproval.id === approvalId) setSelectedApproval(null);
    } catch (err) { toast(err.message || 'Action failed', 'err'); }
    finally { setActionLoading(null); }
  }, [api, load, toast, selectedApproval, logout]);

  const loadDetail = React.useCallback(async (id) => {
    setDetailLoading(id);
    try {
      const r = await api.get('/approvals/' + id);
      if (r.ok && r.data) setSelectedApproval(r.data);
    } catch (err) { toast('Failed to load approval detail', 'err'); }
    finally { setDetailLoading(null); }
  }, [api, toast]);

  const closeDetail = () => setSelectedApproval(null);

  const statusBadge = (status) => e('span', { className: 'badge-status', style: { color: STATUS_COLORS[status] || '#94a3b8' } }, status);

  const columns = [
    { key: 'id', label: 'ID' },
    { key: 'resourceType', label: 'Type' },
    { key: 'reason', label: 'Reason / Amount' },
    { key: 'requestedBy', label: 'Requester' },
    { key: 'workflowDefinitionId', label: 'Workflow' },
    { key: 'currentStep', label: 'Step' },
    { key: 'createdAt', label: 'Created' },
    { key: 'status', label: 'Status' }
  ];

  const renderRow = (item) => e('tr', { key: item.id },
    e('td', { className: 'mono-cell' }, item.id),
    e('td', null, prettifyKey(item.resourceType || '')),
    e('td', null, item.reason || '—'),
    e('td', null, item.requestedBy || '—'),
    e('td', null, item.workflowDefinitionId ? e('span', { className: 'small-muted' }, item.workflowDefinitionId) : e('span', { className: 'small-muted' }, 'Direct')),
    e('td', null, item.currentStep > 0 ? 'Step ' + item.currentStep : 'Direct'),
    e('td', null, formatValue(item.createdAt)),
    e('td', null, statusBadge(item.status)),
    e('td', { className: 'actions-cell' },
      tab === 'pending' && item.status === 'pending' ? e('div', { className: 'btn-group btn-group-sm' },
        e('button', { className: 'btn btn-sm btn-outline-success', disabled: actionLoading === item.id + ':approve', onClick: () => handleDecide(item.id, true, '') },
          actionLoading === item.id + ':approve' ? e('span', { className: 'spinner-border spinner-border-sm' }) : 'Approve'),
        e('button', { className: 'btn btn-sm btn-outline-danger', disabled: actionLoading === item.id + ':reject', onClick: () => handleDecide(item.id, false, 'Rejected by reviewer') },
          actionLoading === item.id + ':reject' ? e('span', { className: 'spinner-border spinner-border-sm' }) : 'Reject')) : e('button', { className: 'btn btn-sm btn-outline-light', disabled: detailLoading === item.id, onClick: () => loadDetail(item.id) },
          detailLoading === item.id ? e('span', { className: 'spinner-border spinner-border-sm' }) : 'View')));

  const renderTable = (data) => e('div', { className: 'table-responsive' },
    e('table', { className: 'table table-sm table-hover align-middle' },
      e('thead', null, e('tr', null,
        columns.map(c => e('th', { key: c.key }, c.label)))),
      e('tbody', null,
        data.length === 0
          ? e('tr', null, e('td', { colSpan: columns.length + 1, className: 'text-center small-muted py-4' }, 'No records in this tab'))
          : data.map(item => renderRow(item)))));

  const detailPanel = selectedApproval ? e('div', { className: 'detail-overlay' },
    e('div', { className: 'detail-panel card-ghost' },
      e('div', { className: 'd-flex justify-content-between align-items-center mb-3' },
        e('h5', { className: 'mb-0' }, 'Approval Details'),
        e('button', { className: 'btn btn-sm btn-outline-light', onClick: closeDetail }, '×')),
      e('div', { className: 'detail-grid' },
        e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'ID'), e('span', { className: 'detail-value mono-cell' }, selectedApproval.id)),
        e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Status'), e('span', { className: 'detail-value' }, statusBadge(selectedApproval.status))),
        e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Resource'), e('span', { className: 'detail-value' }, selectedApproval.resourceType || '')),
        e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Resource ID'), e('span', { className: 'detail-value mono-cell' }, selectedApproval.resourceId || '—')),
        e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Reason'), e('span', { className: 'detail-value' }, selectedApproval.reason || '—')),
        e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Requester'), e('span', { className: 'detail-value' }, selectedApproval.requestedBy || '—')),
        e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Workflow'), e('span', { className: 'detail-value' }, selectedApproval.workflowName || 'Direct (no workflow)')),
        e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Current Step'), e('span', { className: 'detail-value' }, selectedApproval.currentStep > 0 ? ('Step ' + selectedApproval.currentStep + ' of ' + (selectedApproval.stepApprovers || []).length + ' — ' + (selectedApproval.currentStepApproverType || '')) : 'Direct approval')),
        e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Created'), e('span', { className: 'detail-value' }, formatValue(selectedApproval.createdAt))),
        e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Decided'), e('span', { className: 'detail-value' }, formatValue(selectedApproval.decidedAt))),
        e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Decided By'), e('span', { className: 'detail-value' }, selectedApproval.decidedBy || '—')),
        e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Comment'), e('span', { className: 'detail-value' }, selectedApproval.decisionComment || '—')),
      (selectedApproval.stepApprovers || []).length > 0 ? e('div', null,
        e('h6', { className: 'mt-3' }, 'Workflow Steps'),
        e('table', { className: 'table table-sm table-bordered' },
          e('thead', null, e('tr', null, e('th', null, 'Step'), e('th', null, 'Approver Type'), e('th', null, 'Approver Value'), e('th', null, 'Can Skip'))),
          e('tbody', null, (selectedApproval.stepApprovers || []).map((s, i) => e('tr', { key: s.stepNumber || i },
            e('td', null, s.stepNumber),
            e('td', null, prettifyKey(s.approverType)),
            e('td', null, s.approverValue),
            e('td', null, s.canSkip ? e('span', { className: 'badge-status', style: { color: '#6ee7b7' } }, 'Yes') : e('span', { className: 'small-muted' }, 'No'))))))) : null,
      (selectedApproval.decisions || []).length > 0 ? e('div', null,
        e('h6', { className: 'mt-3' }, 'Decision History'),
        e('table', { className: 'table table-sm table-bordered' },
          e('thead', null, e('tr', null, e('th', null, 'Step'), e('th', null, 'Approver'), e('th', null, 'Decision'), e('th', null, 'Comment'), e('th', null, 'Date'))),
          e('tbody', null, (selectedApproval.decisions || []).map(d => e('tr', { key: d.id },
            e('td', null, d.stepNumber),
            e('td', null, d.approverUserId),
            e('td', null, statusBadge(d.decision)),
            e('td', null, d.comment || '—'),
            e('td', null, formatValue(d.createdAt))))))) : null),
    e('div', null,
      tab === 'pending' && selectedApproval && selectedApproval.status === 'pending' && selectedApproval.currentStep > 0 ? e('div', { className: 'd-flex gap-2 mt-3' },
        e('button', { className: 'btn btn-sm btn-outline-success', disabled: actionLoading === selectedApproval.id + ':approve', onClick: () => handleDecide(selectedApproval.id, true, '') },
          actionLoading === selectedApproval.id + ':approve' ? e('span', { className: 'spinner-border spinner-border-sm' }) : 'Approve Step'),
        e('button', { className: 'btn btn-sm btn-outline-danger', disabled: actionLoading === selectedApproval.id + ':reject', onClick: () => handleDecide(selectedApproval.id, false, 'Rejected') },
          actionLoading === selectedApproval.id + ':reject' ? e('span', { className: 'spinner-border spinner-border-sm' }) : 'Reject Step')) : null))) : null;
  return e('div', { className: 'module-page' },
    e('div', { className: 'd-flex justify-content-between align-items-center mb-3 flex-wrap gap-2' },
      e('div', { className: 'd-flex align-items-center gap-2' },
        e('a', { href: '#dashboard', className: 'btn btn-sm btn-outline-light' }, e('i', { className: 'bi bi-arrow-left' }), ' Dashboard'),
        e('h4', { className: 'mb-0 hero-title' }, '✅ Approvals')),
      e('button', { className: 'btn btn-sm btn-light', onClick: load, disabled: loading }, loading ? e('span', { className: 'spinner-border spinner-border-sm' }) : e('i', { className: 'bi bi-arrow-clockwise' }))),
    e('ul', { className: 'nav nav-tabs mb-3' },
      e('li', { className: 'nav-item' }, e('button', { className: 'nav-link ' + (tab === 'pending' ? 'active' : ''), onClick: () => setTab('pending') }, 'Pending')),
      e('li', { className: 'nav-item' }, e('button', { className: 'nav-link ' + (tab === 'completed' ? 'active' : ''), onClick: () => setTab('completed') }, 'Completed'))),
    error ? e('div', { className: 'alert alert-danger' }, 'Error: ' + error) : null,
    (loading && items.length === 0) ? e('div', { className: 'module-card card-ghost skeleton', style: { height: 200 } }) : renderTable(items),
    e('div', { className: 'small-muted mt-2' }, tab === 'pending'
      ? 'Showing approvals assigned to your role or user at the current workflow step. You can approve or reject items here.'
      : 'Showing approvals you requested, decided, or participated in.'),
    detailPanel,
    e(ToastHost)
  );
}

// ===== WORKFLOW SETUP PAGE =====
function WorkflowSetupPage() {
  const api = React.useMemo(() => ApiClient(), []);
  const { logout } = useAuth(); const { toast, ToastHost } = useToast();
  const [workflows, setWorkflows] = React.useState([]);
  const [loading, setLoading] = React.useState(false);
  const [saving, setSaving] = React.useState(false);
  const [error, setError] = React.useState(null);
  const [editing, setEditing] = React.useState(null);
  const [resourceTypes, setResourceTypes] = React.useState([]);
  const [rolesWithUsers, setRolesWithUsers] = React.useState([]);

  const load = React.useCallback(async () => {
    setLoading(true); setError(null);
    try {
      const [wfRes, rtRes, rwRes] = await Promise.all([
        api.get('/workflows'),
        api.get('/workflows/resource-types'),
        api.get('/workflows/roles-with-users')
      ]);
      if (wfRes.status === 401) { logout(); return; }
      if (!wfRes.ok) throw new Error((wfRes.data && (wfRes.data.title || wfRes.data.detail)) || 'Failed to load workflows');
      setWorkflows(Array.isArray(wfRes.data) ? wfRes.data : []);
      if (rtRes.ok && rtRes.data && rtRes.data.resourceTypes) setResourceTypes(rtRes.data.resourceTypes);
      if (rwRes.ok && rwRes.data && rwRes.data.roles) setRolesWithUsers(rwRes.data.roles);
    } catch (err) { setError(err.message || 'Failed to load workflows'); }
    finally { setLoading(false); }
  }, [api, toast, logout]);

  React.useEffect(() => { load(); }, [load]);

  const handleEdit = (wf) => {
    console.log('handleEdit called for workflow:', wf.id);
    api.get('/workflows/' + wf.id).then(r => {
      if (r.ok && r.data) {
        const wfData = r.data;
        const parts = (wfData.resourceType || '').split('.');
        const module = parts[0] || '';
        const action = parts[1] || '';
        const steps = (wfData.steps || []).map(s => {
          var selectedRoleId = '';
          var selectedUserId = 'any';
          if (s.approverType === 'role') {
            selectedRoleId = s.approverValue || '';
          } else if (s.approverType === 'user') {
            const role = rolesWithUsers.find(rw => rw.users && rw.users.some(u => u.userId === s.approverValue));
            selectedRoleId = role ? role.name : '';
            selectedUserId = s.approverValue || 'any';
          }
          return {
            id: s.id, workflowDefinitionId: s.workflowDefinitionId, stepNumber: s.stepNumber,
            approverType: s.approverType, approverValue: s.approverValue, canSkip: s.canSkip || false,
            selectedRoleId, selectedUserId
          };
        });
        setEditing({
          id: wfData.id, name: wfData.name, description: wfData.description || '',
          resourceType: wfData.resourceType || '', module, action,
          triggerType: wfData.triggerType || 'always', triggerAmount: wfData.triggerAmount || null,
          triggerQuantity: wfData.triggerQuantity || null, isActive: wfData.isActive === true,
          createdAt: wfData.createdAt, updatedAt: wfData.updatedAt,
          steps
        });
        console.log('editing state set for edit');
      } else toast('Failed to load workflow details', 'err');
    }).catch(() => toast('Failed to load workflow details', 'err'));
  };

  const handleNew = () => {
    console.log('handleNew called');
    setEditing({
      name: '', description: '', resourceType: '', module: '', action: '',
      triggerType: 'always', triggerAmount: null, triggerQuantity: null,
      isActive: true, steps: []
    });
    console.log('editing state set');
  };

  const handleDelete = React.useCallback(async (id) => {
    if (!confirm('Delete this workflow? This cannot be undone.')) return;
    const r = await api.del('/workflows/' + id);
    if (r.ok) { toast('Workflow deleted', 'ok'); await load(); }
    else toast((r.data && r.data.detail) || 'Delete failed', 'err');
  }, [api, load, toast]);

  const addStep = () => {
    setEditing({
      ...editing,
      steps: [...(editing.steps || []), {
        stepNumber: (editing.steps || []).length + 1,
        selectedRoleId: '', selectedUserId: 'any',
        approverType: 'role', approverValue: '', canSkip: false
      }]
    });
  };

  const updateStepCanSkip = (idx, canSkip) => {
    const steps = [...(editing.steps || [])];
    steps[idx].canSkip = canSkip;
    setEditing({ ...editing, steps });
  };

  const updateStepRole = (idx, roleName) => {
    const steps = [...(editing.steps || [])];
    steps[idx].selectedRoleId = roleName;
    steps[idx].selectedUserId = 'any';
    steps[idx].approverType = 'role';
    steps[idx].approverValue = roleName;
    setEditing({ ...editing, steps });
  };

  const updateStepUser = (idx, userId) => {
    const steps = [...(editing.steps || [])];
    steps[idx].selectedUserId = userId;
    if (userId === 'any') {
      steps[idx].approverType = 'role';
      steps[idx].approverValue = steps[idx].selectedRoleId || '';
    } else {
      steps[idx].approverType = 'user';
      steps[idx].approverValue = userId;
    }
    setEditing({ ...editing, steps });
  };

  const removeStep = (idx) => {
    const steps = [...(editing.steps || [])].filter((_, i) => i !== idx).map((s, i) => ({ ...s, stepNumber: i + 1 }));
    setEditing({ ...editing, steps });
  };

  const handleSave = React.useCallback(async () => {
    setSaving(true);
    try {
      const isNew = !editing.id;
      var resourceType = editing.resourceType || '';
      if (editing.module && editing.action) resourceType = editing.module + '.' + editing.action;
      const payload = {
        name: editing.name,
        resourceType: resourceType,
        triggerType: editing.triggerType,
        triggerAmount: editing.triggerType === 'amount' ? (Number(editing.triggerAmount) || 0) : null,
        triggerQuantity: editing.triggerType === 'quantity' ? (Number(editing.triggerQuantity) || 0) : null,
        isActive: editing.isActive,
        description: editing.description || '',
        steps: (editing.steps || []).map(s => ({
          approverType: s.approverType,
          approverValue: s.approverValue,
          canSkip: s.canSkip === true
        }))
      };
      const r = isNew ? await api.post('/workflows', payload) : await api.put('/workflows/' + editing.id, payload);
      if (r.status === 401) { logout(); return; }
      if (!r.ok) throw new Error((r.data && (r.data.title || r.data.detail)) || 'Save failed');
      toast(isNew ? 'Workflow created' : 'Workflow updated', 'ok');
      setEditing(null);
      await load();
    } catch (err) { toast(err.message || 'Save failed', 'err'); }
    finally { setSaving(false); }
  }, [api, editing, load, toast, logout]);

  const closeEditor = () => setEditing(null);
  const selectedModuleObj = editing && resourceTypes.find(rt => rt.module === editing.module);
  const availableActions = selectedModuleObj ? selectedModuleObj.actions : [];

  return e('div', { className: 'module-page' },
    e('div', { className: 'd-flex justify-content-between align-items-center mb-3 flex-wrap gap-2' },
      e('div', { className: 'd-flex align-items-center gap-2' },
        e('a', { href: '#dashboard', className: 'btn btn-sm btn-outline-light' }, e('i', { className: 'bi bi-arrow-left' }), ' Dashboard'),
        e('h4', { className: 'mb-0 hero-title' }, '⚙️ Workflow Setup')),
      e('button', { type: 'button', className: 'btn btn-sm btn-accent', onClick: () => { console.log('New Workflow button clicked'); handleNew(); } }, e('i', { className: 'bi bi-plus-lg' }), ' New Workflow')),
    error ? e('div', { className: 'alert alert-danger' }, 'Error: ' + error) : null,
    (loading && workflows.length === 0) ? e('div', { className: 'module-card card-ghost skeleton', style: { height: 200 } })
      : workflows.length === 0
        ? e('div', { className: 'card-ghost' }, e('div', { className: 'empty-state' }, 'No workflows configured. Click "New Workflow" to create one.'))
        : e('div', { className: 'table-responsive' },
          e('table', { className: 'table table-sm table-hover align-middle' },
            e('thead', null, e('tr', null,
              e('th', null, 'Name'), e('th', null, 'Resource Type'), e('th', null, 'Trigger'), e('th', null, 'Active'), e('th', null, 'Steps'), e('th', null, 'Actions'))),
            e('tbody', null, workflows.map(wf => e('tr', { key: wf.id },
              e('td', null, wf.name),
              e('td', null, e('code', null, wf.resourceType)),
              e('td', null, wf.triggerType === 'amount' ? ('Amount >= ' + (wf.triggerAmount || 0)) : wf.triggerType === 'quantity' ? ('Qty >= ' + (wf.triggerQuantity || 0)) : 'Always'),
              e('td', null, wf.isActive ? e('span', { className: 'badge-status', style: { color: '#6ee7b7' } }, 'Active') : e('span', { className: 'badge-status', style: { color: '#94a3b8' } }, 'Inactive')),
              e('td', null, e('span', { className: 'small-muted' }, (wf.steps || []).length + ' step(s)')),
              e('td', { className: 'actions-cell' },
                e('div', { className: 'btn-group btn-group-sm' },
                  e('button', { className: 'btn btn-sm btn-outline-light', onClick: () => handleEdit(wf) }, 'Edit'),
                  e('button', { className: 'btn btn-sm btn-outline-danger', onClick: () => handleDelete(wf.id) }, 'Delete')))))))),
    editing ? e('div', { className: 'detail-overlay' },
      e('div', { className: 'detail-panel card-ghost' },
        e('div', { className: 'd-flex justify-content-between align-items-center mb-3' },
          e('h5', { className: 'mb-0' }, editing.id ? 'Edit Workflow' : 'New Workflow'),
          e('button', { className: 'btn btn-sm btn-outline-light', onClick: closeEditor, disabled: saving }, '×')),
        e('div', { className: 'detail-grid' },
          e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Name'), e('span', { className: 'detail-value' },
            e('input', { type: 'text', className: 'form-control form-control-sm', value: editing.name || '', onChange: ev => setEditing({ ...editing, name: ev.target.value }) }))),
          e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Module'), e('span', { className: 'detail-value' },
            e('select', { className: 'form-control form-control-sm', value: editing.module || '', onChange: ev => setEditing({ ...editing, module: ev.target.value, action: '', resourceType: '' }) },
              e('option', { value: '' }, 'Select a module'),
              resourceTypes.map(rt => e('option', { key: rt.module, value: rt.module }, rt.label))))),
          e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Action'), e('span', { className: 'detail-value' },
            e('select', { className: 'form-control form-control-sm', value: editing.action || '', onChange: ev => setEditing({ ...editing, action: ev.target.value, resourceType: (editing.module || '') + '.' + ev.target.value }) },
              e('option', { value: '' }, editing.module ? 'Select an action' : 'Select a module first'),
              availableActions.map((act, i) => e('option', { key: i, value: act }, act))))),
          e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Trigger'), e('span', { className: 'detail-value' },
            e('select', { className: 'form-control form-control-sm', value: editing.triggerType || 'always', onChange: ev => setEditing({ ...editing, triggerType: ev.target.value }) },
              e('option', { value: 'always' }, 'Always'),
              e('option', { value: 'amount' }, 'Amount threshold'),
              e('option', { value: 'quantity' }, 'Quantity threshold')))),
          editing.triggerType === 'amount' && e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Amount >= '), e('span', { className: 'detail-value' },
            e('input', { type: 'number', className: 'form-control form-control-sm', value: editing.triggerAmount || '', onChange: ev => setEditing({ ...editing, triggerAmount: Number(ev.target.value) }) }))),
          editing.triggerType === 'quantity' && e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Qty >= '), e('span', { className: 'detail-value' },
            e('input', { type: 'number', className: 'form-control form-control-sm', value: editing.triggerQuantity || '', onChange: ev => setEditing({ ...editing, triggerQuantity: Number(ev.target.value) }) }))),
          e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Active'), e('span', { className: 'detail-value' },
            e('select', { className: 'form-control form-control-sm', value: editing.isActive ? 'true' : 'false', onChange: ev => setEditing({ ...editing, isActive: ev.target.value === 'true' }) },
              e('option', { value: 'true' }, 'Yes'), e('option', { value: 'false' }, 'No')))),
          e('div', { className: 'detail-row' }, e('span', { className: 'detail-label' }, 'Description'), e('span', { className: 'detail-value' },
            e('textarea', { className: 'form-control form-control-sm', rows: 2, value: editing.description || '', onChange: ev => setEditing({ ...editing, description: ev.target.value }) }))),
        e('div', null,
          e('h6', { className: 'mt-3' }, 'Steps (approvers in sequence)'),
          e('table', { className: 'table table-sm table-bordered' },
            e('thead', null, e('tr', null, e('th', null, '#'), e('th', null, 'Role'), e('th', null, 'Approver'), e('th', null, 'Can Skip'), e('th', null, 'Actions'))),
            e('tbody', null,
              (editing.steps || []).map((s, i) => e('tr', { key: i },
                e('td', null, e('span', { className: 'small-muted' }, s.stepNumber || i + 1)),
                e('td', null, e('select', { className: 'form-control form-control-sm', value: s.selectedRoleId || '', onChange: ev => updateStepRole(i, ev.target.value), disabled: saving },
                  e('option', { value: '' }, 'Select a role'),
                  rolesWithUsers.map(rw => e('option', { key: rw.id, value: rw.name }, rw.name + ' (' + (rw.users ? rw.users.length : 0) + ')')))),
                e('td', null, s.selectedRoleId ? e('select', { className: 'form-control form-control-sm', value: s.selectedUserId || 'any', onChange: ev => updateStepUser(i, ev.target.value), disabled: saving },
                  e('option', { value: 'any' }, 'Any user (all users in role can approve)'),
                  (rolesWithUsers.find(rw => rw.name === s.selectedRoleId) || { users: [] }).users.map(u => e('option', { key: u.userId, value: u.userId }, u.firstName + ' ' + u.lastName + ' (' + u.email + ')'))) : e('span', { className: 'small-muted' }, '—')),
                e('td', null, e('div', { className: 'form-check' },
                  e('input', { type: 'checkbox', className: 'form-check-input', id: 'canskip-' + i, checked: s.canSkip === true, onChange: ev => updateStepCanSkip(i, ev.target.checked), disabled: saving }),
                  e('label', { className: 'form-check-label small-muted', htmlFor: 'canskip-' + i }, 'Can skip'))),
                e('td', null, e('button', { className: 'btn btn-sm btn-outline-danger', onClick: () => removeStep(i), disabled: saving }, '×')))
              ))),
          e('button', { className: 'btn btn-sm btn-outline-light mt-2', onClick: addStep, disabled: saving }, '+ Add Step')),
        e('div', { className: 'd-flex justify-content-end gap-2 mt-3' },
          e('button', { className: 'btn btn-sm btn-outline-light', onClick: closeEditor, disabled: saving }, 'Cancel'),
          e('button', { className: 'btn btn-sm btn-accent', onClick: handleSave, disabled: saving }, editing.id ? "Update Workflow" : "Save Workflow"))))) : null,
    e(ToastHost));
}

// ===== LAYOUT: Sidebar + Router =====

const CATEGORY_ICONS = {
  'Assets Management': '📦', 'Procurement': '🛒', 'Workflow Management': '✅',
  'HR Management': '👥', 'Finance': '💰', 'POS': '💳',
  'Reporting': '📈', 'System': '⚙️'
};

function Sidebar({ user, activeKey, onNavigate, onLogout }) {
  const [openCategory, setOpenCategory] = React.useState(null);
  const toggleCategory = (cat) => setOpenCategory(prev => (prev === cat ? null : cat));

  React.useEffect(() => {
    const activeModule = MODULES.find(m => m.key === activeKey);
    if (activeModule && activeModule.category && !openCategory) {
      setOpenCategory(activeModule.category);
    }
  }, [activeKey]);

  const systemModules = MODULES.filter(m => m.category === 'System');
  const categoryModules = MODULES.filter(m => m.category !== 'System');
  const grouped = [];
  const categoriesInUse = CATEGORY_ORDER.filter(cat => cat !== 'System');
  categoriesInUse.forEach(cat => {
    const mods = categoryModules.filter(m => m.category === cat);
    if (mods.length > 0) grouped.push({ category: cat, items: mods });
  });

  return e('nav', { className: 'sidebar' },
    e('div', { className: 'sidebar-header' },
      e('div', { className: 'd-flex align-items-center gap-2' },
        e('span', { className: 'avatar avatar-md' }, user?.username?.[0]?.toUpperCase() || '?'),
        e('div', null,
          e('div', { className: 'sidebar-user-name' }, user?.username || 'user'),
          e('div', { className: 'sidebar-user-role small-muted' }, user?.isSuperAdmin ? 'Super Admin' : (user?.roles || []).join(', ') || 'User')))),
    e('div', { className: 'sidebar-nav' },
      e('div', { key: 'dashboard-group' },
        e('button', { key: 'dashboard', className: 'sidebar-nav-item btn btn-link text-start w-100 sidebar-nav-item--first', 'aria-current': activeKey === 'dashboard' ? 'page' : undefined, onClick: () => onNavigate('dashboard') },
          e('span', { className: 'sidebar-nav-icon' }, '📊'), ' Dashboard')),
      grouped.map(g => {
        const isOpen = openCategory === g.category;
        const hasActive = g.items.some(it => it.key === activeKey);
        const catIcon = CATEGORY_ICONS[g.category] || '📁';
        return e('div', { key: 'cat-' + g.category, className: 'sidebar-nav-category' },
          e('button', {
            key: 'header-' + g.category, className: 'sidebar-nav-category-header btn btn-link text-start w-100',
            'aria-expanded': isOpen, onClick: () => toggleCategory(g.category)
          },
            e('span', { className: 'sidebar-nav-category-icon' }, catIcon),
            e('span', { className: 'sidebar-nav-category-label' }, g.category),
            e('span', { className: 'sidebar-nav-category-toggle', style: { transform: isOpen ? 'rotate(90deg)' : 'rotate(0deg)' } }, '▶'),
            hasActive && !isOpen ? e('span', { className: 'sidebar-nav-dot' }, '●') : null),
          isOpen && g.items.map(it => {
            const active = activeKey === it.key;
            return e('button', { key: it.key, className: 'sidebar-nav-item btn btn-link text-start w-100 sidebar-nav-item--child', 'aria-current': active ? 'page' : undefined, onClick: () => onNavigate(it.key) },
              e('span', { className: 'sidebar-nav-icon' }, it.icon), ' ' + it.label);
          }));
      }),
      systemModules.length > 0 && e('div', { key: 'system-group', className: 'sidebar-nav-category' },
        e('button', {
          key: 'header-system', className: 'sidebar-nav-category-header btn btn-link text-start w-100',
          'aria-expanded': openCategory === 'System', onClick: () => toggleCategory('System')
        },
          e('span', { className: 'sidebar-nav-category-icon' }, CATEGORY_ICONS['System'] || '⚙️'),
          e('span', { className: 'sidebar-nav-category-label' }, 'System'),
          e('span', { className: 'sidebar-nav-category-toggle', style: { transform: openCategory === 'System' ? 'rotate(90deg)' : 'rotate(0deg)' } }, '▶')),
        openCategory === 'System' && systemModules.map(it => {
          const active = activeKey === it.key;
          return e('button', { key: it.key, className: 'sidebar-nav-item btn btn-link text-start w-100 sidebar-nav-item--child', 'aria-current': active ? 'page' : undefined, onClick: () => onNavigate(it.key) },
            e('span', { className: 'sidebar-nav-icon' }, it.icon), ' ' + it.label);
        }))),
    e('div', { className: 'sidebar-footer' }, e('button', { className: 'btn btn-sm btn-outline-light w-100', onClick: onLogout }, e('i', { className: 'bi bi-box-arrow-right' }), ' Logout')));
}

function parseRoute(hash) {
  const h = (hash || '').slice(1) || 'dashboard';
  if (h === '' || h === 'login') return { page: 'login' };
  if (h === 'approvals') return { page: 'approvals' };
  if (h === 'workflow-setup') return { page: 'workflow-setup' };
  const mod = MODULES.find(m => m.path === h);
  if (mod) return { page: 'module', moduleKey: mod.key };
  if (h === 'dashboard') return { page: 'dashboard' };
  return { page: 'dashboard' };
}

function AppContent() {
  const { user, login, logout } = useAuth();
  const [route, setRoute] = React.useState(() => parseRoute(window.location.hash));
  const { toast, ToastHost } = useToast();

  React.useEffect(() => {
    const onHash = () => setRoute(parseRoute(window.location.hash));
    window.addEventListener('hashchange', onHash);
    return () => window.removeEventListener('hashchange', onHash);
  }, []);

  const navigate = (key) => {
    const mod = MODULES.find(m => m.key === key);
    window.location.hash = mod ? '#' + mod.path : (key === 'dashboard' ? '#dashboard' : '#' + key);
  };
  const activeKey = route.page === 'module' ? route.moduleKey : 'dashboard';

  React.useEffect(() => {
    if (user) filterModulesByAccess(user.accessibleModules || user.modules || []);
  }, [user]);

  let pageElement;
  if (!user) pageElement = e(LoginPage, { onLogin: login });
  else if (route.page === 'approvals') pageElement = e(ApprovalsPage, { key: 'approvals-page' });
  else if (route.page === 'workflow-setup') pageElement = e(WorkflowSetupPage, { key: 'workflow-setup-page' });
  else if (route.page === 'module') {
    const mk = route.moduleKey;
    if (mk === 'users') pageElement = e(UserManagementPage, { key: 'users-page' });
    else if (mk === 'roles') pageElement = e(RoleManagementPage, { key: 'roles-page' });
    else if (mk === 'settings') pageElement = e(ProfileSettingsPage, { key: 'settings-page' });
    else pageElement = e(ModulePage, { key: mk + '-page', moduleKey: mk });
  }
  else pageElement = e(DashboardPage, { key: 'dashboard-page' });

  return e('div', { className: 'dashboard-layout' },
    user && e(Sidebar, { user, activeKey, onNavigate: navigate, onLogout: logout }),
    e('main', { className: 'main-content' },
      e('div', { className: 'app-shell' }, pageElement)),
    e(ToastHost)
  );
}

function App() {
  return e(AuthProvider, null, e(AppContent));
}

ReactDOM.createRoot(document.getElementById('app')).render(e(App));
