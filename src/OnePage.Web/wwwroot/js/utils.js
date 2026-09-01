const e = React.createElement;

const formatMoney = (n) => new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD', minimumFractionDigits: 0, maximumFractionDigits: 2 }).format(n || 0);
const formatNum = (n) => new Intl.NumberFormat(undefined, { maximumFractionDigits: 2 }).format(n || 0);
const STATUS_COLORS = { pending: '#fbbf24', approved: '#6ee7b7', rejected: '#f87171' };

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

export { e, formatMoney, formatNum, STATUS_COLORS, prettifyKey, formatValue, getStoredUser };
