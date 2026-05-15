mergeInto(LibraryManager.library, {

  // ─────────────────────────────────────────────────────────
  //  INITIALIZATION
  // ─────────────────────────────────────────────────────────
  FB_Initialize: function() {
    if (window._fbBridgeReady) return;

    // Định nghĩa các hàm khởi tạo trực tiếp trên window để đảm bảo truy cập được ở mọi nơi
    window._fbDoInit = function() {
      if (window._fbBridgeReady) return;
      try {
        var config = {
          apiKey:            "AIzaSyDbRIKWL6QcJi_H8CjNSEb4tswAC8WA1Os",
          authDomain:        "vibecode-drawline.firebaseapp.com",
          databaseURL:       "https://vibecode-drawline-default-rtdb.asia-southeast1.firebasedatabase.app",
          projectId:         "vibecode-drawline",
          storageBucket:     "vibecode-drawline.firebasestorage.app",
          messagingSenderId: "996110386832",
          appId:             "1:996110386832:web:be45d6c02cabfd5aacba15"
        };

        if (!firebase.apps.length) firebase.initializeApp(config);
        window.firebaseAuth = firebase.auth();
        window.firebaseDB   = firebase.database();

        // Auth Helpers
        window.firebaseSignIn = function(e, p) { return window.firebaseAuth.signInWithEmailAndPassword(e, p); };
        window.firebaseSignUp = function(e, p) { return window.firebaseAuth.createUserWithEmailAndPassword(e, p); };
        window.firebaseSignOut = function() { return window.firebaseAuth.signOut(); };
        window.firebaseSendPasswordReset = function(e) { return window.firebaseAuth.sendPasswordResetEmail(e); };
        
        // DB Helpers
        window.firebaseDBSet = function(path, value) { return window.firebaseDB.ref(path).set(value); };
        window.firebaseDBGet = function(path) { return window.firebaseDB.ref(path).get(); };
        window.firebaseDBQuery = function(path, orderBy, limit) {
          var ref = window.firebaseDB.ref(path);
          if (orderBy) ref = ref.orderByChild(orderBy);
          if (limit > 0) ref = ref.limitToLast(limit);
          return ref.get();
        };

        window._fbBridgeReady = true;
        console.log('[FirebaseBridge] Firebase initialized successfully!');
      } catch(e) {
        console.error('[FirebaseBridge] Init error: ' + e.message);
      }
    };

    window._fbLoadScript = function(url, callback) {
      var s = document.createElement('script');
      s.src = url;
      s.onload = callback;
      s.onerror = function() { console.error('[FirebaseBridge] Failed to load: ' + url); callback(); };
      document.head.appendChild(s);
    };

    // Kiểm tra nếu SDK đã có sẵn trong trang
    if (typeof firebase !== 'undefined') {
      window._fbDoInit();
    } else {
      console.log('[FirebaseBridge] Loading SDKs from CDN...');
      window._fbLoadScript('https://www.gstatic.com/firebasejs/10.12.2/firebase-app-compat.js', function() {
        window._fbLoadScript('https://www.gstatic.com/firebasejs/10.12.2/firebase-auth-compat.js', function() {
          window._fbLoadScript('https://www.gstatic.com/firebasejs/10.12.2/firebase-database-compat.js', function() {
            window._fbDoInit();
          });
        });
      });
    }
  },

  // ─────────────────────────────────────────────────────────
  //  AUTH
  // ─────────────────────────────────────────────────────────
  FB_Auth_IsSignedIn: function() { 
    if (!window.firebaseAuth) return -1; 
    return window.firebaseAuth.currentUser ? 1 : 0; 
  },

  FB_Auth_SignIn: function(ePtr, pPtr, gPtr, sPtr, fPtr) {
    var e = UTF8ToString(ePtr), p = UTF8ToString(pPtr), g = UTF8ToString(gPtr), s = UTF8ToString(sPtr), f = UTF8ToString(fPtr);
    if (!window.firebaseAuth) { SendMessage(g, f, "ERR|Not ready"); return; }
    window.firebaseSignIn(e, p)
      .then(function(c){ SendMessage(g, s, c.user.uid + "|" + (c.user.email || "")); })
      .catch(function(err){ SendMessage(g, f, err.code + "|" + err.message); });
  },

  FB_Auth_SignUp: function(ePtr, pPtr, gPtr, sPtr, fPtr) {
    var e = UTF8ToString(ePtr), p = UTF8ToString(pPtr), g = UTF8ToString(gPtr), s = UTF8ToString(sPtr), f = UTF8ToString(fPtr);
    if (!window.firebaseAuth) { SendMessage(g, f, "ERR|Not ready"); return; }
    window.firebaseSignUp(e, p)
      .then(function(c){ SendMessage(g, s, c.user.uid + "|" + (c.user.email || "")); })
      .catch(function(err){ SendMessage(g, f, err.code + "|" + err.message); });
  },

  FB_Auth_SignOut: function() { if (window.firebaseSignOut) window.firebaseSignOut(); },

  FB_Auth_SendPasswordReset: function(ePtr, gPtr, sPtr, fPtr) {
    var e = UTF8ToString(ePtr), g = UTF8ToString(gPtr), s = UTF8ToString(sPtr), f = UTF8ToString(fPtr);
    if (!window.firebaseAuth) return;
    window.firebaseSendPasswordReset(e)
      .then(function(){ SendMessage(g, s, "OK"); })
      .catch(function(err){ SendMessage(g, f, err.code + "|" + err.message); });
  },

  FB_Auth_GetCurrentUserId: function() {
    if (!window.firebaseAuth || !window.firebaseAuth.currentUser) return 0;
    var uid = window.firebaseAuth.currentUser.uid;
    var sz = lengthBytesUTF8(uid) + 1; var buf = _malloc(sz); stringToUTF8(uid, buf, sz); return buf;
  },

  FB_Auth_GetCurrentUserEmail: function() {
    if (!window.firebaseAuth || !window.firebaseAuth.currentUser) return 0;
    var email = window.firebaseAuth.currentUser.email || "";
    var sz = lengthBytesUTF8(email) + 1; var buf = _malloc(sz); stringToUTF8(email, buf, sz); return buf;
  },

  // ─────────────────────────────────────────────────────────
  //  DATABASE
  // ─────────────────────────────────────────────────────────
  FB_DB_Write: function(pathPtr, jsonPtr, gPtr, sPtr, fPtr) {
    var path = UTF8ToString(pathPtr), json = UTF8ToString(jsonPtr), g = UTF8ToString(gPtr), s = UTF8ToString(sPtr), f = UTF8ToString(fPtr);
    if (!window.firebaseDB) return;
    var val; try { val = JSON.parse(json); } catch(e) { val = json; }
    window.firebaseDBSet(path, val).then(function(){ if(g && s) SendMessage(g, s, "OK"); }).catch(function(e){ if(g && f) SendMessage(g, f, e.code + "|" + e.message); });
  },

  FB_DB_Read: function(pathPtr, gPtr, cPtr) {
    var path = UTF8ToString(pathPtr), g = UTF8ToString(gPtr), c = UTF8ToString(cPtr);
    if (!window.firebaseDB) { SendMessage(g, c, "ERROR|Not ready"); return; }
    window.firebaseDBGet(path).then(function(snap){
      if(snap.exists()) SendMessage(g, c, "OK|" + JSON.stringify(snap.val()));
      else SendMessage(g, c, "NULL|");
    }).catch(function(e){ SendMessage(g, c, "ERROR|" + e.message); });
  },

  FB_DB_Query: function(pathPtr, orderByPtr, limit, gPtr, cPtr) {
    var path = UTF8ToString(pathPtr), orderBy = UTF8ToString(orderByPtr), g = UTF8ToString(gPtr), c = UTF8ToString(cPtr);
    if (!window.firebaseDB) { SendMessage(g, c, "ERROR|Not ready"); return; }
    window.firebaseDBQuery(path, orderBy, limit).then(function(snap) {
      var result = [];
      snap.forEach(function(child) {
        var item = child.val();
        item._key = child.key;
        result.push(item);
      });
      SendMessage(g, c, "OK|" + JSON.stringify(result));
    }).catch(function(e) {
      SendMessage(g, c, "ERROR|" + e.message);
    });
  }

});
