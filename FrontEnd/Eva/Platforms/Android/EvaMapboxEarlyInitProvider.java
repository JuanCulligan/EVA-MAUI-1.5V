package com.eva.app;

import android.content.ContentProvider;
import android.content.ContentValues;
import android.database.Cursor;
import android.net.Uri;
import android.util.Log;

import androidx.startup.AppInitializer;

/**
 * Debe ejecutarse ANTES de mono.MonoRuntimeProvider (initOrder mayor) para que
 * MapboxSDKCommonInitializer complete bindgen en libmapbox-common antes de que Mono
 * cargue libmapbox-maps.so (__cxa_guard recursivo si maps va primero).
 */
public class EvaMapboxEarlyInitProvider extends ContentProvider {
    private static final String TAG = "EVA";

    @Override
    public boolean onCreate() {
        try {
            android.content.Context ctx = getContext();
            if (ctx == null) {
                return true;
            }
            android.content.Context app = ctx.getApplicationContext();
            AppInitializer ai = AppInitializer.getInstance(app);
            ai.initializeComponent(com.mapbox.common.MapboxSDKCommonInitializer.class);
            Log.i(TAG, "EvaMapboxEarlyInitProvider: MapboxSDKCommonInitializer OK");
        } catch (Throwable t) {
            Log.e(TAG, "EvaMapboxEarlyInitProvider", t);
        }
        return true;
    }

    @Override
    public Cursor query(Uri uri, String[] projection, String selection, String[] selectionArgs, String sortOrder) {
        return null;
    }

    @Override
    public String getType(Uri uri) {
        return null;
    }

    @Override
    public Uri insert(Uri uri, ContentValues values) {
        return null;
    }

    @Override
    public int delete(Uri uri, String selection, String[] selectionArgs) {
        return 0;
    }

    @Override
    public int update(Uri uri, ContentValues values, String selection, String[] selectionArgs) {
        return 0;
    }
}
