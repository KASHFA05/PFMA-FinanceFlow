// FinanceFlow service worker - lets the site be installed as a phone app.
// Only static files (css, js, images) are cached. Pages with user data are never cached.
const CACHE = 'financeflow-static-v1';

self.addEventListener('install', function (event) {
    self.skipWaiting();
});

self.addEventListener('activate', function (event) {
    event.waitUntil(
        caches.keys().then(function (keys) {
            return Promise.all(keys.filter(function (k) { return k !== CACHE; }).map(function (k) { return caches.delete(k); }));
        }).then(function () { return self.clients.claim(); })
    );
});

self.addEventListener('fetch', function (event) {
    var req = event.request;
    if (req.method !== 'GET') return;
    var url = new URL(req.url);
    if (url.origin !== location.origin) return;

    var isStatic = /\.(css|js|png|jpg|jpeg|svg|ico|woff2?)$/i.test(url.pathname);
    if (!isStatic || url.pathname === '/sw.js') return;

    event.respondWith(
        fetch(req).then(function (res) {
            var copy = res.clone();
            caches.open(CACHE).then(function (c) { c.put(req, copy); });
            return res;
        }).catch(function () { return caches.match(req); })
    );
});
