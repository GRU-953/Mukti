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

    // ── Word — full document ──────────────────────────────────────────────

    scanWordDocument: function (knownBijoyFonts, bengaliMarkers) {
        return window.muktiOffice._scanWordRanges(knownBijoyFonts, bengaliMarkers, false);
    },

    // U-011: selection-only
    scanWordSelection: function (knownBijoyFonts, bengaliMarkers) {
        return window.muktiOffice._scanWordRanges(knownBijoyFonts, bengaliMarkers, true);
    },

    _scanWordRanges: function (knownBijoyFonts, bengaliMarkers, selectionOnly) {
        return new Promise(function (resolve, reject) {
            Word.run(function (ctx) {
                var results = [];
                var warnings = [];
                var warnedFonts = {};

                function scanRangeItems(rangeItems, baseParaIndex) {
                    return Promise.all(rangeItems.map(function (range, ri) {
                        range.font.load('name');
                        return ctx.sync().then(function () {
                            var text = range.text;
                            if (!text || !text.trim()) return;
                            var rawFont = range.font.name || '';
                            var fontName = rawFont.trim().toLowerCase();
                            if (knownBijoyFonts.indexOf(fontName) >= 0) {
                                results.push({ text: text, fontName: rawFont, paraIndex: baseParaIndex, runIndex: ri });
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
                }

                if (selectionOnly) {
                    // U-011: get selected range only
                    var sel = ctx.document.getSelection();
                    var selRanges = sel.getTextRanges([' ', '\n', '\t'], false);
                    selRanges.load('items');
                    return ctx.sync().then(function () {
                        if (selRanges.items.length === 0) {
                            resolve({ runs: [], warnings: ['No text selected'] });
                            return;
                        }
                        return scanRangeItems(selRanges.items, 0).then(function () {
                            resolve({ runs: results, warnings: warnings });
                        });
                    });
                }

                // Full document: body paragraphs
                var body = ctx.document.body;
                var paras = body.paragraphs;
                paras.load('items');
                return ctx.sync().then(function () {
                    var bodyOps = paras.items.map(function (para, pi) {
                        var ranges = para.getTextRanges([' ', '\n', '\t'], false);
                        ranges.load('items');
                        return ctx.sync().then(function () {
                            return scanRangeItems(ranges.items, pi);
                        });
                    });

                    return Promise.all(bodyOps).then(function () {
                        // U-004: Headers and footers via sections
                        var sections = ctx.document.sections;
                        sections.load('items');
                        return ctx.sync().then(function () {
                            var hfOps = [];
                            sections.items.forEach(function (section, si) {
                                ['primary', 'firstPage', 'evenPages'].forEach(function (type) {
                                    try {
                                        var hdr = section.getHeader(Word.HeaderFooterType[type]);
                                        var hdrParas = hdr.paragraphs;
                                        hdrParas.load('items');
                                        hfOps.push(ctx.sync().then(function () {
                                            return Promise.all(hdrParas.items.map(function (para, pi) {
                                                var ranges = para.getTextRanges([' ', '\n', '\t'], false);
                                                ranges.load('items');
                                                return ctx.sync().then(function () {
                                                    return scanRangeItems(ranges.items, 100000 + si * 1000 + pi);
                                                });
                                            }));
                                        }).catch(function () {}));

                                        var ftr = section.getFooter(Word.HeaderFooterType[type]);
                                        var ftrParas = ftr.paragraphs;
                                        ftrParas.load('items');
                                        hfOps.push(ctx.sync().then(function () {
                                            return Promise.all(ftrParas.items.map(function (para, pi) {
                                                var ranges = para.getTextRanges([' ', '\n', '\t'], false);
                                                ranges.load('items');
                                                return ctx.sync().then(function () {
                                                    return scanRangeItems(ranges.items, 200000 + si * 1000 + pi);
                                                });
                                            }));
                                        }).catch(function () {}));
                                    } catch (e) {}
                                });
                            });
                            return Promise.all(hfOps).then(function () {
                                // Word tables
                                var tableOps = [];
                                try {
                                    var tables = body.tables;
                                    tables.load('items');
                                    tableOps.push(ctx.sync().then(function () {
                                        return Promise.all(tables.items.map(function (table, ti) {
                                            table.rows.load('items');
                                            return ctx.sync().then(function () {
                                                return Promise.all(table.rows.items.map(function (row, ri) {
                                                    row.cells.load('items');
                                                    return ctx.sync().then(function () {
                                                        return Promise.all(row.cells.items.map(function (cell, ci) {
                                                            var cellParas = cell.body.paragraphs;
                                                            cellParas.load('items');
                                                            return ctx.sync().then(function () {
                                                                return Promise.all(cellParas.items.map(function (para, pi) {
                                                                    var ranges = para.getTextRanges([' ', '\n', '\t'], false);
                                                                    ranges.load('items');
                                                                    return ctx.sync().then(function () {
                                                                        return scanRangeItems(ranges.items, 400000 + ti * 10000 + ri * 100 + ci);
                                                                    });
                                                                }));
                                                            });
                                                        }));
                                                    });
                                                }));
                                            });
                                        }));
                                    }).catch(function () {}));
                                } catch (e) {}
                                return Promise.all(tableOps).then(function () {
                                    resolve({ runs: results, warnings: warnings });
                                });
                            });
                        }).catch(function () {
                            resolve({ runs: results, warnings: warnings });
                        });
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
                        if (item.paraIndex >= 100000) return; // headers/footers: skip index-based apply
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
                }).then(function () {
                    // Apply to table cells (paraIndex >= 400000): use body search
                    var tableRuns = convertedRuns.filter(function (r) { return r.paraIndex >= 400000; });
                    if (tableRuns.length === 0) { resolve(true); return; }
                    var searchOps = tableRuns.map(function (item) {
                        var orig = item.original.trim();
                        if (!orig) return Promise.resolve();
                        var results = ctx.document.body.search(orig, { matchCase: true });
                        results.load('items');
                        return ctx.sync().then(function () {
                            results.items.forEach(function (r) {
                                r.insertText(item.converted, 'Replace');
                                r.font.name = outputFont;
                            });
                            return ctx.sync();
                        }).catch(function () {});
                    });
                    return Promise.all(searchOps).then(function () { resolve(true); });
                });
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
                        if (item.paraIndex >= 100000) return;
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
                }).then(function () {
                    var tableItems = snapshot.filter(function (r) { return r.paraIndex >= 400000; });
                    if (tableItems.length === 0) { resolve(true); return; }
                    var searchOps = tableItems.map(function (item) {
                        var conv = item.converted.trim();
                        if (!conv) return Promise.resolve();
                        var results = ctx.document.body.search(conv, { matchCase: true });
                        results.load('items');
                        return ctx.sync().then(function () {
                            results.items.forEach(function (r) {
                                r.insertText(item.original, 'Replace');
                                r.font.name = item.fontName;
                            });
                            return ctx.sync();
                        }).catch(function () {});
                    });
                    return Promise.all(searchOps).then(function () { resolve(true); });
                });
            }).catch(function (e) { reject(e.message || String(e)); });
        });
    },

    // ── Excel ─────────────────────────────────────────────────────────────────

    scanExcelDocument: function (knownBijoyFonts, bengaliMarkers) {
        return window.muktiOffice._scanExcelRange(knownBijoyFonts, bengaliMarkers, false);
    },

    // U-011: selection-only Excel scan
    scanExcelSelection: function (knownBijoyFonts, bengaliMarkers) {
        return window.muktiOffice._scanExcelRange(knownBijoyFonts, bengaliMarkers, true);
    },

    _scanExcelRange: function (knownBijoyFonts, bengaliMarkers, selectionOnly) {
        return new Promise(function (resolve, reject) {
            if (typeof Excel === 'undefined') { reject('Excel API not available'); return; }
            Excel.run(function (ctx) {
                if (selectionOnly) {
                    var range = ctx.workbook.getSelectedRange();
                    range.load(['values', 'formulas', 'rowCount', 'columnCount']);
                    return ctx.sync().then(function () {
                        return window.muktiOffice._processExcelRange(ctx, range, 0, knownBijoyFonts, bengaliMarkers);
                    }).then(function (sheetResult) {
                        resolve({ runs: sheetResult.results, warnings: sheetResult.warnings, formulaSkippedCount: sheetResult.formulaCount });
                    });
                } else {
                    var worksheets = ctx.workbook.worksheets;
                    worksheets.load('items');
                    return ctx.sync().then(function () {
                        var allResults = [], allWarnings = [], totalFormulas = 0;
                        var warnedFonts = {};
                        var sheetOps = worksheets.items.map(function (ws, si) {
                            var usedRange;
                            try { usedRange = ws.getUsedRange(); } catch (e) { return Promise.resolve(); }
                            usedRange.load(['values', 'formulas', 'rowCount', 'columnCount']);
                            return ctx.sync().then(function () {
                                return window.muktiOffice._processExcelRange(ctx, usedRange, si, knownBijoyFonts, bengaliMarkers, warnedFonts);
                            }).then(function (sheetResult) {
                                allResults = allResults.concat(sheetResult.results);
                                totalFormulas += sheetResult.formulaCount;
                                sheetResult.warnings.forEach(function (w) {
                                    if (allWarnings.indexOf(w) < 0) allWarnings.push(w);
                                });
                            }).catch(function () {});
                        });
                        return Promise.all(sheetOps).then(function () {
                            resolve({ runs: allResults, warnings: allWarnings, formulaSkippedCount: totalFormulas });
                        });
                    });
                }
            }).catch(function (e) { reject(e.message || String(e)); });
        });
    },

    _processExcelRange: function (ctx, range, sheetIndex, knownBijoyFonts, bengaliMarkers, warnedFonts) {
        warnedFonts = warnedFonts || {};
        var results = [], warnings = [], formulaCount = 0;
        var cellsToLoad = [];
        try {
            for (var r = 0; r < range.rowCount; r++) {
                for (var c = 0; c < range.columnCount; c++) {
                    var formula = range.formulas[r][c];
                    var isFormula = typeof formula === 'string' && formula.charAt(0) === '=';
                    if (isFormula) { formulaCount++; continue; }
                    var val = range.values[r][c];
                    if (val && typeof val === 'string' && val.trim()) {
                        var cell = range.getCell(r, c);
                        cell.format.font.load('name');
                        cellsToLoad.push({ cell: cell, row: r, col: c, text: String(val), sheetIndex: sheetIndex });
                    }
                }
            }
        } catch (e) {}
        return ctx.sync().then(function () {
            cellsToLoad.forEach(function (item) {
                var rawFont = item.cell.format.font.name || '';
                var fontName = rawFont.trim().toLowerCase();
                if (knownBijoyFonts.indexOf(fontName) >= 0) {
                    results.push({ text: item.text, fontName: rawFont, paraIndex: item.row, runIndex: item.col, sheetIndex: item.sheetIndex });
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
            return { results: results, warnings: warnings, formulaCount: formulaCount };
        });
    },

    applyExcelConversion: function (convertedRuns, outputFont) {
        return new Promise(function (resolve, reject) {
            if (typeof Excel === 'undefined') { reject('Excel API not available'); return; }
            Excel.run(function (ctx) {
                var worksheets = ctx.workbook.worksheets;
                worksheets.load('items');
                return ctx.sync().then(function () {
                    var ops = convertedRuns.map(function (item) {
                        var si = (item.sheetIndex !== undefined) ? item.sheetIndex : 0;
                        if (si >= worksheets.items.length) return Promise.resolve();
                        var ws = worksheets.items[si];
                        var usedRange = ws.getUsedRange();
                        usedRange.load(['values', 'rowCount', 'columnCount']);
                        return ctx.sync().then(function () {
                            if (item.paraIndex >= usedRange.rowCount || item.runIndex >= usedRange.columnCount) return;
                            var currentVal = usedRange.values[item.paraIndex][item.runIndex];
                            if (currentVal && String(currentVal).trim() === item.original.trim()) {
                                var cell = usedRange.getCell(item.paraIndex, item.runIndex);
                                cell.values = [[item.converted]];
                                cell.format.font.name = outputFont;
                            }
                            return ctx.sync();
                        }).catch(function () {});
                    });
                    return Promise.all(ops);
                }).then(function () { resolve(true); });
            }).catch(function (e) { reject(e.message || String(e)); });
        });
    },

    revertExcelConversion: function (snapshot) {
        return new Promise(function (resolve, reject) {
            if (typeof Excel === 'undefined') { reject('Excel API not available'); return; }
            Excel.run(function (ctx) {
                var worksheets = ctx.workbook.worksheets;
                worksheets.load('items');
                return ctx.sync().then(function () {
                    var ops = snapshot.map(function (item) {
                        var si = (item.sheetIndex !== undefined) ? item.sheetIndex : 0;
                        if (si >= worksheets.items.length) return Promise.resolve();
                        var ws = worksheets.items[si];
                        var usedRange = ws.getUsedRange();
                        usedRange.load(['values', 'rowCount', 'columnCount']);
                        return ctx.sync().then(function () {
                            if (item.paraIndex >= usedRange.rowCount || item.runIndex >= usedRange.columnCount) return;
                            var currentVal = usedRange.values[item.paraIndex][item.runIndex];
                            if (currentVal && String(currentVal).trim() === item.converted.trim()) {
                                var cell = usedRange.getCell(item.paraIndex, item.runIndex);
                                cell.values = [[item.original]];
                                cell.format.font.name = item.fontName;
                            }
                            return ctx.sync();
                        }).catch(function () {});
                    });
                    return Promise.all(ops);
                }).then(function () { resolve(true); });
            }).catch(function (e) { reject(e.message || String(e)); });
        });
    },

    // ── PowerPoint ───────────────────────────────────────────────────────────

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
                                                runs.items.forEach(function (run) { run.font.load('name'); });
                                                return ctx.sync().then(function () {
                                                    runs.items.forEach(function (run, ri) {
                                                        var text = run.text;
                                                        if (!text || !text.trim()) return;
                                                        var rawFont = run.font.name || '';
                                                        var fontName = rawFont.trim().toLowerCase();
                                                        if (knownBijoyFonts.indexOf(fontName) >= 0) {
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

                                // U-006: scan speaker notes (PowerPointApi 1.5+, graceful fallback)
                                var notesOp = (function () {
                                    try {
                                        var notesBody = slide.notes.body;
                                        if (!notesBody) return Promise.resolve();
                                        var notesParas = notesBody.paragraphs;
                                        notesParas.load('items');
                                        return ctx.sync().then(function () {
                                            var notesRunOps = notesParas.items.map(function (para, pi) {
                                                var runs = para.runs;
                                                runs.load('items');
                                                return ctx.sync().then(function () {
                                                    runs.items.forEach(function (run) { run.font.load('name'); });
                                                    return ctx.sync().then(function () {
                                                        runs.items.forEach(function (run, ri) {
                                                            var text = run.text;
                                                            if (!text || !text.trim()) return;
                                                            var rawFont = run.font.name || '';
                                                            var fontName = rawFont.trim().toLowerCase();
                                                            if (knownBijoyFonts.indexOf(fontName) >= 0) {
                                                                results.push({ text: text, fontName: rawFont, paraIndex: si * 10000 + 9900 + pi, runIndex: ri });
                                                            }
                                                        });
                                                    });
                                                });
                                            });
                                            return Promise.all(notesRunOps);
                                        }).catch(function () { return Promise.resolve(); });
                                    } catch (e) { return Promise.resolve(); }
                                })();

                                return Promise.all(shapeOps.concat([notesOp]));
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

                                // U-006: apply to speaker notes
                                var notesOp = (function () {
                                    try {
                                        var notesBody = slide.notes.body;
                                        if (!notesBody) return Promise.resolve();
                                        var notesParas = notesBody.paragraphs;
                                        notesParas.load('items');
                                        return ctx.sync().then(function () {
                                            return Promise.all(notesParas.items.map(function (para) {
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
                                            }));
                                        }).catch(function () { return Promise.resolve(); });
                                    } catch (e) { return Promise.resolve(); }
                                })();

                                return Promise.all(shapeOps.concat([notesOp]));
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

                                // U-006: revert speaker notes
                                var notesOp = (function () {
                                    try {
                                        var notesBody = slide.notes.body;
                                        if (!notesBody) return Promise.resolve();
                                        var notesParas = notesBody.paragraphs;
                                        notesParas.load('items');
                                        return ctx.sync().then(function () {
                                            return Promise.all(notesParas.items.map(function (para) {
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
                                            }));
                                        }).catch(function () { return Promise.resolve(); });
                                    } catch (e) { return Promise.resolve(); }
                                })();

                                return Promise.all(shapeOps.concat([notesOp]));
                            });
                        });
                    });
                    return Promise.all(slideOps);
                }).then(function () { resolve(true); });
            }).catch(function (e) { reject(e.message || String(e)); });
        });
    }
};
