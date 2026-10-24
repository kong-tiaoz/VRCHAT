const http = require('node:http');
const dgram = require('node:dgram');
function oscString(s) {
  const b = Buffer.from(s + '\0', 'utf8');
  return Buffer.concat([b, Buffer.alloc((4 - b.length % 4) % 4)]);
}
function packet(s) {
  return Buffer.concat([oscString('/chatbox/input'), oscString(',sTF'), oscString(s)]);
}
function message(s) {
  if (!s.is_playing || !s.title) return '';
  const pos = Number(s.current_time_ms ?? Number(s.current_time || 0) * 1000);
  let lines = (s.qrc_data || []).map(l => ({t: Number(l.start_time_ms), text: l.content}));
  if (!lines.length) {
    for (const line of (s.lyrics || '').split(/\r?\n/)) {
      const text = line.replace(/\[[^\]]*\]/g, '').trim();
      for (const m of line.matchAll(/\[(\d+):(\d+(?:\.\d+)?)\]/g))
        lines.push({t: (Number(m[1]) * 60 + Number(m[2])) * 1000, text});
    }
  }
  lines.sort((a,b) => a.t - b.t);
  let lyric = '';
  for (const line of lines) { if (line.t > pos) break; lyric = line.text || ''; }
  const clean = t => String(t || '').replace(/[\r\n\t]+/g, ' ').trim();
  let title = `♫ ${clean(s.title)}${s.artist ? ' — ' + clean(s.artist) : ''}`;
  title = Array.from(title).slice(0, 75).join('');
  return Array.from(title + (clean(lyric) ? '\n♪ ' + clean(lyric) : '')).slice(0, 144).join('');
}
function readState() {
  return new Promise((resolve, reject) => {
    const req = http.get('http://127.0.0.1:3000/api/current', {timeout: 1500}, res => {
      let data = '';
      res.setEncoding('utf8');
      res.on('data', c => { data += c; if (data.length > 4000000) res.destroy(); });
      res.on('error', reject);
      res.on('end', () => { try { resolve(JSON.parse(data)); } catch(e) { reject(e); } });
    });
    req.on('timeout', () => req.destroy(new Error('timeout')));
    req.on('error', reject);
  });
}
async function main() {
  const preview = process.argv.includes('--preview');
  const socket = dgram.createSocket('udp4');
  socket.on('error', e => console.error('OSC:', e.message));
  let last = '', sent = 0, warned = false;
  console.log(preview ? 'Preview only; no messages sent to VRChat.' : 'QQMusic → VRChat 127.0.0.1:9000. Ctrl+C to stop.');
  async function send(text) {
    if (preview) console.log(text || '[paused / cleared]');
    else await new Promise((resolve, reject) => socket.send(packet(text), 9000, '127.0.0.1', e => e ? reject(e) : resolve()));
    last = text; sent = Date.now();
  }
  process.on('SIGINT', () => { socket.close(); process.exit(); });
  for (;;) {
    try {
      const text = message(await readState());
      warned = false;
      const now = Date.now();
      // Keep the newest lyric only; at least 2 seconds between OSC messages.
      if ((text !== last && now - sent >= 2000) || (text && now - sent >= 12000)) await send(text);
    } catch(e) {
      if (!warned) { console.log('Waiting for QQMusicMonitor (local server port 3000)...'); warned = true; }
      if (last && Date.now() - sent >= 2000) { try { await send(''); } catch {} }
    }
    await new Promise(r => setTimeout(r, 250));
  }
}
module.exports = {message, packet};
if (require.main === module) main().catch(e => { console.error(e); process.exitCode = 1; });
