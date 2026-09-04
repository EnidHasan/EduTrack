const sidebar = document.getElementById('sidebar');
const overlay = document.getElementById('overlay');
const menuButton = document.getElementById('menuButton');

function setNavigation(open) {
    sidebar?.classList.toggle('open', open);
    overlay?.classList.toggle('show', open);
    menuButton?.setAttribute('aria-expanded', String(open));
    document.body.classList.toggle('nav-open', open);
}

menuButton?.addEventListener('click', () => setNavigation(!sidebar?.classList.contains('open')));
overlay?.addEventListener('click', () => setNavigation(false));
sidebar?.querySelectorAll('a').forEach(link => link.addEventListener('click', () => {
    if (window.innerWidth <= 900) setNavigation(false);
}));
document.addEventListener('keydown', event => {
    if (event.key === 'Escape') setNavigation(false);
});
window.addEventListener('resize', () => {
    if (window.innerWidth > 900) setNavigation(false);
});

setTimeout(() => document.querySelectorAll('.notice').forEach(notice => {
    notice.classList.add('notice-leaving');
    setTimeout(() => notice.remove(), 250);
}), 4500);
