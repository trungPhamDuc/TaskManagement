self.addEventListener('push', event => {
    let payload = {};
    try { payload = event.data ? event.data.json() : {}; } catch { payload = { body: event.data?.text() || '' }; }
    event.waitUntil(self.registration.showNotification(payload.title || 'TPM · Nhiệm vụ', {
        body: payload.body || 'Bạn có thông báo nhiệm vụ mới.',
        icon: '/icons/tpm.svg',
        badge: '/icons/tpm.svg',
        tag: payload.tag || 'tpm-notification',
        data: { url: payload.url || '/' }
    }));
});

self.addEventListener('notificationclick', event => {
    event.notification.close();
    const target = new URL(event.notification.data?.url || '/', self.location.origin).href;
    event.waitUntil(clients.matchAll({ type: 'window', includeUncontrolled: true }).then(async windows => {
        for (const client of windows) {
            if (client.url.startsWith(self.location.origin) && 'focus' in client) {
                await client.focus();
                if ('navigate' in client) return client.navigate(target);
                return;
            }
        }
        return clients.openWindow(target);
    }));
});
