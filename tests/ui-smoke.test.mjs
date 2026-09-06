import assert from 'node:assert/strict';
import { after, before, test } from 'node:test';
import { spawn } from 'node:child_process';
import { createServer } from 'node:http';
import { readFile, mkdtemp, rm, access } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import { setTimeout as delay } from 'node:timers/promises';

// Run with Node 22+ and Chromium: node --test tests/ui-smoke.test.mjs.
// These isolated component fixtures use production assets, not authenticated Razor pages.
const root = fileURLToPath(new URL('../', import.meta.url));
const wwwroot = join(root, 'EduTrack.Web', 'wwwroot');
const layout = await readFile(join(root, 'EduTrack.Web', 'Views', 'Shared', '_Layout.cshtml'), 'utf8');
const loginView = await readFile(join(root, 'EduTrack.Web', 'Views', 'Account', 'Login.cshtml'), 'utf8');
const styles = ['lib/bootstrap/dist/css/bootstrap.min.css', ...['site', 'login', 'app', 'refinements', 'accounts', 'sidebar-fix', 'checkpoint2', 'premium-theme'].map(name => `css/${name}.css`)];
let server, browser, profile, socket, address, sequence = 0;
const pending = new Map();

function send(method, params = {}) {
    return new Promise((resolve, reject) => {
        const id = ++sequence;
        const timer = setTimeout(() => { pending.delete(id); reject(new Error(`CDP timeout: ${method}`)); }, 10000);
        pending.set(id, { resolve, reject, timer });
        socket.send(JSON.stringify({ id, method, params }));
    });
}

