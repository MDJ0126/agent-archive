import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { z } from 'zod';
import { dispatch, locked } from './store.mjs';
// HTTP delegates to the single writer when Studio is open. Otherwise stdio owns its queue.
async function call(action, args) {
  const url = `http://127.0.0.1:${process.env.SPRITE_STUDIO_PORT ?? 43127}`;
  let config;
  try { const response = await fetch(`${url}/config`, { signal: AbortSignal.timeout(1000) }); config = await response.json(); }
  catch (e) { if (e.cause?.code !== 'ECONNREFUSED') throw e; return locked(() => dispatch(action, args)); }
  const response = await fetch(`${url}/api`, { method: 'POST', headers: { 'Content-Type': 'application/json', 'X-Studio-Token': config.token, Origin: url }, body: JSON.stringify({ action, ...args }) });
  const value = await response.json(); if (!response.ok) throw Error(value.error); return value;
}
const server = new McpServer({ name: 'dotaction-sprite-studio', version: '0.1.0' });
const identity = { id: z.string().regex(/^[a-z][a-z0-9_-]{0,63}$/) };
const edit = { ...identity, revision: z.number().int().min(1), frame: z.number().int().min(0).default(0) };
function tool(name, description, schema, action) {
  server.registerTool(name, { description, inputSchema: schema }, async args => {
    try {
      const value = await call(action, args);
      if (action === 'reference') {
        const { image, ...text } = value;
        return { content: [{ type: 'image', mimeType: 'image/png', data: image }, { type: 'text', text: JSON.stringify(text) }] };
      }
      return { content: [{ type: 'text', text: JSON.stringify(value) }] };
    } catch (e) { return { isError: true, content: [{ type: 'text', text: e.message }] }; }
  });
}
tool('sprite_list', 'List editable DotAction sprite documents.', {}, 'list');
tool('sprite_create', 'Create a blank indexed sprite using the DotAction palette.', { ...identity, width: z.number().int().min(1).max(256).default(32), height: z.number().int().min(1).max(256).default(32) }, 'create');
tool('sprite_read', 'Read full editable document and revision before mutations.', identity, 'read');
tool('sprite_reference', 'View the locked reference PNG and all frame difference metrics. Preserve identity, equipment, palette, scale and light when drawing animation poses.', identity, 'reference');
tool('sprite_draw', 'Draw exact pixels on a frame; color=-1 erases, otherwise color is a palette index. Start from duplicate of reference for consistent animations.', { ...edit, pixels: z.array(z.object({ x: z.number().int(), y: z.number().int(), color: z.number().int() })).max(65536) }, 'draw');
tool('sprite_duplicate', 'Duplicate a frame before editing a new pose. This does not generate physical motion.', { ...edit, name: z.string().max(100).optional() }, 'duplicate');
tool('sprite_transform', 'Integer translation or horizontal flip. Reject clipped pixels by default.', { ...edit, dx: z.number().int().default(0), dy: z.number().int().default(0), flipX: z.boolean().default(false), allowClip: z.boolean().default(false) }, 'transform');
tool('sprite_import_png', 'Normalize an image-generator PNG onto the fixed grid and shared palette. Obtain generated PNG separately; this tool does not call a paid image service.', { ...edit, png: z.string().max(24000000), append: z.boolean().default(true), mode: z.enum(['area', 'nearest']).default('area'), threshold: z.number().int().min(1).max(255).default(128), tag: z.string().max(100).default('idle'), background: z.array(z.number().int().min(0).max(255)).length(3).optional(), tolerance: z.number().int().min(0).max(255).default(0) }, 'import');
tool('sprite_import_file', 'Import a generated PNG saved under this project art directory, preserving the original. Use sprite_reference to inspect identity first.', { ...edit, path: z.string(), append: z.boolean().default(true), mode: z.enum(['area', 'nearest']).default('area'), threshold: z.number().int().min(1).max(255).default(128), tag: z.string().max(100).default('idle'), background: z.array(z.number().int().min(0).max(255)).length(3).optional(), tolerance: z.number().int().min(0).max(255).default(0) }, 'import_file');
tool('sprite_save', 'Save complete document to edit timings, tags, order or reference. Must preserve valid indexed pixels.', { ...identity, revision: z.number().int().min(1), document: z.record(z.string(), z.unknown()) }, 'save');
tool('sprite_export', 'Export untrimmed sprite sheet PNG and Aseprite-compatible JSON to art/exports/sprite-studio.', { ...identity, columns: z.number().int().min(1).max(64).default(8) }, 'export');
await server.connect(new StdioServerTransport());
