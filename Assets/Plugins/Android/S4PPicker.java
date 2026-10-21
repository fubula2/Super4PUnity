package com.super4p.picker;

import android.app.Activity;
import android.app.Fragment;
import android.app.FragmentTransaction;
import android.content.ClipData;
import android.content.ContentResolver;
import android.content.Intent;
import android.database.Cursor;
import android.net.Uri;
import android.provider.DocumentsContract;
import android.provider.OpenableColumns;

import com.unity3d.player.UnityPlayer;

import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.OutputStream;
import java.util.ArrayList;
import java.util.List;

//Opens the system file/folder picker and copies what was picked into the app's cache, so Unity can read it.
//Result goes to the ImportPickerReceiver object as lines: D<TAB>cacheDir, P<TAB>path, F<TAB>name.
public class S4PPicker {
    private static final int REQUEST = 4721;
    private static final String TAG = "S4PPickerFragment";

    public static void pickFiles(final Activity activity) { launch(activity, false, false); }
    public static void pickAudio(final Activity activity) { launch(activity, false, true); }
    public static void pickFolder(final Activity activity) { launch(activity, true, false); }

    private static void launch(final Activity activity, final boolean folder, final boolean audio) {
        activity.runOnUiThread(new Runnable() {
            @Override public void run() {
                PickerFragment fragment = new PickerFragment();
                fragment.folder = folder;
                fragment.audio = audio;
                FragmentTransaction tx = activity.getFragmentManager().beginTransaction();
                tx.add(fragment, TAG);
                tx.commit();
            }
        });
    }

    private static void send(String method, String message) {
        UnityPlayer.UnitySendMessage("ImportPickerReceiver", method, message);
    }

    //Called when the app starts or comes back to the front: if it was opened with a .zip/.rar ("Open with" or Share),
    //the file is copied into the cache and reported through OnOpened. Each intent is handled once.
    public static void handleIntent(final Activity activity) {
        Intent intent = activity.getIntent();
        if (intent == null || intent.getBooleanExtra("s4p_done", false)) return;
        String action = intent.getAction();
        final List<Uri> uris = new ArrayList<Uri>();
        if (Intent.ACTION_VIEW.equals(action) && intent.getData() != null) {
            uris.add(intent.getData());
        } else if (Intent.ACTION_SEND.equals(action)) {
            Uri u = (Uri) intent.getParcelableExtra(Intent.EXTRA_STREAM);
            if (u != null) uris.add(u);
        } else if (Intent.ACTION_SEND_MULTIPLE.equals(action)) {
            ArrayList<Uri> list = intent.getParcelableArrayListExtra(Intent.EXTRA_STREAM);
            if (list != null) uris.addAll(list);
        }
        if (uris.isEmpty()) return;
        intent.putExtra("s4p_done", true);
        new Thread(new Runnable() {
            @Override public void run() { copyUris(activity, uris, "OnOpened"); }
        }).start();
    }

    public static class PickerFragment extends Fragment {
        boolean folder;
        boolean audio;

        @Override public void onCreate(android.os.Bundle state) {
            super.onCreate(state);
            setRetainInstance(true);
            Intent intent;
            if (folder) {
                intent = new Intent(Intent.ACTION_OPEN_DOCUMENT_TREE);
            } else {
                intent = new Intent(Intent.ACTION_OPEN_DOCUMENT);
                intent.addCategory(Intent.CATEGORY_OPENABLE);
                intent.setType("*/*");
                intent.putExtra(Intent.EXTRA_MIME_TYPES, audio
                    ? new String[] { "audio/*", "application/ogg", "application/octet-stream" }
                    : new String[] { "application/zip", "application/x-zip-compressed", "application/vnd.rar",
                        "application/x-rar-compressed", "application/octet-stream" });
                intent.putExtra(Intent.EXTRA_ALLOW_MULTIPLE, true);
            }
            startActivityForResult(intent, REQUEST);
        }

        @Override public void onActivityResult(int requestCode, final int resultCode, final Intent data) {
            super.onActivityResult(requestCode, resultCode, data);
            final Activity activity = getActivity();
            getFragmentManager().beginTransaction().remove(this).commitAllowingStateLoss();
            if (requestCode != REQUEST || resultCode != Activity.RESULT_OK || data == null) {
                send("OnPicked", "");
                return;
            }
            final boolean isFolder = folder;
            new Thread(new Runnable() {
                @Override public void run() { copyAll(activity, data, isFolder); }
            }).start();
        }
    }