async function evaluate(expression) {
    const result = await send('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true });
    if (result.exceptionDetails) throw new Error(JSON.stringify(result.exceptionDetails));
    return result.result.value;
}

function loginFixture() {
    const markup = loginView.slice(loginView.indexOf('<div class="login-page">'), loginView.indexOf('@section Scripts'))
        .replaceAll('~/', '/')
        .replace(/<input asp-for="(Email|Password)"/g, (_, name) => `<input id="${name}" name="${name}" type="${name === 'Email' ? 'email' : 'password'}"`)
        .replace(/<label asp-for="(Email|Password)"/g, '<label for="$1"')
        .replace('<label for="Password" class="form-label"></label>', '<label for="Password" class="form-label">Password</label>');
    return `<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">${styles.map(path => `<link rel="stylesheet" href="/${path}">`).join('')}</head><body class="role-member page-account"><main id="main-content">${markup}</main><script src="/js/site.js"></script></body></html>`;
}

function fixture(role) {
    if (role === 'member') return loginFixture();
    const links = { admin: ['Overview', 'Students', 'Teachers', 'Courses', 'Enrollments', 'Users', 'At-risk', 'Reports'], teacher: ['My courses', 'Disputes', 'At-risk'], student: ['Transcript', 'Disputes'] }[role];
    const notices = [...layout.matchAll(/<div class="notice (?:success|error)">.*?<\/div>/g)].map(match => match[0].replace('@success', 'Record saved.').replace('@error', 'Could not save this record.')).join('');
    const close = layout.match(/<button id="navigationClose".*?<\/button>/)[0];
    return `<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">${styles.map(path => `<link rel="stylesheet" href="/${path}">`).join('')}</head><body class="role-${role}">
    <a class="skip-link" href="#main-content">Skip to main content</a>
    <aside id="sidebar" class="sidebar" aria-label="Primary navigation" tabindex="-1">${close}<a class="brand" href="#main-content"><img class="brand-logo" src="/assets/edutrack-logo.svg" alt=""><span>EduTrack</span></a><nav>${links.map((text, i) => `<a href="#main-content" ${i === 0 ? 'class="active" aria-current="page"' : ''}>${text}</a>`).join('')}</nav><div class="sidebar-account"><a id="profileLink" href="#main-content">My profile</a></div></aside>
    <div class="app-shell"><header class="topbar"><button id="menuButton" class="menu-btn" type="button" aria-label="Open navigation menu" aria-controls="sidebar" aria-expanded="false">Menu</button></header><main id="main-content" class="content" tabindex="-1"><div class="notices">${notices}</div><section class="page-head module-head"><div class="module-heading"><h1>Student records</h1></div></section><section class="card" id="sampleCard"><div class="card-heading">Academic overview</div><div class="table-responsive"><table class="table"><thead><tr><th>Student</th><th>Status</th></tr></thead><tbody><tr><td>Sample student</td><td><span class="status">Active</span></td></tr></tbody></table></div><label for="sampleInput">Name</label><input id="sampleInput" class="form-control"><button class="btn-primary" id="saveButton">Save</button></section><section class="card security-card"><h3>Security</h3></section><section class="login-panel"><div class="login-box"><h2>Sign in</h2><input id="Password" type="password"><button data-password-toggle="Password" aria-label="Show password"><i class="bi bi-eye"></i></button></div></section></main></div><button id="overlay" class="overlay" type="button" tabindex="-1" aria-hidden="true"></button><div id="alreadyInert" inert></div><script src="/js/site.js"></script></body></html>`;
}

before(async () => {
    const candidates = [process.env.EDUTRACK_BROWSER, 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe', 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe', '/usr/bin/chromium', '/usr/bin/google-chrome'].filter(Boolean);
    let executable;
    for (const candidate of candidates) { try { await access(candidate); executable = candidate; break; } catch {} }
    assert.ok(executable, 'Install Chromium or set EDUTRACK_BROWSER to its executable path.');
    server = createServer(async (request, response) => {
        try {
            const url = new URL(request.url, 'http://localhost');
            if (url.pathname === '/fixture') {
                response.setHeader('Content-Type', 'text/html; charset=utf-8');
                response.end(fixture(url.searchParams.get('role') ?? 'admin'));
            } else {
                const path = resolve(wwwroot, `.${decodeURIComponent(url.pathname)}`);
                if (!path.startsWith(wwwroot + sep)) { response.writeHead(403); response.end(); return; }
                response.setHeader('Content-Type', path.endsWith('.css') ? 'text/css' : path.endsWith('.js') ? 'text/javascript' : 'image/svg+xml');
                response.end(await readFile(path));
            }
        } catch { response.writeHead(404); response.end(); }
    });
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    address = `http://127.0.0.1:${server.address().port}`;
    profile = await mkdtemp(join(tmpdir(), 'edutrack-ui-'));
    browser = spawn(executable, ['--headless=new', '--disable-gpu', '--no-first-run', '--no-default-browser-check', '--remote-debugging-port=0', `--user-data-dir=${profile}`, 'about:blank'], { stdio: 'ignore' });
    let port;
    for (let attempt = 0; attempt < 100; attempt++) {
        try { port = (await readFile(join(profile, 'DevToolsActivePort'), 'utf8')).split('\n')[0]; break; } catch { await delay(100); }
    }
    assert.ok(port, 'Chromium did not start.');
    const targets = await (await fetch(`http://127.0.0.1:${port}/json/list`)).json();
    socket = new WebSocket(targets.find(target => target.type === 'page').webSocketDebuggerUrl);
    socket.addEventListener('message', event => {
        const message = JSON.parse(event.data);
        const request = pending.get(message.id);
        if (!request) return;
        clearTimeout(request.timer);
        pending.delete(message.id);
        if (message.error) request.reject(new Error(JSON.stringify(message.error))); else request.resolve(message.result);
    });
    await new Promise((resolve, reject) => { socket.addEventListener('open', resolve, { once: true }); socket.addEventListener('error', reject, { once: true }); });
    await send('Page.enable');
    await send('Emulation.setFocusEmulationEnabled', { enabled: true });
}, { timeout: 20000 });

after(async () => {
    socket?.close();
    if (browser && browser.exitCode === null) {
        const exited = new Promise(resolve => browser.once('exit', resolve));
        browser.kill();
        await exited;
    }
    if (server) { server.closeAllConnections(); await new Promise(resolve => server.close(resolve)); }
    if (profile) await rm(profile, { recursive: true, force: true, maxRetries: 10, retryDelay: 200 });
});

async function viewport(width, height = 800) {
    await send('Emulation.setDeviceMetricsOverride', { width, height, deviceScaleFactor: 1, mobile: false });
    await delay(250);
}

async function load(role = 'admin', width = 390) {
    await viewport(width);
    const url = `${address}/fixture?role=${role}&t=${Date.now()}`;
    await send('Page.navigate', { url });
    for (let attempt = 0; attempt < 100; attempt++) {
        if (await evaluate(`location.href === ${JSON.stringify(url)} && document.readyState === 'complete' && typeof setNavigation === 'function'`)) return;
        await delay(50);
    }
    throw new Error('Fixture did not load.');
}

async function key(key, modifiers = 0) {
    await send('Input.dispatchKeyEvent', { type: 'keyDown', key, code: key, windowsVirtualKeyCode: key === 'Tab' ? 9 : 27, modifiers });
    await send('Input.dispatchKeyEvent', { type: 'keyUp', key, code: key, windowsVirtualKeyCode: key === 'Tab' ? 9 : 27, modifiers });
}

test('all 14 active-link conditions are mirrored by aria-current in the Razor layout', () => {
    const links = [...layout.matchAll(/<a\s[^>]*class="@\(([\s\S]*?) \? "active" : ""\)[\s\S]*?<\/a>/g)];
    assert.equal(links.length, 14);
    for (const [markup, condition] of links) assert.ok(markup.includes(`aria-current="@(${condition} ? "page" : null)"`));
});

for (const role of ['admin', 'teacher', 'student']) {
    test(`${role}: drawer traps focus, closes by Escape/overlay/link/button, and restores desktop state`, async () => {
        await load(role);
        assert.equal(await evaluate('sidebar.inert && getComputedStyle(sidebar).visibility === "hidden"'), true);
        await evaluate('menuButton.focus(); menuButton.click()');
        assert.equal(await evaluate('document.activeElement.id'), 'navigationClose');
        assert.equal(await evaluate('sidebar.getAttribute("role") === "dialog" && sidebar.getAttribute("aria-modal") === "true" && document.querySelector(".app-shell").inert'), true);
        await key('Tab', 8);
        assert.equal(await evaluate('document.activeElement.id'), 'profileLink');
        await key('Tab');
        assert.equal(await evaluate('document.activeElement.id'), 'navigationClose');
        await evaluate('document.getElementById("saveButton").focus()');
        assert.equal(await evaluate('sidebar.contains(document.activeElement)'), true);
        await key('Escape');
        assert.equal(await evaluate('document.activeElement.id'), 'menuButton');
        for (const target of ['overlay', 'navigationClose', 'profileLink']) {
            await evaluate(`menuButton.click(); document.getElementById('${target}').click()`);
            assert.equal(await evaluate('!navigationOpen && sidebar.inert && !document.querySelector(".app-shell").inert'), true);
        }
        await viewport(900);
        await evaluate('menuButton.click()');
        await viewport(901);
        assert.equal(await evaluate('!sidebar.inert && !sidebar.hasAttribute("aria-modal") && !document.body.classList.contains("nav-open") && !document.querySelector(".app-shell").inert'), true);
        assert.equal(await evaluate('document.getElementById("alreadyInert").inert'), true);
        assert.notEqual(await evaluate('document.activeElement.id'), 'navigationClose');
        await evaluate('document.getElementById("profileLink").focus()');
        await viewport(320);
        assert.equal(await evaluate('document.activeElement.id'), 'menuButton');
        assert.equal(await evaluate('document.documentElement.scrollWidth <= innerWidth'), true);
        await viewport(1440);
        assert.equal(await evaluate('getComputedStyle(document.getElementById("sampleCard")).backgroundColor'), 'rgb(255, 255, 255)');
        assert.equal(await evaluate('getComputedStyle(sidebar).backgroundColor'), 'rgb(15, 39, 64)');
        assert.equal(await evaluate('getComputedStyle(document.body).backgroundImage'), 'none');
        assert.equal(await evaluate('getComputedStyle(document.querySelector(".security-card")).backgroundColor'), 'rgb(15, 39, 64)');
    });
}

test('success expires, errors persist, and simultaneous notices do not overlap', async () => {
    await load('admin', 1440);
    assert.equal(await evaluate('document.querySelector(".notice.success").getBoundingClientRect().bottom < document.querySelector(".notice.error").getBoundingClientRect().top'), true);
    await delay(4900);
    assert.equal(await evaluate('!document.querySelector(".notice.success") && !!document.querySelector(".notice.error")'), true);
    await evaluate('document.querySelector(".notice.error button").focus(); document.activeElement.click()');
    assert.equal(await evaluate('document.activeElement.id'), 'main-content');
    await delay(300);
    assert.equal(await evaluate('document.querySelectorAll(".notice").length'), 0);
});

test('success timer pauses on hover and keyboard focus, then resumes', async () => {
    await load();
    await evaluate('document.querySelector(".notice.success").dispatchEvent(new PointerEvent("pointerenter"))');
    await delay(4800);
    assert.equal(await evaluate('!!document.querySelector(".notice.success:not(.notice-leaving)")'), true);
    await evaluate('document.querySelector(".notice.success button").focus(); document.querySelector(".notice.success").dispatchEvent(new PointerEvent("pointerleave"))');
    await delay(4800);
    assert.equal(await evaluate('!!document.querySelector(".notice.success:not(.notice-leaving)")'), true);
    await evaluate('document.getElementById("sampleInput").focus()');
    assert.equal(await evaluate('document.activeElement.id'), 'sampleInput');
    await delay(4900);
    assert.equal(await evaluate('!document.querySelector(".notice.success")'), true, JSON.stringify(await evaluate('({ focus: document.activeElement.id, hidden: document.hidden, hovered: document.querySelector(".notice.success")?.matches(":hover"), state: document.querySelector(".notice.success")?.className })')));
});

test('reduced motion dismisses immediately and password toggle still works', async () => {
    await send('Emulation.setEmulatedMedia', { features: [{ name: 'prefers-reduced-motion', value: 'reduce' }] });
    await load();
    await evaluate('document.querySelector(".notice.success button").focus(); document.activeElement.click()');
    await delay(30);
    assert.equal(await evaluate('!document.querySelector(".notice.success") && document.activeElement.closest(".notice").classList.contains("error")'), true);
    await evaluate('document.querySelector("[data-password-toggle]").click()');
    assert.equal(await evaluate('document.getElementById("Password").type'), 'text');
    await evaluate('document.querySelector("[data-password-toggle]").click()');
    assert.equal(await evaluate('document.getElementById("Password").type'), 'password');
    await send('Emulation.setEmulatedMedia', { features: [] });
});

test('anonymous login uses the shared palette with visible blue focus states', async () => {
    await load('member', 1440);
    const expected = [
        ['.login-art', 'backgroundColor', 'rgb(15, 39, 64)'],
        ['.login-art', 'backgroundImage', 'none'],
        ['.login-panel', 'backgroundColor', 'rgb(244, 247, 251)'],
        ['.login-panel', 'backgroundImage', 'none'],
        ['.login-box', 'backgroundColor', 'rgb(255, 255, 255)'],
        ['.login-box', 'backgroundImage', 'none'],
        ['.login-copy h1 em', 'color', 'rgb(94, 234, 212)'],
        ['.login-copy h1 em', 'backgroundImage', 'none'],
        ['.login-audience span', 'backgroundImage', 'none'],
        ['.login-audience span', 'backgroundColor', 'rgb(248, 250, 252)'],
        ['.login-security i', 'color', 'rgb(8, 120, 111)'],
        ['.login-submit', 'backgroundColor', 'rgb(37, 99, 235)'],
        ['.login-submit', 'backgroundImage', 'none'],
        ['#Email', 'backgroundColor', 'rgb(255, 255, 255)'],
        ['#Email', 'borderTopColor', 'rgb(148, 163, 184)']
    ];
    for (const [selector, property, value] of expected) {
        assert.equal(await evaluate(`getComputedStyle(document.querySelector(${JSON.stringify(selector)}))[${JSON.stringify(property)}]`), value, `${selector}: ${property}`);
    }
    assert.equal(await evaluate('getComputedStyle(document.querySelector(".login-art"), "::before").content'), 'none');
    assert.equal(await evaluate('getComputedStyle(document.querySelector(".login-art"), "::after").content'), 'none');
    await evaluate('document.getElementById("Email").focus()');
    assert.equal(await evaluate('getComputedStyle(document.getElementById("Email")).borderTopColor'), 'rgb(37, 99, 235)');
    await key('Tab');
    assert.equal(await evaluate('document.activeElement.id'), 'Password');
    await evaluate('document.querySelector("[data-password-toggle]").click()');
    assert.equal(await evaluate('document.getElementById("Password").type'), 'text');
    await evaluate('document.querySelector("[data-password-toggle]").click()');
    assert.equal(await evaluate('document.getElementById("Password").type'), 'password');
    assert.equal(await evaluate('[...document.querySelectorAll(".login-page img")].every(img => img.complete && img.naturalWidth > 0 && new URL(img.src).pathname === "/assets/edutrack-logo.svg")'), true);
});

test('anonymous login keeps its split desktop layout and fits mobile and short screens', async () => {
    await load('member', 1440);
    for (const [width, height] of [[1440, 900], [1280, 720], [901, 800], [900, 800], [390, 844], [320, 568]]) {
        await viewport(width, height);
        const geometry = await evaluate(`(() => {
            const art = document.querySelector('.login-art');
            const panel = document.querySelector('.login-panel');
            const box = document.querySelector('.login-box').getBoundingClientRect();
            const button = document.querySelector('.login-submit').getBoundingClientRect();
            return { display: getComputedStyle(art).display, artRight: art.getBoundingClientRect().right,
                panelLeft: panel.getBoundingClientRect().left, boxLeft: box.left, boxRight: box.right,
                buttonBottom: button.bottom, boxBottom: box.bottom, pageWidth: document.documentElement.scrollWidth };
        })()`);
        if (width > 900) {
            assert.notEqual(geometry.display, 'none');
            assert.ok(geometry.panelLeft >= geometry.artRight - 1, `Split layout at ${width}px`);
        } else {
            assert.equal(geometry.display, 'none');
        }
        assert.ok(geometry.pageWidth <= width, `Horizontal overflow at ${width}px: ${JSON.stringify(geometry)}`);
        assert.ok(geometry.boxLeft >= 0 && geometry.boxRight <= width + 1, `Card clipped at ${width}px`);
        assert.ok(geometry.buttonBottom <= geometry.boxBottom, `Submit clipped at ${width}x${height}`);
    }
});
