(() => {
  const board = document.querySelector('.relations-board');
  const canvas = board.querySelector('canvas');
  const context = canvas.getContext('2d');
  const nodes = new Map([...board.querySelectorAll('[data-node]')].map(node => [node.dataset.node, node]));
  const edges = document.querySelector('#relations-edges').content.children;
  const dialog = document.querySelector('.relations-dialog');
  const article = dialog.querySelector('.relations-document');
  const sourceLink = dialog.querySelector('.relations-source');
  const documents = new Map([...nodes.values()].flatMap(node => [
    [new URL(node.dataset.preview, location.href).href, node],
    [node.dataset.source, node],
  ]));

  function draw() {
    const bounds = board.getBoundingClientRect();
    const scale = window.devicePixelRatio || 1;
    canvas.width = Math.ceil(bounds.width * scale);
    canvas.height = Math.ceil(bounds.height * scale);
    context.scale(scale, scale);
    context.lineWidth = 1.5;
    for (const { dataset: { from, to, kind } } of edges) {
      const target = nodes.get(to);
      const start = nodes.get(from).getBoundingClientRect();
      const end = target.getBoundingClientRect();
      const x1 = start.left + start.width / 2 - bounds.left;
      const x2 = end.left + end.width / 2 - bounds.left;
      const y1 = start.bottom - bounds.top;
      const y2 = end.top - bounds.top - 3;
      const mid = (y1 + y2) / 2;
      const required = kind === 'required';
      context.strokeStyle = context.fillStyle = required ? '#d4c08b' : '#8193ad';
      context.setLineDash(required ? [] : [5, 5]);
      if (kind === 'branch') {
        const tip = end.left - bounds.left - 3;
        const trunk = tip - 11;
        const y = end.top + end.height / 2 - bounds.top;
        context.beginPath();
        context.moveTo(trunk, y1);
        context.lineTo(trunk, y);
        context.lineTo(tip - 5, y);
        context.stroke();
        context.beginPath();
        context.moveTo(tip, y);
        context.lineTo(tip - 7, y - 4);
        context.lineTo(tip - 7, y + 4);
        context.fill();
        continue;
      }
      context.beginPath();
      context.moveTo(x1, y1);
      context.bezierCurveTo(x1, mid, x2, mid, x2, y2 - 5);
      context.stroke();
      context.beginPath();
      context.moveTo(x2, y2);
      context.lineTo(x2 - 4, y2 - 7);
      context.lineTo(x2 + 4, y2 - 7);
      context.fill();
    }
  }

  function showDocument(node) {
    const { preview, title } = node.dataset;
    const base = new URL(preview, location.href);
    const content = document.getElementById(`preview-${node.dataset.node}`).content.cloneNode(true);
    for (const element of content.querySelectorAll('[href], [src]')) {
      const attribute = element.hasAttribute('href') ? 'href' : 'src';
      const value = element.getAttribute(attribute);
      if (value.startsWith('#')) continue;
      element.setAttribute(attribute, new URL(value, base).href);
    }
    dialog.querySelector('#relations-title').textContent = title;
    dialog.querySelector('#relations-location').textContent = node.querySelector('small').textContent;
    sourceLink.href = base.href;
    article.replaceChildren(content);
    if (!dialog.open) dialog.showModal();
    article.scrollTop = 0;
  }
  board.addEventListener('click', event => {
    const node = event.target.closest('button[data-preview]');
    if (node) showDocument(node);
  });
  dialog.addEventListener('click', event => {
    if (event.target === dialog) {
      const bounds = dialog.getBoundingClientRect();
      if (event.clientX < bounds.left || event.clientX >= bounds.right ||
          event.clientY < bounds.top || event.clientY >= bounds.bottom) dialog.close();
      return;
    }
    const link = event.target.closest('[data-jump]');
    if (link) {
      event.preventDefault();
      const heading = article.querySelector(`#${link.dataset.jump}`);
      heading.tabIndex = -1;
      heading.focus({ preventScroll: true });
      heading.scrollIntoView({ block: 'start' });
      return;
    }
    const related = event.target.closest('.relations-document a');
    const node = related && documents.get(related.href);
    if (node && !event.ctrlKey && !event.metaKey && !event.shiftKey && !event.altKey) {
      event.preventDefault();
      showDocument(node);
      dialog.querySelector('button').focus();
    }
  });
  new ResizeObserver(draw).observe(board);
})();