    private static void copyAll(Activity activity, Intent data, boolean folder) {
        StringBuilder out = new StringBuilder();
        File base = activity.getExternalCacheDir() != null ? activity.getExternalCacheDir() : activity.getCacheDir();
        File root = new File(base, "import/" + System.currentTimeMillis());
        root.mkdirs();
        out.append("D\t").append(root.getAbsolutePath()).append("\n");
        ContentResolver resolver = activity.getContentResolver();
        try {
            if (folder) {
                Uri tree = data.getData();
                String treeId = DocumentsContract.getTreeDocumentId(tree);
                Uri rootDoc = DocumentsContract.buildDocumentUriUsingTree(tree, treeId);
                String name = queryName(resolver, rootDoc, "folder");
                File dest = uniqueFile(root, name);
                try {
                    copyTree(resolver, tree, treeId, dest);
                    out.append("P\t").append(dest.getAbsolutePath()).append("\n");
                } catch (Exception e) {
                    out.append("F\t").append(name).append("\n");
                }
            } else {
                List<Uri> uris = new ArrayList<Uri>();
                if (data.getClipData() != null) {
                    ClipData clip = data.getClipData();
                    for (int i = 0; i < clip.getItemCount(); i++) uris.add(clip.getItemAt(i).getUri());
                } else if (data.getData() != null) {
                    uris.add(data.getData());
                }
                root.delete();
                copyUris(activity, uris, "OnPicked");
                return;
            }
        } catch (Exception e) {
            out.append("F\t").append("selection").append("\n");
        }
        send("OnPicked", out.toString());
    }

    private static void copyUris(Activity activity, List<Uri> uris, String method) {
        StringBuilder out = new StringBuilder();
        File base = activity.getExternalCacheDir() != null ? activity.getExternalCacheDir() : activity.getCacheDir();
        File root = new File(base, "import/" + System.currentTimeMillis());
        root.mkdirs();
        out.append("D\t").append(root.getAbsolutePath()).append("\n");
        ContentResolver resolver = activity.getContentResolver();
        for (Uri uri : uris) {
            String fallback = uri.getLastPathSegment() != null ? uri.getLastPathSegment().replace('/', '_') : "file";
            String name = queryName(resolver, uri, fallback);
            File dest = uniqueFile(root, name);
            try {
                copyStream(resolver.openInputStream(uri), dest);
                out.append("P\t").append(dest.getAbsolutePath()).append("\n");
            } catch (Exception e) {
                out.append("F\t").append(name).append("\n");
            }
        }
        send(method, out.toString());
    }

    private static void copyTree(ContentResolver resolver, Uri tree, String parentId, File destDir) throws Exception {
        destDir.mkdirs();
        Uri children = DocumentsContract.buildChildDocumentsUriUsingTree(tree, parentId);
        Cursor c = resolver.query(children, new String[] {
            DocumentsContract.Document.COLUMN_DOCUMENT_ID,
            DocumentsContract.Document.COLUMN_DISPLAY_NAME,
            DocumentsContract.Document.COLUMN_MIME_TYPE }, null, null, null);
        if (c == null) return;
        try {
            while (c.moveToNext()) {
                String id = c.getString(0);
                String name = c.getString(1);
                String mime = c.getString(2);
                if (name == null) continue;
                name = name.replace('/', '_');
                if (DocumentsContract.Document.MIME_TYPE_DIR.equals(mime)) {
                    copyTree(resolver, tree, id, new File(destDir, name));
                } else {
                    Uri doc = DocumentsContract.buildDocumentUriUsingTree(tree, id);
                    copyStream(resolver.openInputStream(doc), new File(destDir, name));
                }
            }
        } finally {
            c.close();
        }
    }

    private static void copyStream(InputStream in, File dest) throws Exception {
        if (in == null) throw new java.io.IOException("cannot open");
        OutputStream out = new FileOutputStream(dest);
        try {
            byte[] buffer = new byte[1 << 16];
            int n;
            while ((n = in.read(buffer)) > 0) out.write(buffer, 0, n);
        } finally {
            out.close();
            in.close();
        }
    }

    private static String queryName(ContentResolver resolver, Uri uri, String fallback) {
        Cursor c = null;
        try {
            c = resolver.query(uri, new String[] { OpenableColumns.DISPLAY_NAME }, null, null, null);
            if (c != null && c.moveToFirst() && c.getString(0) != null) return c.getString(0).replace('/', '_');
        } catch (Exception ignored) {
        } finally {
            if (c != null) c.close();
        }
        return fallback;
    }

    private static File uniqueFile(File dir, String name) {
        File f = new File(dir, name);
        int i = 1;
        while (f.exists()) {
            int dot = name.lastIndexOf('.');
            String stem = dot > 0 ? name.substring(0, dot) : name;
            String ext = dot > 0 ? name.substring(dot) : "";
            f = new File(dir, stem + "_" + (i++) + ext);
        }
        return f;
    }
}
