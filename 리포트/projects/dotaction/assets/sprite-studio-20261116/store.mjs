import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { PNG } from 'pngjs';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import { validate, rgba, normalize, sheet, integer, createDocument, compare, transform } from './public/core.mjs';
export const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
export const documents = path.join(root, 'art/source/sprite-studio');
export const exportsRoot = path.join(root, 'art/exports/sprite-studio');
export const palette = JSON.parse(await fs.readFile(path.join(root, 'art/palettes/dotaction-32.json'), 'utf8')).colors.map(c => c.rgb);
export function id(value) { if (typeof value !== 'string' || !/^[a-z][a-z0-9_-]{0,63}$/.test(value)) throw Error('ID: 영문 소문자로 시작하는 1~64자 영문·숫자·밑줄·하이픈'); return value; }
export async function load(name) { return validate(JSON.parse(await fs.readFile(path.join(documents, `${id(name)}.sprite.json`), 'utf8'))); }
// Revision is checked inside the process-wide queue to reject UI/MCP overwrite conflicts.
let queue = Promise.resolve();
export function locked(fn) {
  const result = queue.then(async () => {
    await fs.mkdir(documents, { recursive: true });
    const lockPath = path.join(documents, '.writer.lock');
    let handle;
    try { handle = await fs.open(lockPath, 'wx'); }
    catch (e) { if (e.code === 'EEXIST') throw Error('다른 프로세스가 쓰는 중입니다. 잠시 후 다시 시도하세요. 비정상 종료 후라면 docs/sprite-studio.md의 잠금 복구를 확인하세요.'); throw e; }
    try { await handle.writeFile(String(process.pid)); return await fn(); }
    finally { await handle.close(); await fs.unlink(lockPath); }
  });
  queue = result.catch(() => {}); return result;
}
export async function save(name, doc, expectedRevision) {
  id(name); validate(doc); await fs.mkdir(documents, { recursive: true });
  let old; try { old = await load(name); } catch (e) { if (e.code !== 'ENOENT') throw e; }
  if (expectedRevision !== (old?.revision ?? 0)) throw Error('다른 창 또는 MCP가 문서를 변경했습니다. 다시 불러온 후 편집하세요.');
  const result = { ...doc, revision: (old?.revision ?? 0) + 1 };
  const filename = path.join(documents, `${name}.sprite.json`), temp = `${filename}.${process.pid}.tmp`;
  await fs.writeFile(temp, JSON.stringify(result)); await fs.rename(temp, filename); return result;
}
export function png(width, height, data) { return PNG.sync.write({ width, height, data: Buffer.from(data) }); }
export function decode(base64) {
  if (typeof base64 !== 'string' || base64.length > 24000000) throw Error('PNG 입력은 18MB 이하');
  const buffer = Buffer.from(base64.replace(/^data:image\/png;base64,/, ''), 'base64');
  if (buffer.length < 24 || !buffer.subarray(0, 8).equals(Buffer.from([137,80,78,71,13,10,26,10]))) throw Error('PNG만 지원합니다.');
  const w = buffer.readUInt32BE(16), h = buffer.readUInt32BE(20);
  integer(w, 1, 8192); integer(h, 1, 8192); if (w * h > 16777216) throw Error('입력 최대 1600만 픽셀');
  return PNG.sync.read(buffer);
}
export async function exportSheet(name, doc, columns = 8) {
  id(name); const result = sheet(doc, columns);
  await fs.mkdir(exportsRoot, { recursive: true });
  const filename = `${name}-r${doc.revision}-${Date.now()}`;
  const image = path.join(exportsRoot, `${filename}.png`), json = path.join(exportsRoot, `${filename}.json`);
  result.metadata.meta.image = path.basename(image);
  await fs.writeFile(image, png(result.width, result.height, result.data));
  await fs.writeFile(json, JSON.stringify(result.metadata, null, 2));
  let native = null, nativeWarning = null;
  const nativeDir = path.join(documents, 'native');
  try {
    const executable = path.join(root, '.tools/aseprite-build/bin/aseprite.exe');
    await fs.access(executable); await fs.mkdir(nativeDir, { recursive: true });
    const output = path.join(nativeDir, `${filename}.aseprite`);
    await promisify(execFile)(executable, ['-b', '--script-param', `input=${path.join(documents, `${name}.sprite.json`)}`, '--script-param', `output=${output}`, '--script', path.join(root, 'tools/aseprite/import-sprite-studio.lua')], { windowsHide: true, timeout: 20000 });
    if ((await fs.stat(output)).size < 128) throw Error('Aseprite 원본이 비어 있습니다.');
    native = output;
  } catch (e) { nativeWarning = `PNG/JSON 출력은 완료. Aseprite 원본: ${e.message}`; }
  return { image, json, native, nativeWarning, frames: doc.frames.length, width: result.width, height: result.height };
}
export async function dispatch(action, args) {
  const name = args.id;
  if (action === 'list') { await fs.mkdir(documents, { recursive: true }); return (await fs.readdir(documents)).filter(f => f.endsWith('.sprite.json')).map(f => f.replace('.sprite.json', '')); }
  if (action === 'create') return save(name, createDocument(palette, args.width ?? 32, args.height ?? 32, name), 0);
  if (action === 'save') return save(name, args.document, args.revision);
  const doc = await load(name);
  if (action === 'read') return doc;
  if (action === 'export') return exportSheet(name, doc, args.columns);
  if (action === 'reference') return { document: doc, comparison: doc.frames.map((_, i) => compare(doc, i)), image: png(doc.width, doc.height, rgba(doc, doc.reference)).toString('base64') };
  if (args.revision !== doc.revision) throw Error('최신 revision을 read로 확인하세요.');
  const index = integer(args.frame ?? 0, 0, doc.frames.length - 1);
  if (action === 'draw') {
    if (!Array.isArray(args.pixels) || args.pixels.length > 65536) throw Error('픽셀 배열 오류');
    for (const p of args.pixels) {
      integer(p.x, 0, doc.width - 1); integer(p.y, 0, doc.height - 1); integer(p.color, -1, doc.palette.length - 1);
      doc.frames[index].pixels[p.y * doc.width + p.x] = p.color;
    }
  } else if (action === 'transform') {
    const result = transform(doc, index, args);
    if (result.clipped && !args.allowClip) throw Error(`${result.clipped}픽셀이 잘립니다. allowClip=true 또는 이동량을 수정하세요.`);
  } else if (action === 'duplicate') {
    const f = structuredClone(doc.frames[index]); f.name = args.name ?? `${f.tag}_${doc.frames.length}`;
    doc.frames.splice(index + 1, 0, f); if (doc.reference > index) doc.reference++;
  } else if (action === 'import' || action === 'import_file') {
    let base64 = args.png;
    if (action === 'import_file') {
      if (typeof args.path !== 'string') throw Error('PNG 경로 필요');
      const artRoot = await fs.realpath(path.join(root, 'art'));
      const target = await fs.realpath(path.resolve(root, args.path));
      const relative = path.relative(artRoot, target);
      if (relative.startsWith('..') || path.isAbsolute(relative) || path.extname(target).toLowerCase() !== '.png') throw Error('프로젝트 art 폴더 안의 PNG만 가져올 수 있습니다.');
      if ((await fs.stat(target)).size > 18000000) throw Error('PNG 최대 18MB');
      base64 = (await fs.readFile(target)).toString('base64');
    }
    const image = decode(base64); const pixels = normalize(image.data, image.width, image.height, doc, args);
    if (args.append) doc.frames.push({ name: `frame_${doc.frames.length}`, tag: args.tag ?? 'idle', duration: 120, pixels });
    else doc.frames[index].pixels = pixels;
  } else throw Error('알 수 없는 작업');
  return save(name, doc, args.revision);
}
