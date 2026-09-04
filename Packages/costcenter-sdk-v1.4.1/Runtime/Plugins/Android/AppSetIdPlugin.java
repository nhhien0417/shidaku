package com.ExtraLabs.unitysdk;

import android.app.Activity;
import com.unity3d.player.UnityPlayer;
import com.google.android.gms.appset.AppSet;
import com.google.android.gms.appset.AppSetIdClient;
import com.google.android.gms.appset.AppSetIdInfo;
import com.google.android.gms.tasks.OnSuccessListener;

public class AppSetIdPlugin {

    private static Activity unityActivity;

    // Gọi từ Unity để set Activity
    public static void setActivity(Activity activity) {
        unityActivity = activity;
    }

    // Lấy AppSetId và trả về callback
    public static void getAppSetId(final String gameObjectName, final String callbackMethod) {
        if (unityActivity == null) return;

        AppSetIdClient client = AppSet.getClient(unityActivity);
        client.getAppSetIdInfo()
                .addOnSuccessListener(new OnSuccessListener<AppSetIdInfo>() {
                    @Override
                    public void onSuccess(AppSetIdInfo appSetIdInfo) {
                        String appSetId = appSetIdInfo.getId();
                        UnityPlayer.UnitySendMessage(gameObjectName, callbackMethod, appSetId);
                    }
                });
    }
}
