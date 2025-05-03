mergeInto(LibraryManager.library, {
  EnforcePortraitMode: function() {
    // This function is called from Unity
    var enforcePortrait = function() {
      var canvas = document.getElementById('unity-canvas');
      var container = document.getElementById('unity-container');
      var windowWidth = window.innerWidth;
      var windowHeight = window.innerHeight;
      var aspectRatio = 9/16;
      
      if (windowWidth / windowHeight > aspectRatio) {
        canvas.style.height = "100vh";
        canvas.style.width = (windowHeight * aspectRatio) + "px";
      } else {
        canvas.style.width = "100vw";
        canvas.style.height = (windowWidth / aspectRatio) + "px";
      }
    };
    
    // Call immediately and set up listener
    enforcePortrait();
    window.addEventListener('resize', enforcePortrait);
  }
});