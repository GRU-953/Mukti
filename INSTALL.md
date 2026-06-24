# How to install Mukti

## What Mukti is

Mukti is a free Microsoft Word add-in that turns old "Bijoy / SutonnyMJ" Bangla
text into modern Unicode Bangla. (An "add-in" is a small extra tool that adds a
button inside Word.) You scan your document, see a preview, then apply the
change — and there is a reliable button to undo it.

## What you need

- Microsoft Word — any one of these: Word on Windows 11, Word on a Mac, or Word
  on the web (the version that runs in your internet browser).
- An internet connection. Mukti is "online-first": it loads its program code
  from the internet each time it starts, so it will not work without a
  connection. (It is not an offline tool.)
- One file called `manifest.xml`. (A "manifest" is a small text file that tells
  Word what the add-in is and where to find it.) You download this file from the
  project's Releases page on GitHub:
  **https://github.com/gru-953/mukti** → the **Releases** section.
  We do not list a direct download link here, because it changes with each new
  version — always take `manifest.xml` from the latest release.

Pick the method below that matches how you use Word. Method A is the easiest.

---

## Method A — Word on the web (easiest, recommended)

This is the simplest way and needs no special setup.

1. Open your internet browser (for example Edge or Chrome).
2. Go to **office.com** and sign in with your Microsoft account.
3. Open **Word** and open (or create) a document.
4. At the top, click the **Home** tab. *(If you do not see the button in the
   next step there, click the **Insert** tab instead. Menu names may differ
   slightly by version.)*
5. Click **Add-ins**. *(Menu names may differ slightly by version.)*
6. In the panel that opens, click **More Add-ins** (if shown), then click the
   **My Add-ins** tab at the top.
7. Click **Upload My Add-in** (usually a small link near the top-right of that
   panel).
8. Click **Browse**, find the `manifest.xml` file you downloaded, and select it.
9. Click **Upload**.

**How to know it worked:** A group called **Mukti** with a **Mukti** button
appears on the **Home** tab of the ribbon (the strip of buttons at the top).
Click it and a panel slides in from the right.

**How to remove it:** Click **Add-ins** again, open the **My Add-ins** tab,
find **Mukti**, click the three dots (**…**) next to it, and choose **Remove**.
Uploaded add-ins on the web are also cleared automatically when you sign out or
after some time, so removing it leaves no trace.

---

## Method B — Word on Windows (desktop)

On the Windows desktop, Word loads a sideloaded add-in from a shared folder on
your network. ("Sideload" just means installing an add-in by hand instead of
from a store.) This needs a one-time setup.

### Step 1 — Put the manifest in a shared folder

1. On your computer, create a new folder, for example `C:\MuktiAddin`.
2. Copy the `manifest.xml` file you downloaded into that folder.
3. Right-click the folder and choose **Properties**.
4. Click the **Sharing** tab, then click **Share**.
5. Add yourself (or **Everyone**) and click **Share**, then **Done**.
6. Note the folder's network path shown on the Sharing tab — it looks like
   `\\YOUR-PC\MuktiAddin`. You will need it in Step 2.

### Step 2 — Tell Word to trust that folder

1. Open **Word** on your desktop.
2. Click **File** (top-left), then **Options** at the bottom.
3. In the window that opens, click **Trust Center** (in the left list).
4. Click the **Trust Center Settings** button on the right.
5. Click **Trusted Add-in Catalogs** in the left list. *(Menu names may differ
   slightly by version.)*
6. In the **Catalog Url** box, type the network path from Step 1
   (for example `\\YOUR-PC\MuktiAddin`) and click **Add catalog**.
7. Tick the **Show in Menu** box next to the path you just added.
8. Click **OK**, then **OK** again.
9. Close Word completely and open it again (this is required for the setting to
   take effect).

### Step 3 — Insert Mukti

1. In Word, click the **Insert** tab.
2. Click **My Add-ins** (or the small arrow beside **Add-ins**). *(Menu names
   may differ slightly by version.)*
