(function () {
  var overlay = document.getElementById('loading-overlay');
  var iframe = document.getElementById('game-iframe');

  window.addEventListener('message', function (event) {
    if (!iframe || event.source !== iframe.contentWindow) return;

    switch (event.data) {
      case 'SDK_GAME_START':
        if (overlay) overlay.style.display = 'none';
        break;
      case 'SDK_GAME_PAUSE':
        if (iframe) iframe.style.filter = 'blur(4px)';
        break;
      case 'SDK_GAME_RESUME':
        if (iframe) iframe.style.filter = '';
        break;
      case 'SDK_ERROR':
        if (overlay) {
          overlay.textContent = 'Game failed to load. Please refresh.';
          overlay.style.display = 'flex';
        }
        break;
    }
  });

  // Increment play count after 5 seconds (fire-and-forget)
  var slug = iframe && iframe.dataset && iframe.dataset.gameSlug;
  if (slug) {
    setTimeout(function () {
      var token = document.querySelector('input[name="__RequestVerificationToken"]');
      fetch('/games/' + slug + '/play', {
        method: 'POST',
        headers: { 'RequestVerificationToken': token ? token.value : '' }
      });
    }, 5000);
  }
}());
