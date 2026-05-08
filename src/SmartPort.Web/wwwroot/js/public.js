/* ============================================================
   SmartPort — Public Website JS
   ============================================================ */

document.addEventListener('DOMContentLoaded', function () {

  // ── Sticky navbar on scroll ───────────────────────────────
  var nav = document.getElementById('pubNav');
  if (nav) {
    window.addEventListener('scroll', function () {
      if (window.scrollY > 20) {
        nav.classList.add('scrolled');
      } else {
        nav.classList.remove('scrolled');
      }
    });
  }

  // ── Mobile menu toggle ────────────────────────────────────
  var toggle = document.getElementById('mobileMenuToggle');
  var links  = document.querySelector('.sp-pub-nav__links');
  if (toggle && links) {
    toggle.addEventListener('click', function () {
      var isOpen = links.style.display === 'flex';
      links.style.display = isOpen ? '' : 'flex';
      links.style.flexDirection = 'column';
      links.style.position = 'absolute';
      links.style.top = '60px';
      links.style.left = '0';
      links.style.right = '0';
      links.style.background = '#0f172a';
      links.style.padding = '1rem 2rem';
      links.style.borderBottom = '1px solid #1e3a5f';
    });
  }

});
