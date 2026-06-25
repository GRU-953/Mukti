window.muktiOffice = {

    isReady: function () {
        return typeof Office !== 'undefined' && Office.context != null;
    },

    getHostName: function () {
        if (typeof Office === 'undefined') return 'None';
        var host = Office.context.host;
        if (host === Office.HostType.Word) return 'Word';
        if (host === Office.HostType.Excel) return 'Excel';
        if (host === Office.HostType.PowerPoint) return 'PowerPoint';
        return 'Unknown';
    },

    getDisplayLanguage: function () {
        try { return Office.context.displayLanguage || 'en-US'; }
        catch (e) { return 'en-US'; }
    },

    scanWordDocument: function (knownBijoyFonts) {
        return new Promise(function (resolve, reject) {
            Word.run(function (ctx) {
                var body = ctx.document.body;
                var paras = body.paragraphs;
                paras.load('items');
                return ctx.sync().then(function () {
                    var results = [];
                    var loadOps = paras.items.map(function (para, pi) {
                        var ranges = para.getTextRanges([' ', '\n', '\t'], false);
                        ranges.load('items');
                        return ctx.sync().then(function () {
                            return Promise.all(ranges.items.map(function (range, ri) {
                                range.font.load('name');
                                return ctx.sync().then(function () {
                                    var text = range.text;
                                    var fontName = (range.font.name || '').trim().toLowerCase();
                                    if (knownBijoyFonts.indexOf(fontName) >= 0 && text.trim()) {
                                        results.push({ text: text, fontName: range.font.name, paraIndex: pi, runIndex: ri });
                                    }
                                });
                            }));
                        });
                    });
                    return Promise.all(loadOps).then(function () {
                        resolve({ runs: results, warnings: [] });
                    });
                });
            }).catch(function (e) { reject(e.message || String(e)); });
        });
    },

    applyWordConversion: function (convertedRuns, outputFont) {
        return new Promise(function (resolve, reject) {
            Word.run(function (ctx) {
                var body = ctx.document.body;
                var paras = body.paragraphs;
                paras.load('items');
                return ctx.sync().then(function () {
                    var ops = [];
                    convertedRuns.forEach(function (item) {
                        var para = paras.items[item.paraIndex];
                        if (!para) return;
                        var ranges = para.getTextRanges([' ', '\n', '\t'], false);
                        ranges.load('items');
                        ops.push(ctx.sync().then(function () {
                            var range = ranges.items[item.runIndex];
                            if (range && range.text.trim() === item.original.trim()) {
                                range.insertText(item.converted, 'Replace');
                                range.font.name = outputFont;
                            }
                        }));
                    });
                    return Promise.all(ops).then(function () { return ctx.sync(); });
                }).then(function () { resolve(true); });
            }).catch(function (e) { reject(e.message || String(e)); });
        });
    },

    revertWordConversion: function (snapshot) {
        return new Promise(function (resolve, reject) {
            Word.run(function (ctx) {
                var body = ctx.document.body;
                var paras = body.paragraphs;
                paras.load('items');
                return ctx.sync().then(function () {
                    var ops = [];
                    snapshot.forEach(function (item) {
                        var para = paras.items[item.paraIndex];
                        if (!para) return;
                        var ranges = para.getTextRanges([' ', '\n', '\t'], false);
                        ranges.load('items');
                        ops.push(ctx.sync().then(function () {
                            var range = ranges.items[item.runIndex];
                            if (range && range.text.trim() === item.converted.trim()) {
                                range.insertText(item.original, 'Replace');
                                range.font.name = item.fontName;
                            }
                        }));
                    });
                    return Promise.all(ops).then(function () { return ctx.sync(); });
                }).then(function () { resolve(true); });
            }).catch(function (e) { reject(e.message || String(e)); });
        });
    }
};
