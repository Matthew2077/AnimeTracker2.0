import { api } from '../api.js';
import { h, img, progress, badge, statusBadge, label, GROUPS, STATUSES } from '../ui.js';

export async function detailView(id) {
  const [series, initial] = await Promise.all([api.series(id), api.entries(id)]);
  let list = initial;
  const sections = h('div');

  const act = async (fn) => {
    try { await fn(); } catch (err) { alert(err.message); render(); }
  };
  const replace = (upd) => { list = list.map((x) => (x.id === upd.id ? upd : x)); render(); };

  async function move(entry, items, index, delta) {
    const ids = items.map((x) => x.id);
    [ids[index], ids[index + delta]] = [ids[index + delta], ids[index]];
    await api.reorder(id, { group: entry.mediaGroup, orderedIds: ids });
    list = await api.entries(id);
    render();
  }

  function entryRow(e, i, items) {
    return h('div', { class: 'entry' },
      img(e.imageFile, e.title),
      h('div', {},
        h('h3', {}, e.title, e.label && h('span', { class: 'muted' }, ` · ${e.label}`)),
        h('div', { class: 'badges' }, badge(label(e.mediaType)), statusBadge(e.status),
          e.tags.map((t) => h('span', { class: 'tag' }, t.name))),
        h('p', { class: 'small' }, `${e.watchedUnits}/${e.totalUnits} ${label(e.unitLabel)}`),
        progress(e.watchedUnits, e.totalUnits)),
      h('div', { class: 'actions' },
        e.externalUrl && h('a', { class: 'btn primary', href: e.externalUrl, target: '_blank', rel: 'noopener noreferrer' }, 'Open'),
        e.status !== 'Completed' &&
          h('button', { class: 'btn', onClick: () => act(async () => replace(await api.increment(e.id))) }, '+1'),
        h('select', {
          value: e.status,
          onChange: (ev) => act(async () => {
            const to = ev.target.value;
            const resets = (e.status === 'Completed' && to !== 'Completed') || (to === 'Planned' && e.watchedUnits > 0);
            if (resets && !confirm('This will reset watched units to 0. Continue?')) { ev.target.value = e.status; return; }
            replace(await api.setStatus(e.id, to));
          }),
        }, STATUSES.map((s) => h('option', { value: s }, s))),
        h('button', { class: 'btn', title: 'Move up', disabled: i === 0, onClick: () => act(() => move(e, items, i, -1)) }, '▲'),
        h('button', { class: 'btn', title: 'Move down', disabled: i === items.length - 1, onClick: () => act(() => move(e, items, i, 1)) }, '▼')));
  }

  function render() {
    const blocks = Object.keys(GROUPS).map((g) => {
      const items = list.filter((e) => e.mediaGroup === g).sort((a, b) => a.sortOrder - b.sortOrder);
      if (!items.length) return null;   // sezione vuota nascosta
      return h('section', {}, h('h2', {}, GROUPS[g]), h('div', { class: 'entries' }, items.map((e, i) => entryRow(e, i, items))));
    }).filter(Boolean);
    sections.replaceChildren(...(blocks.length ? blocks : [h('p', { class: 'muted' }, 'No entries yet.')]));
  }

  render();
  const s = series.stats;
  return h('div', {},
    h('a', { class: 'back', href: '#/' }, '← Back to library'),
    h('div', { class: 'detail-head' },
      img(series.coverFile, series.name, 'cover'),
      h('div', {},
        h('h1', {}, series.name),
        series.genreName && h('p', { class: 'muted' }, series.genreName),
        h('div', { class: 'badges' },
          badge(`${s.completed} completed`, 'st-completed'), badge(`${s.ongoing} ongoing`, 'st-ongoing'),
          badge(`${s.dropped} dropped`, 'st-dropped'), badge(`${s.planned} planned`, 'st-planned')),
        series.notes && h('p', {}, series.notes))),
    sections);
}
