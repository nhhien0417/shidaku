#if MOCALIB_USE_FIREBASE_LEADERBOARD

using System;
using System.Threading.Tasks;

using Firebase.Auth;
using Titipi.MocaLib.Runtime.Common;
using UnityEngine;

namespace Titipi.MocaLib.Runtime.Services
{
    public static class FirebaseAuthService
    {
        private const string TAG = "FirebaseAuthService";

        public static bool IsInitialized { get; private set; }

        // Firestore's native credentials provider holds a NON-owning pointer to the
        // underlying firebase::auth::Auth. If the managed FirebaseAuth wrapper is GC'd,
        // that native object is destroyed and Firestore crashes inside
        // FirebaseAuthCredentialsProvider::GetToken (shared_from_this / bad_weak_ptr)
        // on the next token request. A static field is a GC root, so pinning the
        // instance here prevents the crash (iOS/IL2CPP is the most affected platform).
        public static FirebaseAuth Auth { get; private set; }

        public static async Task SignInAnonymously()
        {
            Auth = FirebaseAuth.DefaultInstance;

            var cachedUid = PlayerPrefs.GetString("FirebaseUID", null);
            if (!string.IsNullOrEmpty(cachedUid))
            {
                Utils.MocaLibLog(TAG, $"Returning user with UID: {cachedUid}");

                IsInitialized = true;
                return;
            }

            try
            {
                var result = await Auth.SignInAnonymouslyAsync();
                IsInitialized = true;

                PlayerPrefs.SetString("FirebaseUID", result.User.UserId);
                PlayerPrefs.Save();

                Utils.MocaLibLog(TAG, $"Firebase signed in anonymously: {result.User.UserId}");
            }
            catch (Exception e)
            {
                Utils.MocaLibLogError(TAG, $"Anonymous sign-in failed: {e}");
            }
        }
    }
}

#endif
