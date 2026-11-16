// Shared indexed-pixel document format. -1 is transparent; other values index the palette.
export function integer(value, min, max, label = '값') {
  if (!Number.isInteger(value) || value < min || value > max) throw Error(`${label}: ${min}~${max} 정수 필요`);
  return value;
}
export function createDocument(palette, width = 32, height = 32, name = 'untitled') {
  integer(width, 1, 256); integer(height, 1, 256);
  return { version: 1, name, width, height, palette: structuredClone(palette), reference: 0,
    frames: [{ name: 'idle_0', duration: 120, tag: 'idle', pixels: Array(width * height).fill(-1) }] };
}
export function validate(doc) {
  if (!doc || doc.version !== 1 || typeof doc.name !== 'string' || doc.name.length > 100) throw Error('지원하지 않는 문서');
  integer(doc.width, 1, 256); integer(doc.height, 1, 256);
  if (!Array.isArray(doc.palette) || !doc.palette.length || doc.palette.length > 256) throw Error('팔레트 오류');
  for (const c of doc.palette) { if (!Array.isArray(c) || c.length !== 3) throw Error('RGB 팔레트 오류'); c.forEach(v => integer(v, 0, 255)); }
  if (!Array.isArray(doc.frames) || !doc.frames.length || doc.frames.length > 256) throw Error('프레임은 1~256개');
  if (doc.width * doc.height * doc.frames.length > 4194304) throw Error('문서 픽셀 한도 초과');
  integer(doc.reference, 0, doc.frames.length - 1);
  for (const f of doc.frames) {
    if (typeof f.name !== 'string' || typeof f.tag !== 'string' || f.name.length > 100 || f.tag.length > 100) throw Error('프레임 이름/태그 오류');
    integer(f.duration, 10, 10000);
    if (!Array.isArray(f.pixels) || f.pixels.length !== doc.width * doc.height) throw Error('프레임 크기 오류');
    f.pixels.forEach(p => integer(p, -1, doc.palette.length - 1));
  }
  return doc;
}
export function rgba(doc, index) {
  const frame = doc.frames[integer(index, 0, doc.frames.length - 1)];
  const data = new Uint8ClampedArray(doc.width * doc.height * 4);
  frame.pixels.forEach((p, i) => { if (p >= 0) { data.set(doc.palette[p], i * 4); data[i * 4 + 3] = 255; } });
  return data;
}
// Average premultiplied samples on one fixed destination grid; never dither across frames.
export function normalize(data, sw, sh, doc, { threshold = 128, mode = 'area', background = null, tolerance = 0 } = {}) {
  integer(sw, 1, 8192); integer(sh, 1, 8192); integer(threshold, 1, 255); integer(tolerance, 0, 255);
  if (data.length !== sw * sh * 4 || !['area', 'nearest'].includes(mode)) throw Error('입력 이미지 오류');
  if (background !== null && (!Array.isArray(background) || background.length !== 3 || background.some(v => !Number.isInteger(v) || v < 0 || v > 255))) throw Error('배경색 오류');
  const out = Array(doc.width * doc.height).fill(-1), cache = new Map();
  for (let y = 0; y < doc.height; y++) for (let x = 0; x < doc.width; x++) {
    let r = 0, g = 0, b = 0, alpha = 0, weight = 0;
    const x0 = x * sw / doc.width, x1 = (x + 1) * sw / doc.width;
    const y0 = y * sh / doc.height, y1 = (y + 1) * sh / doc.height;
    const nearest = mode === 'nearest';
    for (let sy = nearest ? Math.min(sh - 1, Math.floor((y0 + y1) / 2)) : Math.floor(y0); sy < (nearest ? Math.min(sh - 1, Math.floor((y0 + y1) / 2)) + 1 : Math.ceil(y1)); sy++) {
      for (let sx = nearest ? Math.min(sw - 1, Math.floor((x0 + x1) / 2)) : Math.floor(x0); sx < (nearest ? Math.min(sw - 1, Math.floor((x0 + x1) / 2)) + 1 : Math.ceil(x1)); sx++) {
        const i = (sy * sw + sx) * 4;
        const w = nearest ? 1 : (Math.min(x1, sx + 1) - Math.max(x0, sx)) * (Math.min(y1, sy + 1) - Math.max(y0, sy));
        let a = data[i + 3] / 255;
        if (background && Math.max(...background.map((c, j) => Math.abs(c - data[i + j]))) <= tolerance) a = 0;
        r += data[i] * a * w; g += data[i + 1] * a * w; b += data[i + 2] * a * w; alpha += a * w; weight += w;
      }
    }
    if (!alpha || alpha / weight * 255 < threshold) continue;
    const rgb = [r / alpha, g / alpha, b / alpha].map(Math.round), key = rgb.join(',');
    if (!cache.has(key)) {
      let best = 0, distance = Infinity;
      doc.palette.forEach((c, i) => { const d = c.reduce((s, v, j) => s + (v - rgb[j]) ** 2, 0); if (d < distance) { distance = d; best = i; } });
      cache.set(key, best);
    }
    out[y * doc.width + x] = cache.get(key);
  }
  return out;
}
export function transform(doc, index, { dx = 0, dy = 0, flipX = false } = {}) {
  integer(dx, -doc.width, doc.width); integer(dy, -doc.height, doc.height);
  const frame = doc.frames[integer(index, 0, doc.frames.length - 1)], result = Array(frame.pixels.length).fill(-1);
  let clipped = 0;
  frame.pixels.forEach((p, i) => {
    const x = (flipX ? doc.width - 1 - i % doc.width : i % doc.width) + dx, y = Math.floor(i / doc.width) + dy;
    if (x >= 0 && y >= 0 && x < doc.width && y < doc.height) result[y * doc.width + x] = p;
    else if (p >= 0) clipped++;
  });
  frame.pixels = result; return { clipped };
}
export function compare(doc, index) {
  const p = doc.frames[index].pixels, ref = doc.frames[doc.reference].pixels;
  const bounds = pixels => {
    let left = doc.width, top = doc.height, right = -1, bottom = -1;
    pixels.forEach((v, i) => { if (v >= 0) { const x = i % doc.width, y = Math.floor(i / doc.width); left = Math.min(left, x); right = Math.max(right, x); top = Math.min(top, y); bottom = Math.max(bottom, y); } });
    return right < 0 ? null : { left, top, right, bottom };
  };
  let changed = 0, silhouette = 0;
  p.forEach((v, i) => { changed += +(v !== ref[i]); silhouette += +((v >= 0) !== (ref[i] >= 0)); });
  return { changedPixels: changed, silhouettePixels: silhouette, bounds: bounds(p), referenceBounds: bounds(ref), note: '동작에 따른 정상 변화도 포함합니다. 캐릭터 동일성을 자동 판정하지 않습니다.' };
}
export function sheet(doc, columns = 8) {
  validate(doc); integer(columns, 1, 64); columns = Math.min(columns, doc.frames.length);
  const width = columns * doc.width, height = Math.ceil(doc.frames.length / columns) * doc.height;
  if (width > 4096 || height > 4096) throw Error('시트 최대 크기는 4096×4096입니다. 열 수를 조정하세요.');
  const data = new Uint8Array(width * height * 4), frames = [], tags = [];
  doc.frames.forEach((f, i) => {
    const x = i % columns * doc.width, y = Math.floor(i / columns) * doc.height, source = rgba(doc, i);
    for (let row = 0; row < doc.height; row++) data.set(source.subarray(row * doc.width * 4, (row + 1) * doc.width * 4), ((y + row) * width + x) * 4);
    frames.push({ filename: `${i}_${f.name}`, frame: { x, y, w: doc.width, h: doc.height }, rotated: false, trimmed: false,
      spriteSourceSize: { x: 0, y: 0, w: doc.width, h: doc.height }, sourceSize: { w: doc.width, h: doc.height }, duration: f.duration });
    const previous = tags.at(-1);
    if (previous && previous.name === f.tag) previous.to = i;
    else tags.push({ name: f.tag, from: i, to: i, direction: 'forward' });
  });
  return { width, height, data, metadata: { frames, meta: { app: 'DotAction Sprite Studio', version: '0.1.0', image: 'sheet.png', format: 'RGBA8888', size: { w: width, h: height }, scale: '1', frameTags: tags, pivot: { x: 0.5, y: 0 } } } };
}
