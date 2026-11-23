const minute = 60_000;
const hour = 60 * minute;
const day = 24 * hour;
const koreaOffset = 9 * hour;
const dateOnly = /^\d{4}-\d{2}-\d{2}$/;
const relative = new Intl.RelativeTimeFormat('ko-KR', {numeric: 'always'});
const exact = new Intl.DateTimeFormat('sv-SE', {
 timeZone: 'Asia/Seoul', year: 'numeric', month: '2-digit', day: '2-digit',
 hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23'
});
const units = [
 ['year', 365 * day], ['month', 30 * day], ['week', 7 * day],
 ['day', day], ['hour', hour], ['minute', minute]
];

function timestamp(value) {
 return Date.parse(dateOnly.test(value) ? value + 'T00:00:00+09:00' : value);
}

export function relativeTime(value, now = Date.now()) {
 const time = timestamp(value);
 if (!Number.isFinite(time)) return value;
 const calendarDate = dateOnly.test(value);
 const difference = calendarDate
  ? (Math.floor((time + koreaOffset) / day) - Math.floor((now + koreaOffset) / day)) * day
  : time - now;
 for (const [unit, duration] of units) {
  if (Math.abs(difference) >= duration) {
   return relative.format(Math.trunc(difference / duration), unit);
  }
 }
 return calendarDate ? '오늘' : difference > 0
  ? relative.format(Math.ceil(difference / 1000), 'second') : '방금 전';
}

export function exactTime(value) {
 const time = timestamp(value);
 if (!Number.isFinite(time)) return value;
 if (dateOnly.test(value)) return value + ' (시각 미기록)';
 const milliseconds = new Date(time).getUTCMilliseconds();
 const fraction = milliseconds ? '.' + String(milliseconds).padStart(3, '0') : '';
 return exact.format(time) + fraction + ' KST';
}

const escape = value => String(value).replace(/[&<>"']/g, character =>
 ({'&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'}[character]));

export function timeHTML(value, className = '') {
 const label = escape(exactTime(value));
 const classAttribute = className ? ` class="${escape(className)}"` : '';
 return `<time${classAttribute} datetime="${escape(value)}" data-exact-time="${label}">${label}</time>`;
}

const selector = 'time[datetime]';

function refreshTime(node, now) {
 const label = relativeTime(node.dateTime, now);
 if (node.textContent !== label) node.textContent = label;
}

export function startRelativeTimes() {
 function refreshTimes() {
  const now = Date.now();
  for (const node of document.querySelectorAll(selector)) refreshTime(node, now);
 }
 refreshTimes();
 new MutationObserver(records => {
  const now = Date.now();
  for (const record of records) {
   for (const node of record.addedNodes) {
    if (node.nodeType !== Node.ELEMENT_NODE) continue;
    if (node.matches(selector)) refreshTime(node, now);
    for (const time of node.querySelectorAll(selector)) refreshTime(time, now);
   }
  }
 }).observe(document.body, {childList: true, subtree: true});
 setInterval(() => {
  if (!document.hidden) refreshTimes();
 }, minute);
 document.addEventListener('visibilitychange', () => {
  if (!document.hidden) refreshTimes();
 });

 const tooltip = document.createElement('div');
 tooltip.className = 'exact-time-tooltip';
 tooltip.setAttribute('role', 'tooltip');
 tooltip.hidden = true;
 document.body.append(tooltip);
 const hide = () => { tooltip.hidden = true; };
 document.addEventListener('pointerover', event => {
  const time = event.target.closest('time[data-exact-time]');
  if (!time || event.pointerType === 'touch') return;
  const rect = time.getBoundingClientRect();
  tooltip.textContent = time.dataset.exactTime;
  tooltip.hidden = false;
  const width = tooltip.offsetWidth;
  const height = tooltip.offsetHeight;
  tooltip.style.left = Math.max(8, Math.min(innerWidth - width - 8, rect.left)) + 'px';
  tooltip.style.top = (rect.top >= height + 12 ? rect.top - height - 6 : rect.bottom + 6) + 'px';
 });
 document.addEventListener('pointerout', event => {
  if (event.target.closest('time[data-exact-time]')) hide();
 });
 document.addEventListener('keydown', event => {
  if (event.key === 'Escape') hide();
 });
 window.addEventListener('scroll', hide, true);
 window.addEventListener('resize', hide);
}
