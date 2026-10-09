// Preserve native PermissionStatus objects and Notification.permission. A plain
// object with a PermissionStatus prototype does not have its internal slots.
if (typeof Notification === 'undefined' || !navigator.permissions) {
    return
}
