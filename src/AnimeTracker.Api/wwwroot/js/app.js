import { h } from './ui.js';
import { seriesView } from './views/series.js';
import { detailView } from './views/detail.js';
import { watchingView } from './views/watching.js';

const app = document.getElementById('app');
let routeId = 0;

async function route() {
  const mine = ++routeId;
  const hash = location.hash.replace(/^#/, '') || '/';
  const detail = hash.match(/^\/series\/(\d+)$/);

  document.querySelectorAll('[data-nav]').forEach((a) =>
    a.classList.toggle('active', a.dataset.nav === (hash === '/watching' ? 'watching' : 'series')));

  let node;
  try {
    if (detail) node = await detailView(Number(detail[1]));
    else if (hash === '/watching') node = await watchingView();
    else node = await seriesView();
  } catch (err) {
    node = h('div', {}, h('p', { class: 'error' }, `Something went wrong: ${err.message}`), h('a', { href: '#/' }, '← Back to library'));
  }
  if (mine === routeId) app.replaceChildren(node);   // ignora risposte di navigazioni superate
}

window.addEventListener('hashchange', route);
route();
