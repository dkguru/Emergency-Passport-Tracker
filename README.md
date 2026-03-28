# Emergency Passport Tracker

A secure, offline Windows Forms application for tracking the issuance and inventory of emergency passports.
Designed for consular and administrative use, the system ensures **data integrity, auditability, and encryption**.

---

## Key Features

### Security

* AES-encrypted local data storage (`eptdata.enc`)
* PIN-based access control
* PBKDF2 key derivation (salted, 100,000 iterations)
* No plaintext data stored on disk

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

### Output

* Printable log matching official form layout
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

* Windows
* .NET 6 or later
* Visual Studio 2022+

### Run the Application

1. Open the solution in Visual Studio
2. Build and run (`F5`)
3. Enter a PIN when prompted

> The PIN is required to decrypt the data file.
> If lost, the data cannot be recovered.

---

## Data Storage

* All data is stored in:

  ```
  eptdata.enc
  ```
* Located in the application directory
* Fully encrypted using AES

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

* Records can be updated until:

  * Status is set to **B (Issued)**
* After that:

  * Record becomes locked
  * No further changes allowed

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

* If the PIN is incorrect ? decryption fails
* If the PIN is lost ? data is permanently inaccessible

---

### Known Limitations

* No cloud sync (offline-first design)
* No password recovery
* Basic UI (WinForms)

---

## Future Improvements

Potential enhancements:

* Multi-user authentication
* Role-based access control
* Cloud backup (encrypted)
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
