# Opure Foundation Preview 1 (v0.1.0-preview.1)

Welcome to the first pre-release preview of Opure, a local-first, developer-controlled software engineering platform. This release marks the completion of foundational milestones up to **FND-055**.

## What's Included
* **Safe Project Boundary & Controlled Execution:** Opure runs in a strictly bounded local environment with a custom MSIX Bootstrap and process supervisor, ensuring there is no hidden AI autonomy or unchecked system access.
* **Disconnected Runtime & Platform State:** Operates fully offline without external AI calls by default. Bounded by a deterministic, transparent SQLite backend.
* **Configuration & Policy Engine:** A robust project configuration pipeline evaluating hierarchical project schemas and product policies, building effective snapshots without hidden fallbacks.
* **Trust Evidence Database:** A central engine for reconciling, auditing, and querying project evidence and security invariants before actions are authorized.
* **Independent Desktop UI Projections:** An Avalonia-based Command and Projection layer disconnected from direct domain authority, ensuring visual safety.

## How to Install

Opure is packaged as an MSIX bundle for Windows 11. Because this is an early developer preview, it is signed with a local test certificate. 

**You must trust the included certificate before installing the package.**

1. Download both `Opure.Preview-1.1.0.10000-win-x64.msix` and `OpureTestCert.pfx` from the assets below.
2. Double-click `OpureTestCert.pfx`.
3. Select **Local Machine** as the Store Location and click Next. (You may need Administrator privileges).
4. Leave the password blank and click Next.
5. Choose **"Place all certificates in the following store"**, click Browse, and select **"Trusted People"** (or **"Trusted Root Certification Authorities"**).
6. Finish the import wizard.
7. You may now double-click the `Opure.Preview-1.1.0.10000-win-x64.msix` file to install Opure!

*Note: You may also need to ensure Windows Developer Mode or side-loading is permitted in your Windows settings depending on your OS configuration.*
