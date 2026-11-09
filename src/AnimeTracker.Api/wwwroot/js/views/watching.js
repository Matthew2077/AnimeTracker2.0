import { api } from '../api.js';
import { h, img, progress, badge, statusBadge, label } from '../ui.js';

export async function watchingView() {
  let list = await api.watching();
  const grid = h('div', { class: 'grid wide' });

  function card(e) {
    return h('div', { class: 'card' },
      img(e.imageFile, e.title),
      h('div', { class: 'card-body' },
        h('h3', {}, e.title, e.label && h('span', { class: 'muted' }, ` · ${e.label}`)),
        h('a', { class: 'muted small', href: `#/series/${e.seriesId}` }, e.seriesName),
        h('div', { class: 'badges' }, badge(label(e.mediaType)), e.genreName && badge(e.genreName), statusBadge(e.status),
          e.tags.map((t) => h('span', { class: 'tag' }, t.name))),
        h('p', { class: 'small' }, `${e.watchedUnits}/${e.totalUnits} ${label(e.unitLabel)}`),
        progress(e.watchedUnits, e.totalUnits),
        h('div', { class: 'actions' },
          e.externalUrl && h('a', { class: 'btn primary', href: e.externalUrl, target: '_blank', rel: 'noopener noreferrer' }, 'Open'),
          h('button', {
            class: 'btn',
            onClick: async () => {
              try {
                const upd = await api.increment(e.id);
                // se arriva al totale diventa Completed ed esce dalla lista
                list = upd.status === 'Ongoing' ? list.map((x) => (x.id === upd.id ? upd : x)) : list.filter((x) => x.id !== upd.id);
                render();
              } catch (err) { alert(err.message); }
            },
          }, '+1'))));
  }

  function render() {
    grid.replaceChildren(...(list.length
      ? list.map(card)
      : [h('p', { class: 'muted' }, 'Nothing in progress. Set an entry to "Ongoing" from its series page.')]));
  }

  render();
  return h('div', {}, h('h1', {}, 'Watching'), grid);
}
