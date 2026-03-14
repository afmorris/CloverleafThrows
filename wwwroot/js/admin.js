// admin.js — shared admin utilities
document.addEventListener('DOMContentLoaded', () => {
    // Auto-dismiss alerts after 5s
    document.querySelectorAll('.alert').forEach(el => {
        setTimeout(() => {
            el.style.transition = 'opacity 0.3s ease';
            el.style.opacity = '0';
            setTimeout(() => el.remove(), 300);
        }, 5000);
    });
});
