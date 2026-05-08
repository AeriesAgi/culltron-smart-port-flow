/* ============================================================
   SmartPort — Application JS
   ============================================================ */

document.addEventListener('DOMContentLoaded', function () {

  // ── Feather icons ──────────────────────────────────────────
  if (typeof feather !== 'undefined') {
    feather.replace({ width: 16, height: 16 });
  }

  // ── Live clock (SAST) ─────────────────────────────────────
  var timeEl = document.getElementById('localTime');
  if (timeEl) {
    function updateClock() {
      var now = new Date();
      timeEl.textContent = now.toLocaleTimeString('en-ZA', {
        hour: '2-digit', minute: '2-digit', second: '2-digit',
        timeZone: 'Africa/Johannesburg'
      }) + ' SAST';
    }
    updateClock();
    setInterval(updateClock, 1000);
  }

  // ── Auto-dismiss flash alerts after 6 seconds ─────────────
  document.querySelectorAll('.sp-alert[role="alert"]').forEach(function (el) {
    setTimeout(function () {
      el.style.transition = 'opacity .4s ease';
      el.style.opacity = '0';
      setTimeout(function () { if (el.parentNode) el.parentNode.removeChild(el); }, 400);
    }, 6000);
  });

  // ── Confirm destructive actions ───────────────────────────
  document.querySelectorAll('[data-confirm]').forEach(function (el) {
    el.addEventListener('click', function (e) {
      if (!window.confirm(el.getAttribute('data-confirm') || 'Are you sure?')) {
        e.preventDefault();
      }
    });
  });

  // ── Auto-uppercase container number inputs ─────────────────
  document.querySelectorAll('input[name="number"]').forEach(function (el) {
    el.addEventListener('input', function () {
      var pos = el.selectionStart;
      el.value = el.value.toUpperCase();
      el.setSelectionRange(pos, pos);
    });
  });

  // ── Mark active nav items ─────────────────────────────────
  var path = window.location.pathname.toLowerCase();
  document.querySelectorAll('.sp-nav__item').forEach(function (el) {
    var href = (el.getAttribute('href') || '').toLowerCase();
    if (href && href !== '/' && path.startsWith(href)) {
      el.classList.add('active');
    }
  });

});
