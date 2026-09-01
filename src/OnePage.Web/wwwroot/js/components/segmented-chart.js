import { e } from '../utils.js';

// ===== Chart from audit events (line chart) =====
function ChartFromEvents({ events }) {
  const canvasRef = React.useRef(null);
  React.useEffect(() => {
    const ctx = canvasRef.current;
    if (!ctx) return;
    if (ctx.chartInstance) ctx.chartInstance.destroy();
    const hourly = {};
    for (let h = 0; h < 24; h++) hourly[h] = 0;
    events.forEach(ev => { const d = new Date(ev.createdAt || ''); if (!isNaN(d.getTime())) hourly[d.getHours()]++; });
    ctx.chartInstance = new Chart(ctx, {
      type: 'line',
      data: { labels: Object.keys(hourly).map(h => h + 'h'), datasets: [{ label: 'Events', data: Object.values(hourly), borderColor: '#38bdf8', backgroundColor: 'rgba(56, 189, 248, .15)', fill: true, tension: .3 }] },
      options: { plugins: { legend: { display: false } }, responsive: true, maintainHeight: false, scales: { y: { ticks: { color: '#94a3b8' }, grid: { color: 'rgba(255,255,255,.06)' } }, x: { ticks: { color: '#94a3b8' }, grid: { display: false } } } }
    });
    return () => { if (ctx.chartInstance) ctx.chartInstance.destroy(); ctx.chartInstance = null; };
  }, [events]);
  return e('canvas', { ref: canvasRef, height: 130 });
}

// ===== Segmented Bar Chart (per-module analytics from /dashboard) =====
function SegmentedChart({ segments, money }) {
  const canvasRef = React.useRef(null);
  React.useEffect(() => {
    const ctx = canvasRef.current;
    if (!ctx) return;
    if (ctx.chartInstance) ctx.chartInstance.destroy();
    const labels = (segments || []).map(x => x.label);
    const dataValues = (segments || []).map(x => Number(x.value) || 0);
    ctx.chartInstance = new Chart(ctx, {
      type: 'bar',
      data: { labels, datasets: [{ label: 'Value', data: dataValues, backgroundColor: 'rgba(56, 189, 248, .42)', borderColor: '#38bdf8', borderWidth: 1 }] },
      options: { plugin: { legend: { display: false } }, responsive: true, maintainHeight: false, scales: { y: { ticks: { color: '#94a3b8' }, grid: { color: 'rgba(255,255,255,.06)' } }, x: { ticks: { color: '#94a3b8', maxRotation: 45 }, grid: { display: false } } } }
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

export { ChartFromEvents, SegmentedChart, AnalyticsChartCard };
