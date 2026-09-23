# JuanTool

A small native Windows launcher inspired by uTools. Press **Alt+Space**, type a bookmark keyword or paste text, and go.

## Use

Run `artifacts/JuanTool/JuanTool.exe`, or double-click **Start JuanTool.cmd** after building. JuanTool stays in the notification area. No administrator access or API key is needed.

| Action | Shortcut |
| --- | --- |
| Show / hide | Alt+Space (configurable in Settings) |
| Select a bookmark | Up / Down |
| Open selected bookmark; Google if none match | Enter |
| Search Google regardless of bookmark matches | Ctrl+Enter |
| Ask ChatGPT | Shift+Enter |
| Hide | Escape or click outside |
| Settings / Quit | Right-click the tray icon |

Google and ChatGPT buttons appear directly under the input. Ordinary Ctrl+V pastes text into the bar. Bookmark titles, URLs, folders and Chrome profile names are searchable. All local Chrome profiles are loaded again whenever the launcher opens. Links open in Chrome, using the bookmark's original profile; if Chrome is not installed, the default browser is used.

The Chrome bookmarks section shows all bookmarks when the input is empty, and all matching bookmarks when you type; scroll to reach the rest of the list. It folds automatically when your text has no bookmark matches. Matching text or clearing the input expands it again. Click the section header to expand or collapse it manually. The Google and ChatGPT buttons use bundled official icons; their sources are recorded in [Assets/README.md](src/JuanTool/Assets/README.md).

**Chrome can be closed while you search bookmarks. Opening a bookmark or a web search launches the browser.** This version does not embed websites or AI answers in the launcher.

The ChatGPT button opens `https://chatgpt.com/?q=...` and copies the text to your clipboard. This URL parameter is a best-effort browser handoff, not a documented OpenAI API contract. If ChatGPT does not fill the prompt, paste with Ctrl+V; you may need to sign in or submit the prompt. Text longer than 6,000 characters is copied and opens the ChatGPT homepage to avoid URL limits. Text-only input is supported, not image or file uploads.

## Settings

Click the gear or use the tray menu. Click the shortcut field and press your desired combination. Windows-reserved shortcuts and shortcuts held by another app cannot be registered. Choose another combination if Alt+Space is taken (for example by PowerToys Run). Single function keys F1–F11 are supported; ordinary typing keys require a modifier so the app does not intercept normal typing.

Chrome's default bookmark location is `%LOCALAPPDATA%\Google\Chrome\User Data`. For a custom Chrome installation, choose its **User Data** folder containing `Default` / `Profile 1` etc. Only saved web bookmarks (`http` / `https`) are shown; bookmarklets and local files are excluded.

“Start JuanTool when I sign in to Windows” is optional and off initially. It uses the current user's Windows Run entry. Keep the executable in a stable folder before enabling it. Disable this option before moving or removing the app.

Preferences are stored in `%LOCALAPPDATA%\JuanTool\settings.json`. Bookmark data and typed queries are not stored by JuanTool or sent anywhere while typing. A selected search sends your text to the selected website. Clicking ChatGPT explicitly replaces your clipboard with the prompt as a fallback.

## Build and test

Requires Windows 10/11 and the .NET 10 SDK. There are no third-party NuGet dependencies.

```powershell
powershell -ExecutionPolicy Bypass -File scripts/build.ps1
```

The default build requires the .NET 10 Desktop Runtime on the target PC. To include the runtime for distribution:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/build.ps1 -SelfContained
```

The standalone build needs network access to download Microsoft's runtime packs. Output is in `artifacts/JuanTool`. Build outputs, local tools and settings are excluded from Git.

Run the dependency-free regression suite separately:

```powershell
dotnet run --project tests/JuanTool.Tests -c Release
```

## Windows installer and release

Install [Inno Setup](https://jrsoftware.org/isdl.php) once:

```powershell
winget install --id JRSoftware.InnoSetup.7 -e -s winget -i
```

If you already ran `scripts/build.ps1 -SelfContained`, package that executable:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/build-installer.ps1 -SkipBuild
```

For later releases, quit JuanTool from the tray and omit `-SkipBuild` to run the tests, publish a fresh self-contained app, and compile the installer. Use `-SkipBuild` only with a self-contained build. A custom Inno Setup installation can be selected with `-CompilerPath 'C:\path\to\ISCC.exe'`.

The output is `artifacts/Installer/JuanTool-Setup-<version>-win-x64.exe`. The version comes from the published executable; update `Version` in `src/JuanTool/JuanTool.csproj` before building a new release. The installer targets Windows 11 on x64-compatible systems, installs for the current user without administrator access, adds a Start menu shortcut, offers a desktop shortcut, and provides an uninstaller. It preserves settings on uninstall and removes the login startup entry only when it points to that installation.

Test the setup program and installed app before distribution. Quit any running JuanTool instance before installing or uninstalling. Then commit and push the release's source, open [GitHub Releases](https://github.com/Duohewater/JuanTool/releases), draft a release with a matching version tag, attach the setup executable, and publish. The current Actions workflow produces app build artifacts; it does not publish GitHub Releases. Installer signing is not configured.

## Implementation

WPF provides the floating UI. Windows `RegisterHotKey` provides the global shortcut. A tray icon and a per-session singleton keep the app available without duplicate background instances. Bookmark JSON is read with shared access; malformed or inaccessible profiles do not prevent other profiles from loading. Browser arguments are passed separately, and only HTTP(S) URLs may be opened.

References: [Windows global hotkeys](https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-registerhotkey), [Chrome user data directories](https://chromium.googlesource.com/chromium/src/+/HEAD/docs/user_data_dir.md).
