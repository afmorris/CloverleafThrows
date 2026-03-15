// Cloverleaf Throws - site.js
// Minimal JS — progressive enhancement only

document.addEventListener('DOMContentLoaded', () => {
    // Scroll today's calendar card into view if present
    const todayCard = document.querySelector('.cal-day.is-today');
    if (todayCard) {
        todayCard.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
});
