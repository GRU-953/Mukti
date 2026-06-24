/**
 * Mukti ribbon command function file.
 *
 * Per docs/phase2/MANIFEST-DESIGN.md, the MVP ribbon has a single button that
 * opens the taskpane (manifest action ShowTaskpane). That action is handled by
 * Word itself, so this file has no conversion logic — it exists because the
 * manifest declares a <FunctionFile>, and Office requires it to call
 * Office.onReady. Keeping the primary flow in the taskpane avoids a no-preview
 * path (do-not-repeat H6).
 *
 * If a future version adds a direct ribbon command, register it here with
 * Office.actions.associate and complete its event.completed().
 */

declare const Office: any;

Office.onReady(() => {
  // Nothing to register for the MVP: the button uses the ShowTaskpane action.
});
