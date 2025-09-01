/// <reference lib="WebWorker" />

// Basic offline caching and background sync queue for write operations
const CACHE_NAME = 'famlio-cache-v1';
const OFFLINE_URLS: string[] = [
  '/',
  '/manifest.webmanifest',
  '/icons/icon-192.png',
  '/icons/icon-512.png'
];

// IndexedDB utilities for queue
const DB_NAME = 'famlio-queue';
const STORE_NAME = 'requests';

function openDb(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const req = indexedDB.open(DB_NAME, 1);
    req.onupgradeneeded = () => {
      const db = req.result;
      if (!db.objectStoreNames.contains(STORE_NAME)) {
        db.createObjectStore(STORE_NAME, { keyPath: 'id' });
      }
    };
    req.onsuccess = () => resolve(req.result);
    req.onerror = () => reject(req.error);
  });
}

async function addToQueue(entry: any): Promise<void> {
  const db = await openDb();
  await new Promise<void>((resolve, reject) => {
    const tx = db.transaction(STORE_NAME, 'readwrite');
    tx.objectStore(STORE_NAME).put(entry);
    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error);
  });
}

async function listQueue(): Promise<any[]> {
  const db = await openDb();
  return await new Promise((resolve, reject) => {
    const tx = db.transaction(STORE_NAME, 'readonly');
    const req = tx.objectStore(STORE_NAME).getAll();
    req.onsuccess = () => resolve(req.result);
    req.onerror = () => reject(req.error);
  });
}

async function removeFromQueue(id: string): Promise<void> {
  const db = await openDb();
  await new Promise<void>((resolve, reject) => {
    const tx = db.transaction(STORE_NAME, 'readwrite');
    tx.objectStore(STORE_NAME).delete(id);
    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error);
  });
}

self.addEventListener('install', (event: ExtendableEvent) => {
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => cache.addAll(OFFLINE_URLS))
  );
  self.skipWaiting();
});

self.addEventListener('activate', (event: ExtendableEvent) => {
  event.waitUntil(self.clients.claim());
});

self.addEventListener('fetch', (event: FetchEvent) => {
  const req = event.request;
  if (req.method === 'GET') {
    event.respondWith(
      fetch(req)
        .then((res) => {
          const copy = res.clone();
          caches.open(CACHE_NAME).then((cache) => cache.put(req, copy));
          return res;
        })
        .catch(() => caches.match(req).then((res) => res || caches.match('/')))
    );
  } else {
    event.respondWith(
      fetch(req.clone()).catch(async () => {
        // queue non-GET when offline
        const body = await req.clone().text();
        await addToQueue({
          id: crypto.randomUUID(),
          url: req.url,
          method: req.method,
          headers: Array.from(req.headers.entries()),
          body
        });
        try {
          await (self as any).registration.sync.register('famlio-sync');
        } catch {}
        return new Response(null, { status: 202, statusText: 'Queued for sync' });
      })
    );
  }
});

self.addEventListener('message', (event: ExtendableMessageEvent) => {
  const { type, payload } = event.data || {};
  if (type === 'QUEUE_REQUEST') {
    const { id, url, method, headers, body } = payload;
    addToQueue({ id, url, method, headers: Object.entries(headers), body: JSON.stringify(body) }).then(async () => {
      try { await (self as any).registration.sync.register('famlio-sync'); } catch {}
    });
  }
});

self.addEventListener('sync', (event: any) => {
  if (event.tag === 'famlio-sync') {
    event.waitUntil(replayQueue());
  }
});

async function replayQueue(): Promise<void> {
  const queued = await listQueue();
  for (const item of queued) {
    const headers = new Headers(item.headers);
    try {
      const res = await fetch(item.url, {
        method: item.method,
        headers,
        body: item.body
      });
      if (res.ok) {
        await removeFromQueue(item.id);
      }
    } catch {
      // keep in queue if still failing
    }
  }
}

export {};

