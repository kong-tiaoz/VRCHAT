const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const http = require('node:http');
const { spawn } = require('node:child_process');
const output = path.join(__dirname, 'test-output');
fs.mkdirSync(output, {recursive:true});
function frame(buffer, opcode = 1, final = true) {
  buffer = Buffer.isBuffer(buffer) ? buffer : Buffer.from(buffer);
  const header = buffer.length < 126 ? Buffer.from([(final ? 128 : 0) | opcode, buffer.length]) : Buffer.from([(final ? 128 : 0) | opcode, 126, buffer.length >> 8, buffer.length & 255]);
  return Buffer.concat([header, buffer]);
}
let connections = 0;
const sockets = new Set();
const server = http.createServer();
server.on('upgrade', (request, socket) => {
  if (new URL(request.url, 'http://localhost').searchParams.get('token') !== 'integration-local-token') { socket.end('HTTP/1.1 403 Forbidden\r\n\r\n'); return; }
  const accept = crypto.createHash('sha1').update(request.headers['sec-websocket-key'] + '258EAFA5-E914-47DA-95CA-C5AB0DC85B11').digest('base64');
  socket.write('HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: ' + accept + '\r\n\r\n');
  sockets.add(socket); socket.on('close', () => sockets.delete(socket)); socket.on('error', () => {});
  connections++;
  const payload = (type, message, translation) => JSON.stringify({type, message, translation:[translation]});
  if (connections === 1) {
    socket.write(frame(payload('SENT', 'My own outgoing message', '我的发言')));
    socket.write(frame(payload('CHAT', 'Typed outgoing message', '手动输入')));
    socket.write(frame('{malformed'));
    const received = Buffer.from(payload('RECEIVED', 'Hello learner!', '你好，学习者！'));
    const split = received.indexOf(Buffer.from('学')) + 1;
    socket.write(frame(received.subarray(0, split), 1, false));
    socket.write(frame(received.subarray(split), 0, true));
    setTimeout(() => socket.destroy(), 350);
  } else socket.write(frame(payload('RECEIVED', 'Take your time.', '慢慢来。')));
});
server.listen(0, '127.0.0.1', () => {
  const config = path.join(output, 'integration-config.json');
  fs.writeFileSync(config, JSON.stringify({WEBSOCKET_SERVER:true,WEBSOCKET_HOST:'127.0.0.1',WEBSOCKET_PORT:server.address().port,WEBSOCKET_AUTH_TOKEN:'integration-local-token'}));
  const child = spawn(path.join(__dirname,'bin','SubtitleTests.exe'), ['--integration',config,path.join(output,'integration-result.json')], {windowsHide:true,stdio:['ignore','inherit','inherit']});
  child.on('error', error => { console.error(error.message); finish(1); });
  child.on('exit', code => finish(code || 0));
});
const timeout = setTimeout(() => { console.error('Integration test timeout'); finish(1); }, 35000);
function finish(code) { clearTimeout(timeout); console.log('Local test server connections: ' + connections); for(const socket of sockets) socket.destroy(); server.close(() => { process.exitCode = code; }); }
