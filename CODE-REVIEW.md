# Emergency Passport Tracker — code review and fixes

Reviewed: `MainForm.cs`, `Program.cs`, `Services/DataService.cs`, `Security/SecurityHelper.cs`, `Models/*`.

---

## 1. The data-loss bug

**What happened.** `DataService.Load()` caught the decryption failure, showed a message box, and
then returned `(new(), new())` — two empty lists. `MainForm.Login()` accepted that as the real
contents of the database. From that moment the app was running with zero records, and the very
next write (adding passports, clicking a cell, or pressing Save) called
`dataService.Save(records, audit, pin)` and re-encrypted *nothing* over `eptdata.enc`.

The original file was gone, and it was gone under the *wrong* code, so even the correct code
would not have brought it back.

There was a second, quieter path to the same place. AES-CBC with PKCS7 padding does not
authenticate anything, so a wrong key does not reliably throw. I measured this against a file in
the original format: **73 of 20,000 wrong codes — about 1 in 273 — decrypted without a padding
error**, producing garbage that failed JSON parsing. The old code caught `JsonException`,
showed a message, and returned empty lists. Same wipe, no exception in sight.

**Fixed.**

* `Load()` now throws `InvalidPinException` instead of returning empty lists. Empty lists are
  returned only when there genuinely is no data file yet.
* `Program.Main` unlocks the file *before* the main window opens. A wrong code shows
  "Incorrect code. Please try again" and re-prompts. Cancel exits the application without ever
  touching the data file.
* Because nothing is loaded until the code is right, there is no longer a state in which the app
  can save over good data with nothing.
* New file format **EPT2** adds an HMAC-SHA256 tag over the header and ciphertext, so a wrong
  code is now caught with certainty rather than by hoping the padding fails. Verified against
  2,000 wrong codes: **all 2,000 detected, none accepted.**
* Old files still open. `EPT1` and the original headerless `salt|iv|ciphertext` layout are both
  read, and are upgraded to EPT2 on the next save. Key derivation is byte-identical to before, so
  your existing data file and code keep working.

---

## 2. Backup and restore

New `Services/BackupService.cs`, plus two buttons on the main window.

* **Backup to CSV** — pick a folder, get `EPT-Records-<timestamp>.csv` and
  `EPT-Audit-<timestamp>.csv`. Everything is exported: all record fields including `Locked`, and
  the complete audit log.
* **Restore from CSV** — pick the records file; the matching audit file is found automatically.
  The current encrypted database is copied to a `Backups` folder *before* anything is replaced.
* The whole file is parsed and validated before any data is touched — unknown status values,
  duplicate passport numbers, unparseable dates and wrong column counts all abort the restore
  with the offending line number, so a bad backup cannot leave you half-restored.
* Proper RFC 4180 handling: commas, quotes, embedded newlines and non-ASCII names all survive a
  round trip (verified). Dates are `yyyy-MM-dd` and culture-invariant, so a backup made on one
  machine restores identically on another.

These files are **not encrypted**, as you asked — the code says so in three places, including the
dialog you see after every backup.

One caveat worth knowing: passport numbers keep their leading zeros *in the file*, but Excel
displays `00012345` as `12345` if you just double-click the CSV. Restore reads the file directly,
so the stored data is never affected.

---

## 3. Other errors found

