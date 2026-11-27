import { readFile } from 'node:fs/promises';

async function readLocal(relativePath) {
  return readFile(new URL(relativePath, import.meta.url), 'utf8');
}

function validate(board) {
  if (board?.schemaVersion !== 1 || !Array.isArray(board.entries) ||
      !board.entries.length || board.latest !== board.entries[0]?.id) {
    throw new Error('Invalid update index');
  }
  const ids = new Set();
  for (const entry of board.entries) {
    if (!/^\d{4}-\d{2}-\d{2}-\d{3}$/.test(entry?.id) || ids.has(entry.id) ||
        entry.source !== 'AGENTS.md') {
      throw new Error('Invalid update entry');
    }
    ids.add(entry.id);
  }
  return board;
}

const status = { checkedAt: new Date().toISOString(), source: 'local', remoteVerified: false };
try {
  const index = process.argv.indexOf('--since');
  const since = index < 0 ? null : process.argv[index + 1];
  if (index >= 0 && !since) throw new Error('--since requires an ID');
  const board = validate(JSON.parse(await readLocal('./guide/updates.json')));
  const checkpoint = board.entries.findIndex(entry => entry.id === since);
  const pending = checkpoint < 0 ? board.entries : board.entries.slice(0, checkpoint);
  const instructions = pending.length ? await readLocal('../AGENTS.md') : null;
  const action = pending.length ? 'Read applicable local instructions.' : 'No new local updates.';
  console.log(JSON.stringify({ ...status, latest: board.latest, pending, instructions, action }, null, 2));
} catch (error) {
  console.log(JSON.stringify({ ...status, error: error.message, action: 'Continue with known local instructions.' }, null, 2));
  process.exitCode = 1;
}