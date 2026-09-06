const sidebar = document.getElementById('sidebar');
const overlay = document.getElementById('overlay');
const menuButton = document.getElementById('menuButton');
const navigationClose = document.getElementById('navigationClose');
const mobileNavigation = window.matchMedia('(max-width: 900px)');
const backgroundState = new Map();
let navigationOpen = false;
let lastFocusedElement = document.activeElement;

function navigationTargets() {
    return [...(sidebar?.querySelectorAll('a[href], button:not(:disabled), [tabindex="0"]') ?? [])]
        .filter(element => element.getClientRects().length && !element.closest('[inert]'));
}

function setNavigation(open) {
    if (!sidebar || !menuButton) return;
    const wasOpen = navigationOpen;
    const focusWasInside = sidebar.contains(document.activeElement) || sidebar.contains(lastFocusedElement);
    navigationOpen = open && mobileNavigation.matches;
    sidebar.classList.toggle('open', navigationOpen);
    overlay?.classList.toggle('show', navigationOpen);
    menuButton.setAttribute('aria-expanded', String(navigationOpen));
    document.body.classList.toggle('nav-open', navigationOpen);

    if (navigationOpen) {
        sidebar.inert = false;
        sidebar.setAttribute('role', 'dialog');
        sidebar.setAttribute('aria-modal', 'true');
        if (!wasOpen) {
            (navigationTargets()[0] ?? sidebar).focus({ preventScroll: true });
            [...document.body.children].forEach(element => {
                if (element === sidebar || element === overlay || ['SCRIPT', 'STYLE', 'LINK'].includes(element.tagName)) return;
                backgroundState.set(element, element.inert);
                element.inert = true;
            });
        }
    } else {
        backgroundState.forEach((inert, element) => { element.inert = inert; });
        backgroundState.clear();
        sidebar.removeAttribute('role');
        sidebar.removeAttribute('aria-modal');
        sidebar.inert = mobileNavigation.matches;
        if (mobileNavigation.matches && (wasOpen || focusWasInside)) {
            menuButton.focus({ preventScroll: true });
        } else if (!mobileNavigation.matches && (document.activeElement === navigationClose || document.activeElement === sidebar)) {
            navigationTargets()[0]?.focus({ preventScroll: true });
        }
    }
}

menuButton?.addEventListener('click', () => setNavigation(!navigationOpen));
navigationClose?.addEventListener('click', () => setNavigation(false));
overlay?.addEventListener('click', () => setNavigation(false));
sidebar?.querySelectorAll('a').forEach(link => link.addEventListener('click', () => {
    if (mobileNavigation.matches) setNavigation(false);
}));
document.addEventListener('keydown', event => {
    if (!navigationOpen) return;
    if (event.key === 'Escape') {
        event.preventDefault();
        setNavigation(false);
    } else if (event.key === 'Tab') {
        const targets = navigationTargets();
        const first = targets[0] ?? sidebar;
        const last = targets.at(-1) ?? sidebar;
        if (event.shiftKey && (document.activeElement === first || document.activeElement === sidebar)) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && (document.activeElement === last || document.activeElement === sidebar)) {
            event.preventDefault();
            first.focus();
        }
    }
});
document.addEventListener('focusin', event => {
    lastFocusedElement = event.target;
    if (navigationOpen && !sidebar.contains(event.target)) {
        (navigationTargets()[0] ?? sidebar).focus({ preventScroll: true });
    }
});
mobileNavigation.addEventListener('change', () => setNavigation(false));
setNavigation(false);

document.querySelectorAll('[data-password-toggle]').forEach(button => {
    button.addEventListener('click', () => {
        const input = document.getElementById(button.dataset.passwordToggle);
        if (!input) return;

        const showing = input.type === 'text';
        input.type = showing ? 'password' : 'text';
        button.setAttribute('aria-label', showing ? 'Show password' : 'Hide password');
        button.setAttribute('aria-pressed', String(!showing));
        const icon = button.querySelector('i');
        icon?.classList.toggle('bi-eye', showing);
        icon?.classList.toggle('bi-eye-slash', !showing);
        input.focus({ preventScroll: true });
    });
});

document.querySelectorAll('.notice').forEach(notice => {
    const automatic = notice.classList.contains('success');
    let remaining = 4500;
    let startedAt;
    let timer;
    let removal;
    let hovered = false;
    let dismissed = false;

    const paused = () => hovered || notice.contains(document.activeElement) || document.hidden;

    function pauseTimer() {
        if (timer !== undefined) {
            clearTimeout(timer);
            timer = undefined;
            remaining = Math.max(0, remaining - (performance.now() - startedAt));
        }
        if (removal !== undefined && !dismissed) {
            clearTimeout(removal);
            removal = undefined;
            notice.classList.remove('notice-leaving');
            remaining = Math.max(remaining, 1000);
        }
    }

    function dismiss(manual = false) {
        if (dismissed || (!manual && paused())) return;
        pauseTimer();
        if (manual) {
            dismissed = true;
            if (notice.contains(document.activeElement)) {
                const next = [...document.querySelectorAll('.notice:not(.notice-leaving) [data-dismiss-notice]')]
                    .find(button => !notice.contains(button));
                (next ?? document.getElementById('main-content'))?.focus({ preventScroll: true });
            }
            notice.inert = true;
        }
        notice.classList.add('notice-leaving');
        removal = setTimeout(() => {
            if (!manual && paused()) {
                pauseTimer();
                return;
            }
            document.removeEventListener('visibilitychange', updateTimer);
            notice.remove();
        }, window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 0 : 250);
    }

    function updateTimer() {
        if (!automatic || dismissed) return;
        if (paused()) {
            pauseTimer();
        } else if (timer === undefined && removal === undefined) {
            startedAt = performance.now();
            timer = setTimeout(() => {
                timer = undefined;
                remaining = 0;
                dismiss();
            }, remaining);
        }
    }

    notice.querySelector('[data-dismiss-notice]')?.addEventListener('click', () => dismiss(true));
    if (automatic) {
        notice.addEventListener('pointerenter', () => { hovered = true; updateTimer(); });
        notice.addEventListener('pointerleave', () => { hovered = false; updateTimer(); });
        notice.addEventListener('focusin', updateTimer);
        notice.addEventListener('focusout', () => queueMicrotask(updateTimer));
        document.addEventListener('visibilitychange', updateTimer);
        updateTimer();
    }
});
