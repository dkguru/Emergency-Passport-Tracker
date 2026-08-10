# Emergency Passport Tracker

A secure, offline Windows Forms application for tracking the issuance and inventory of emergency passports.
Designed for consular and administrative use, the system ensures **data integrity, auditability, and encryption**.

---

## Key Features

### Security

* AES-256-CBC encrypted local data storage (`eptdata.enc`)
* HMAC-SHA256 integrity tag, so a wrong code is detected with certainty
* PIN-based access control, entered masked
* PBKDF2 key derivation (salted, 100,000 iterations, SHA-256)
* No plaintext data stored on disk by the application itself
* Atomic saves: an interrupted write cannot corrupt the data file

### Data Integrity

* Append-only record system (no deletion)
* Automatic audit log for all changes
* Record locking after issuance

### Passport Management

* Add single passport numbers or ranges
* Automatic formatting (8-digit numbers)
* Duplicate prevention
* Status tracking:

  * `A` = In possession
  * `B` = Issued (locks record)
  * `C` = Missing
  * `D` = Destroyed

### User Interface

* DataGrid view of all records
* Search/filter functionality
* Print preview and printing
* Export to PDF

### Backup

* Export all records and the full audit log to CSV
* Restore from CSV, with a safety copy of the encrypted file taken first
* CSV backups are deliberately **not encrypted** - store them on an encrypted drive

### Output

* Printable log matching official form layout, paginated
* PDF export for digital archiving

---

## Project Structure

```
EmergencyPassportTracker/
?
??? Models/
?   ??? PassportRecord.cs
?   ??? User.cs
?   ??? AuditEntry.cs
?
??? Services/
?   ??? DataService.cs
?
??? Security/
?   ??? SecurityHelper.cs
?
??? MainForm.cs
??? Program.cs
??? data.enc (generated at runtime)
```

---

## Getting Started

### Requirements

To run the installed application: **Windows 10 or later, 64-bit.** Nothing else - the .NET
runtime is bundled.

To build from source:

* Windows
* .NET 10 SDK
* Visual Studio 2022+
* Inno Setup 6.3+ (only if you are building the installer)

### Run the Application

1. Open the solution in Visual Studio
2. Build and run (`F5`)
3. Enter a PIN when prompted

### Install the Application

Run `EmergencyPassportTracker-Setup-<version>.exe`. It installs for the current user only,
needs no administrator rights, and includes the .NET runtime, so nothing has to be installed
on the PC beforehand.

To build that installer yourself, see [INSTALLER.md](INSTALLER.md).

> The PIN is required to decrypt the data file.
> If lost, the data cannot be recovered.

---

## Data Storage

* All data is stored in `eptdata.enc`.
* If a data file already exists next to the executable, that one keeps being used.
  Otherwise a new one is created in:

  ```
  %LOCALAPPDATA%\EmergencyPassportTracker\eptdata.enc
  ```

  (the application directory is not writable when the app is installed into Program Files).
* Encrypted with AES-256-CBC; the key is derived from the access code with PBKDF2.
* Every save is written to a temporary file first and then swapped in, and the previous
  version is kept as `eptdata.enc.bak`.
* Occasional encrypted copies are written to a `Backups` sub-folder.

### File format

```
"EPT2" | salt length | iv length | salt | iv | HMAC-SHA256 (32 bytes) | ciphertext
```

Files written by earlier versions (`EPT1`, and the original headerless
`salt | iv | ciphertext` layout) are still read, and are upgraded to `EPT2` on the next save.

---

## Usage

### Add Passport Numbers

* Enter a start and end number
* Click **"Add Passports"**
* The system will:

  * Format numbers to 8 digits
  * Prevent duplicates
  * Log the action

---

### Edit Records

* Records can be updated until they are locked.
* Setting the status to **B (Issued)** starts the issuance: fill in *Issued to* and
  *Date issued* as well. Once all three are present the application asks whether to lock
  the record, and only locks it after you confirm.
* Once locked, a record can never be changed again. Locked rows are greyed out.
* Status can only be set to A, B, C or D.

---

### Search

* Use the search box to filter by:

  * Passport number
  * Name
  * Status

---

### Print

* Click **"Print Log"**
* Preview before printing
* Layout matches official inventory format

---

### Export to PDF

* Exports all records into a structured PDF file
* Choose the destination with the file dialog

---

### Backup and Restore

**Backup to CSV** writes two files into a folder you choose:

```
EPT-Records-yyyyMMdd-HHmmss.csv
EPT-Audit-yyyyMMdd-HHmmss.csv
```

Dates are written as `yyyy-MM-dd`, values are RFC 4180 quoted, and the files are UTF-8.
They are **not encrypted**.

**Restore from CSV** reads those files back. Before anything is replaced the current
encrypted data file is copied to the `Backups` folder. The whole file is validated before
the restore starts, so a malformed backup cannot leave the tracker half-populated.

> Note: passport numbers keep their leading zeros in the file, but Excel may display
> `00012345` as `12345` if you double-click the CSV. Import it as text if that matters.
> Restoring reads the file directly, so the stored values are never affected.

---

## Audit Log

Every action is recorded:

* Record creation
* Record updates
* Status changes

Stored securely alongside main data.

---

## Important Notes

### No Deletion Policy

This system is designed as an **audit-safe ledger**:

* Records cannot be deleted
* This ensures full traceability

---

### Encryption Warning

* If the code is incorrect, the application says so and asks again.
  **It does not open, does not clear anything, and does not write to the data file.**
* If the code is lost, the data is permanently inaccessible. There is no recovery.
* CSV backups are not protected in any way. Anyone who can open the file can read every
  record, so keep them on an encrypted drive.

---

### Known Limitations

* No cloud sync (offline-first design)
* No password recovery
* Basic UI (WinForms)
* CSV backups are unencrypted by design

---

## Future Improvements

Potential enhancements:

* Multi-user authentication
* Role-based access control
* Cloud backup (encrypted)
* Encrypted export format alongside the plain CSV
* Barcode/QR scanning
* Installer packaging

---

## License

Internal / private use.
Adapt as needed for your organization.

---

## Author

Developed for consular passport tracking and inventory management by Jesper Angelo, Lake Oswego, OR

---