| # | Problem | Fix |
|---|---------|-----|
| 1 | `DataGrid_CellClick` called `UpdateRecord()`. **Every single click on any cell** wrote an audit entry and re-encrypted the whole database. Clicking around the grid quietly filled the audit log with fake "Updated" entries. | Handler removed. Updates now come only from a completed edit. |
| 2 | Setting status to **B** locked the record immediately — so you could never type in who it was issued to. The locked-record path also returned *before* writing its audit entry. | Locks only once *Issued to* and *Date issued* are filled in, and only after a confirmation prompt. |
| 3 | `int.Parse(txtStart.Text)` on the Add button. Any non-numeric input threw `FormatException` and killed the app. | `int.TryParse` with a range check and a clear message. |
| 4 | No sanity check on the range. `1` to `99999999` would have tried to create 100 million records and hung the app. | Capped at 10,000 per operation, confirmation above 100, and a report of how many were added vs. already on file. |
| 5 | `PrintPage` drew every record at 20px intervals with no pagination. **Anything past the bottom of page one was silently dropped from the printed log.** | Proper pagination via `HasMorePages`, page numbers, and the signature block on the last page only. |
| 6 | Search lower-cased the search text, then compared it against `PassportNumber` and `Status` case-sensitively — so searching "a" never matched status `A`. `r.PassportNumber.Contains` also threw on a null field. | Case-insensitive and null-safe across number, name, notes and status. Live filtering, Enter to search, Escape to clear. |
| 7 | `BackupData()` did `File.Copy` with no existence check (throws on first run), wrote into the application directory (access denied under Program Files), and ran on *every* save, littering the folder. | Writes to a `Backups` sub-folder next to the data file, only when you press Save. |
| 8 | `RestoreData()` was `private`, never called from anywhere, and didn't reload the data. Dead code. | Replaced by the real restore. |
| 9 | `Save()` wrote straight over `eptdata.enc`. A crash or power loss mid-write left a truncated file — permanently unreadable, no backup. | Writes to a temp file and swaps it in atomically; the previous version is kept as `.bak`. |
| 10 | Data file path was the bare relative name `"eptdata.enc"`, resolved against the *current working directory*. Launching from a shortcut with a different start-in folder silently created a second, empty database. | Absolute path. Existing file next to the exe keeps being used; otherwise `%LOCALAPPDATA%`. |
| 11 | The PIN was typed into `Microsoft.VisualBasic.Interaction.InputBox` — visible in clear text on screen, no confirmation on first run, and Cancel returned `""` which was accepted as a code. | Masked dialog, entered twice when creating a new database, Cancel exits cleanly. |
| 12 | `Rfc2898DeriveBytes` constructors are obsolete in .NET 9 (SYSLIB0060) — build warnings. | Static `Rfc2898DeriveBytes.Pbkdf2`. Output is byte-identical, so existing files still decrypt. |
| 13 | `IsValidStatus()` existed but was never called — any text could be typed into the status column. | Status is a drop-down limited to A/B/C/D, with validation behind it. |
| 14 | `DataWrapper` was declared twice (once in `MainForm`, once in `DataService`); `OldUpdateRecord` was a dead copy of `UpdateRecord`; `MainForm` imported `System.Security.Cryptography`, `System.Text.Json` and `System.Drawing.Imaging` without using them. | Removed. |
| 15 | Nullable reference types were enabled but the models declared non-nullable `string` properties with no initialiser, and `DataWrapper wrapper = null` — a wall of CS8618/CS8600 warnings. | Models initialise or declare `string?` honestly. |
| 16 | `ExportPdf` wrote to the relative path `"EPTLog.PDF"`, silently overwrote it, threw an unhandled exception if the file was open in a reader, and `using` on both `PdfWriter` and `PdfDocument` disposed them a second time after `doc.Close()`. | Save dialog, proper disposal, handled errors, and a real table layout. |
| 17 | Passport number and Locked were editable in the grid — the primary key could be typed over and a record could be unlocked by unticking a box. | Both columns are read-only; locked rows are greyed out and reject edits entirely. |
| 18 | No save on close. Any pending change made outside an immediate save was lost. | Saves on close, and asks before discarding if the save fails. |
| 19 | The audit log was shown in a `MessageBox` — it truncates, cannot be scrolled or copied, and grows unusable. | Scrollable, selectable window, newest first, capped at 500 entries with a count of the rest. |
| 20 | Failed saves were invisible: `Save()` could throw and nothing caught it. | Save failures are reported, and the on-screen data is kept so you can retry. |

---

## 4. Verification

No .NET SDK was reachable from the sandbox I ran this in (the Microsoft and NuGet endpoints are
blocked), so the first build was done by Jesper in Visual Studio. It surfaced two problems in
`MainForm.cs`, both now fixed:

* **CS0104, 20 occurrences.** `iText.Kernel.Geom` declares its own `Point`, `Path` and
  `Rectangle`, which collide with `System.Drawing.Point`, `System.IO.Path` and
  `System.Drawing.Rectangle`. iText is now imported through using-aliases
  (`PdfParagraph`, `PdfTable`, `PdfCell`, ...) instead of plain `using` directives, so nothing
  from iText leaks into the rest of the form's namespace.
* **CS1061.** `Paragraph` has no `SetBold()` in iText 9. Bold now comes from
  `PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)` and `SetFont()`.
* One CS8600 warning: `resources.GetObject()` returns `object?`, cast to a non-nullable `Image`.
  Now cast to `Image?`.

Everything else compiled clean on that first pass, including `DataService`, `BackupService`,
`PinPromptForm`, `Program` and the models.

What I did verify, by re-implementing the exact byte layout and CSV rules and testing them
directly:

* PBKDF2 derivation is unchanged — the 64-byte derivation starts with exactly the old 32-byte key,
  which is what keeps existing data files readable.
* EPT2 round trip, including a 5,000-record payload.
* 2,000 wrong codes against an EPT2 file: all 2,000 reported as wrong, none accepted.
* 20,000 wrong codes against a legacy-format file: 73 got past the padding check (1 in 273) —
  the flaw that motivated the HMAC. All are now classified as a wrong code, never as empty data.
* A single flipped ciphertext bit is detected; a truncated file reports damage rather than
  returning empty data; a zero-length file is treated as a new database.
* CSV round trip with commas, embedded quotes, embedded newlines, leading/trailing spaces,
  leading zeros, non-ASCII names, CRLF and lone-CR line endings, blank lines and a BOM.

Worth testing by hand once it builds: printing a log longer than one page, and a restore from a
backup taken on the same machine.

---

## 5. Files changed

```
MainForm.cs               rewritten
Program.cs                rewritten (unlock happens before the window opens)
Services/DataService.cs   rewritten (EPT2 format, safe failures, atomic writes)
Services/BackupService.cs new (CSV backup/restore)
UI/PinPromptForm.cs       new (masked code entry)
Security/SecurityHelper.cs updated (non-obsolete PBKDF2, two-key derivation)
Models/*.cs               updated (nullability)
README.md                 updated
```

No new NuGet packages. The `Microsoft.VisualBasic` dependency is gone.
