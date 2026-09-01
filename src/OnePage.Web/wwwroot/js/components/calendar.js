import { e, STATUS_COLORS } from '../utils.js';

function CalendarGrid({ month, year, events }) {
  const firstDay = new Date(year, month, 1);
  const startDay = firstDay.getDay();
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  const days = [];
  for (let i = 0; i < startDay; i++) days.push({ day: null, current: false });
  for (let d = 1; d <= daysInMonth; d++) {
    const date = new Date(year, month, d);
    const dayEvents = events.filter(ev => { const ed = new Date(ev.createdAt || ''); return ed && !isNaN(ed.getTime()) && ed.toDateString() === date.toDateString(); });
    days.push({ day: d, current: true, events: dayEvents });
  }
  const today = new Date();
  return e('div', { className: 'calendar-grid' },
    ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].map(d => e('div', { key: d, className: 'calendar-weekday' }, d)),
    days.map((d, i) => e('div', {
      key: i, className: 'calendar-day ' + (d.current && d.day === today.getDate() && month === today.getMonth() && year === today.getFullYear() ? 'today' : '') + (d.events && d.events.length > 0 ? ' has-events' : '') + (!d.current ? ' out-of-month' : ''),
      style: { height: 44 }
    }, d.current ? d.day : ''))
  );
}

function EventBadge({ event }) {
  return e('div', { className: 'event-badge' },
    e('span', { className: 'event-dot', style: { color: STATUS_COLORS[event.action ? 'approved' : 'pending'] || '#94a3b8' } }, '●'),
    e('div', { className: 'event-badge-body' },
      e('div', { className: 'event-title' }, event.action || event.resourceType || 'Event'),
      e('div', { className: 'event-desc' }, event.resourceId || event.detail || ''),
      e('div', { className: 'event-meta' },
        e('span', { className: 'event-type' }, event.userId ? ('User: ' + event.userId) : 'System'),
        e('span', { className: 'event-status' }, event.createdAt ? new Date(event.createdAt).toLocaleTimeString() : ''))));
}

export { CalendarGrid, EventBadge };
