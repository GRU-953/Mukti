// Generate simple placeholder ribbon icons for the add-in (solid brand colour),
// in pure Node (no image libraries available). These are PLACEHOLDERS — valid
// PNGs so the add-in loads and shows a button; replace with a designed mark later.
// Colour: #006A4E (Bangladesh green), with a thin lighter inner square so the
// icon isn't a featureless block at small sizes.
import { deflateSync } from 'node:zlib';
import { writeFileSync } from 'node:fs';

const BG = [0x00, 0x6a, 0x4e]; // brand green
const FG = [0xf4, 0xf4, 0xf0]; // off-white inner mark

// CRC32 (PNG chunk checksum).
const CRC = (() => {
  const t = new Uint32Array(256);
  for (let n = 0; n < 256; n++) {
    let c = n;
    for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
    t[n] = c >>> 0;
  }
  return (buf) => {
    let c = 0xffffffff;
    for (let i = 0; i < buf.length; i++) c = t[(c ^ buf[i]) & 0xff] ^ (c >>> 8);
    return (c ^ 0xffffffff) >>> 0;
  };
})();

function chunk(type, data) {
  const len = Buffer.alloc(4);
  len.writeUInt32BE(data.length, 0);
  const td = Buffer.concat([Buffer.from(type, 'ascii'), data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(CRC(td), 0);
  return Buffer.concat([len, td, crc]);
}

function png(size) {
  const raw = Buffer.alloc(size * (size * 3 + 1));
  const inset = Math.max(1, Math.round(size * 0.22));
  for (let y = 0; y < size; y++) {
    raw[y * (size * 3 + 1)] = 0; // filter byte 0
    for (let x = 0; x < size; x++) {
      const edge = x < inset || x >= size - inset || y < inset || y >= size - inset;
      const c = edge ? BG : FG;
      const o = y * (size * 3 + 1) + 1 + x * 3;
      raw[o] = c[0]; raw[o + 1] = c[1]; raw[o + 2] = c[2];
    }
  }
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(size, 0);
  ihdr.writeUInt32BE(size, 4);
  ihdr[8] = 8;  // bit depth
  ihdr[9] = 2;  // colour type 2 = truecolour RGB
  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    chunk('IHDR', ihdr),
    chunk('IDAT', deflateSync(raw)),
    chunk('IEND', Buffer.alloc(0)),
  ]);
}

for (const size of [16, 32, 64, 80, 128]) {
  writeFileSync(new URL(`../assets/icon-${size}.png`, import.meta.url), png(size));
}
console.log('Wrote placeholder icons: assets/icon-{16,32,64,80,128}.png');
