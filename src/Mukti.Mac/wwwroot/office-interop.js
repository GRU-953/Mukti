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

    // ── Word ─────────────────────────────────────────────────────────────────

    scanWordDocument: function (knownBijoyFonts, bengaliMarkers) {
        return new Promise(function (resolve, reject) {
            Word.run(function (ctx) {
                var body = ctx.document.body;
                var paras = body.paragraphs;
                paras.load('items');
                return ctx.sync().then(function () {
                    var results = [];
                    var warnings = [];
                    var warnedFonts = {};
                    var loadOps = paras.items.map(function (para, pi) {
                        var ranges = para.getTextRanges([' ', '\n', '\t'], false);
                        ranges.load('items');
                        return ctx.sync().then(function () {
                            return Promise.all(ranges.items.map(function (range, ri) {
                                range.font.load('name');
                                return ctx.sync().then(function () {
                                    var text = range.text;
                                    if (!text || !text.trim()) return;
                                    var rawFont = range.font.name || '';
                                    var fontName = rawFont.trim().toLowerCase();
                                    if (knownBijoyFonts.indexOf(fontName) >= 0) {
                                        results.push({ text: text, fontName: rawFont, paraIndex: pi, runIndex: ri });
                                    } else if (!warnedFonts[fontName]) {
                                        for (var m = 0; m < bengaliMarkers.length; m++) {
                                            if (fontName.indexOf(bengaliMarkers[m]) >= 0) {
                                                warnedFonts[fontName] = true;
                                                warnings.push(rawFont);
                                                break;
                                            }
                                        }
                                    }
                                });
                            }));
                        });
                    });
                    return Promise.all(loadOps).then(function () {
                        resolve({ runs: results, warnings: warnings });
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
    },

    // ── Excel ─────────────────────────────────────────────────────────────────
    // paraIndex = row, runIndex = col (0-based within the used range)

    scanExcelDocument: function (knownBijoyFonts, bengaliMarkers) {
        return new Promise(function (resolve, reject) {
            if (typeof Excel === 'undefined') { reject('Excel API not available'); return; }
            Excel.run(function (ctx) {
                var sheet = ctx.workbook.worksheets.getActiveWorksheet();
                var usedRange = sheet.getUsedRange();
                usedRange.load(['values', 'rowCount', 'columnCount']);
                return ctx.sync().then(function () {
                    var results = [];
                    var warnings = [];
                    var warnedFonts = {};
                    var cellsToLoad = [];
                    for (var r = 0; r < usedRange.rowCount; r++) {
                        for (var c = 0; c < usedRange.columnCount; c++) {
                            var val = usedRange.values[r][c];
                            if (val && typeof val === 'string' && val.trim()) {
                                var cell = usedRange.getCell(r, c);
                                cell.format.font.load('name');
                                cellsToLoad.push({ cell: cell, row: r, col: c, text: String(val) });
                            }
                        }
                    }
                    return ctx.sync().then(function () {
                        cellsToLoad.forEach(function (item) {
                            var rawFont = item.cell.format.font.name || '';
                            var fontName = rawFont.trim().toLowerCase();
                            if (knownBijoyFonts.indexOf(fontName) >= 0) {
                                results.push({ text: item.text, fontName: rawFont, paraIndex: item.row, runIndex: item.col });
                            } else if (!warnedFonts[fontName]) {
                                for (var m = 0; m < bengaliMarkers.length; m++) {
                                    if (fontName.indexOf(bengaliMarkers[m]) >= 0) {
                                        warnedFonts[fontName] = true;
                                        warnings.push(rawFont);
                                        break;
                                    }
                                }
                            }
                        });
                        resolve({ runs: results, warnings: warnings });
                    });
                });
            }).catch(function (e) { reject(e.message || String(e)); });
        });
    },

    applyExcelConversion: function (convertedRuns, outputFont) {
        return new Promise(function (resolve, reject) {
            if (typeof Excel === 'undefined') { reject('Excel API not available'); return; }
            Excel.run(function (ctx) {
                var sheet = ctx.workbook.worksheets.getActiveWorksheet();
                var usedRange = sheet.getUsedRange();
                usedRange.load(['values', 'rowCount', 'columnCount']);
                return ctx.sync().then(function () {
                    convertedRuns.forEach(function (item) {
                        if (item.paraIndex >= usedRange.rowCount || item.runIndex >= usedRange.columnCount) return;
                        var currentVal = usedRange.values[item.paraIndex][item.runIndex];
                        if (currentVal && String(currentVal).trim() === item.original.trim()) {
                            var cell = usedRange.getCell(item.paraIndex, item.runIndex);
                            cell.values = [[item.converted]];
                            cell.format.font.name = outputFont;
                        }
                    });
                    return ctx.sync();
                }).then(function () { resolve(true); });
            }).catch(function (e) { reject(e.message || String(e)); });
        });
    },

    revertExcelConversion: function (snapshot) {
        return new Promise(function (resolve, reject) {
            if (typeof Excel === 'undefined') { reject('Excel API not available'); return; }
            Excel.run(function (ctx) {
                var sheet = ctx.workbook.worksheets.getActiveWorksheet();
                var usedRange = sheet.getUsedRange();
                usedRange.load(['values', 'rowCount', 'columnCount']);
                return ctx.sync().then(function () {
                    snapshot.forEach(function (item) {
                        if (item.paraIndex >= usedRange.rowCount || item.runIndex >= usedRange.columnCount) return;
                        var currentVal = usedRange.values[item.paraIndex][item.runIndex];
                        if (currentVal && String(currentVal).trim() === item.converted.trim()) {
                            var cell = usedRange.getCell(item.paraIndex, item.runIndex);
                            cell.values = [[item.original]];
                            cell.format.font.name = item.fontName;
                        }
                    });
                    return ctx.sync();
                }).then(function () { resolve(true); });
            }).catch(function (e) { reject(e.message || String(e)); });
        });
    },

    // ── PowerPoint ───────────────────────────────────────────────────────────
    // Apply/revert use text-based matching (re-scan) because PPT shape/run
    // indices are not stable across edits.

    scanPowerPointDocument: function (knownBijoyFonts, bengaliMarkers) {
        return new Promise(function (resolve, reject) {
            if (typeof PowerPoint === 'undefined') { reject('PowerPoint API not available'); return; }
            PowerPoint.run(function (ctx) {
                var slides = ctx.presentation.slides;
                slides.load('items');
                return ctx.sync().then(function () {
                    var results = [];
                    var warnings = [];
                    var warnedFonts = {};

                    var slideOps = slides.items.map(function (slide, si) {
                        var shapes = slide.shapes;
                        shapes.load('items');
                        return ctx.sync().then(function () {
                            // Batch-load hasText for all shapes in this slide
                            shapes.items.forEach(function (shape) {
                                shape.textFrame.load('hasText');
                            });
                            return ctx.sync().then(function () {
                                var textShapes = shapes.items.filter(function (s) { return s.textFrame.hasText; });
                                var shapeOps = textShapes.map(function (shape, shi) {
                                    var paragraphs = shape.textFrame.textRange.paragraphs;
                                    paragraphs.load('items');
                                    return ctx.sync().then(function () {
                                        var paraOps = paragraphs.items.map(function (para, pi) {
                                            var runs = para.runs;
                                            runs.load('items');
                                            return ctx.sync().then(function () {
                                                runs.items.forEach(function (run) {
                                                    run.font.load('name');
                                                });
                                                return ctx.sync().then(function () {
                                                    runs.items.forEach(function (run, ri) {
                                                        var text = run.text;
                                                        if (!text || !text.trim()) return;
                                                        var rawFont = run.font.name || '';
                                                        var fontName = rawFont.trim().toLowerCase();
                                                        if (knownBijoyFonts.indexOf(fontName) >= 0) {
                                                            // paraIndex encodes slide+shape+para; runIndex is run
                                                            results.push({ text: text, fontName: rawFont, paraIndex: si * 10000 + shi * 100 + pi, runIndex: ri });
                                                        } else if (!warnedFonts[fontName]) {
                                                            for (var m = 0; m < bengaliMarkers.length; m++) {
                                                                if (fontName.indexOf(bengaliMarkers[m]) >= 0) {
                                                                    warnedFonts[fontName] = true;
                                                                    warnings.push(rawFont);
                                                                    break;
                                                                }
                                                            }
                                                        }
                                                    });
                                                });
                                            });
                                        });
                                        return Promise.all(paraOps);
                                    });
                                });
                                return Promise.all(shapeOps);
                            });
                        });
                    });

                    return Promise.all(slideOps).then(function () {
                        resolve({ runs: results, warnings: warnings });
                    });
                });
            }).catch(function (e) { reject(e.message || String(e)); });
        });
    },

    applyPowerPointConversion: function (convertedRuns, outputFont) {
        return new Promise(function (resolve, reject) {
            if (typeof PowerPoint === 'undefined') { reject('PowerPoint API not available'); return; }
            var lookup = {};
            convertedRuns.forEach(function (item) {
                lookup[item.original.trim()] = item.converted;
            });
            PowerPoint.run(function (ctx) {
                var slides = ctx.presentation.slides;
                slides.load('items');
                return ctx.sync().then(function () {
                    var slideOps = slides.items.map(function (slide) {
                        var shapes = slide.shapes;
                        shapes.load('items');
                        return ctx.sync().then(function () {
                            shapes.items.forEach(function (shape) { shape.textFrame.load('hasText'); });
                            return ctx.sync().then(function () {
                                var textShapes = shapes.items.filter(function (s) { return s.textFrame.hasText; });
                                var shapeOps = textShapes.map(function (shape) {
                                    var paragraphs = shape.textFrame.textRange.paragraphs;
                                    paragraphs.load('items');
                                    return ctx.sync().then(function () {
                                        var paraOps = paragraphs.items.map(function (para) {
                                            var runs = para.runs;
                                            runs.load('items');
                                            return ctx.sync().then(function () {
                                                runs.items.forEach(function (run) {
                                                    var trimmed = (run.text || '').trim();
                                                    if (lookup[trimmed] !== undefined) {
                                                        run.text = run.text.replace(trimmed, lookup[trimmed]);
                                                        run.font.name = outputFont;
                                                    }
                                                });
                                                return ctx.sync();
                                            });
                                        });
                                        return Promise.all(paraOps);
                                    });
                                });
                                return Promise.all(shapeOps);
                            });
                        });
                    });
                    return Promise.all(slideOps);
                }).then(function () { resolve(true); });
            }).catch(function (e) { reject(e.message || String(e)); });
        });
    },

    revertPowerPointConversion: function (snapshot) {
        return new Promise(function (resolve, reject) {
            if (typeof PowerPoint === 'undefined') { reject('PowerPoint API not available'); return; }
            var lookup = {};
            snapshot.forEach(function (item) {
                lookup[item.converted.trim()] = { original: item.original, fontName: item.fontName };
            });
            PowerPoint.run(function (ctx) {
                var slides = ctx.presentation.slides;
                slides.load('items');
                return ctx.sync().then(function () {
                    var slideOps = slides.items.map(function (slide) {
                        var shapes = slide.shapes;
                        shapes.load('items');
                        return ctx.sync().then(function () {
                            shapes.items.forEach(function (shape) { shape.textFrame.load('hasText'); });
                            return ctx.sync().then(function () {
                                var textShapes = shapes.items.filter(function (s) { return s.textFrame.hasText; });
                                var shapeOps = textShapes.map(function (shape) {
                                    var paragraphs = shape.textFrame.textRange.paragraphs;
                                    paragraphs.load('items');
                                    return ctx.sync().then(function () {
                                        var paraOps = paragraphs.items.map(function (para) {
                                            var runs = para.runs;
                                            runs.load('items');
                                            return ctx.sync().then(function () {
                                                runs.items.forEach(function (run) {
                                                    var trimmed = (run.text || '').trim();
                                                    var orig = lookup[trimmed];
                                                    if (orig !== undefined) {
                                                        run.text = run.text.replace(trimmed, orig.original);
                                                        run.font.name = orig.fontName;
                                                    }
                                                });
                                                return ctx.sync();
                                            });
                                        });
                                        return Promise.all(paraOps);
                                    });
                                });
                                return Promise.all(shapeOps);
                            });
                        });
                    });
                    return Promise.all(slideOps);
                }).then(function () { resolve(true); });
            }).catch(function (e) { reject(e.message || String(e)); });
        });
    }
};