3. Click the **Shared Folder** tab at the top of the box that appears.
4. Click **Mukti**, then click **Add**.

**How to know it worked:** A **Mukti** button appears on the **Home** tab, and
clicking it opens a panel on the right.

**How to remove it:** Reverse the trust setting — go to **File → Options →
Trust Center → Trust Center Settings → Trusted Add-in Catalogs**, click the
Mukti folder path in the list, click **Remove**, then **OK**. To fully clean up,
also delete the `C:\MuktiAddin` folder. After this, Mukti will no longer appear.

---

## Method C — Word on Mac (desktop)

On a Mac, Word picks up a sideloaded add-in from one special folder.

1. Close **Word** if it is open.
2. Open **Finder**.
3. In the menu bar at the top, click **Go**, then **Go to Folder…**.
4. Type this exact path and press **Return**:
   `~/Library/Containers/com.microsoft.Word/Data/Documents/wef`
   *(If a "wef" folder does not exist there, create a new folder and name it
   exactly `wef`.)*
5. Drag your downloaded `manifest.xml` file into that **wef** folder.
6. Open **Word** and open a document.
7. Click the **Insert** tab, then click **My Add-ins** *(menu names may differ
   slightly by version)*, and choose **Mukti** if it is not already loaded.

**How to know it worked:** A **Mukti** button appears on the **Home** tab; click
it to open the panel on the right.

**How to remove it:** Repeat steps 2–4 to open the **wef** folder, drag the
`manifest.xml` file to the Trash, then close and reopen Word. Mukti will be gone.

---

## Method D — For organisations (admins)

If you manage Word for many people, do not ask each person to sideload Mukti by
hand. Instead, push it to everyone from one place.

1. Sign in to the **Microsoft 365 admin centre** at **admin.microsoft.com**.
2. Go to **Settings → Integrated apps** and use **Upload custom apps /
   centralized deployment** to deploy Mukti's `manifest.xml` to the users or
   groups you choose. *(Menu names may differ slightly by version.)*

This makes Mukti appear automatically for those users with no per-person setup.
To remove it later, delete the deployment from the same Integrated apps page.

---

## A note about trust and permissions (please read)

Because Mukti is **free** and is **not yet sold through Microsoft's store**,
Word may show a warning that the add-in is from an **unknown developer** or
"unverified". This is expected for any hand-installed add-in and does not mean
anything is wrong — you may safely continue.

Mukti also asks for permission to **read and change the current document**. It
needs this for the obvious reason: it has to read your old Bangla text and write
back the converted Unicode text. It cannot do its job without it.

**Your privacy:** Your document content **never leaves your device** — no text,
file names, or other information is sent anywhere, and there is **no telemetry**
(no usage tracking). The only thing Mukti fetches from the internet is its own
program code when it starts, which is why it is **online-first**, not "offline".

---

## First use

1. On the **Home** tab, click the **Mukti** button. The Mukti panel opens on the
   right.
2. Click **Scan**. Mukti looks through your document for old Bijoy/SutonnyMJ text.
3. Review the **Preview** it shows you. Nothing in your document has changed yet.
4. Click **Apply** to convert the text to Unicode.
5. If you want to undo it, click **Revert Mukti changes**. This restores your
   original text reliably (it does not rely on a fragile Ctrl+Z guess).

That's it — your document is now in Unicode Bangla.

---

## Try it with a sample (30 seconds)

If you don't have a Bijoy document to hand, you can make a tiny test one:

1. Open a blank Word document.
2. Set the font to **SutonnyMJ** (type the name into the font box on the Home
   tab). If you don't have SutonnyMJ installed, any Bijoy-family font will do for
   a quick look — the conversion is the same.
3. Type exactly: `Avwg evsjvq wjwL` — in a Bijoy font this shows as Bangla.
4. Select that line, open **Mukti**, and **Scan → Preview → Apply**.

**You should get:** `আমি বাংলায় লিখি` (Bangla for "I write in Bangla"), shown in
Noto Sans Bengali. If you click **Revert Mukti changes**, the original line comes
back. That's the whole tool in one go.

