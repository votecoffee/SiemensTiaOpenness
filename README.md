# TIA Portal V15.1 HMI Export / Import Tool

WinForms utility for classic WinCC Comfort / RT Advanced projects through **TIA Portal Openness V15.1**.

This version was updated against the supplied TP1200 Core App export and supports both screen-only work and a dependency-aware Complete HMI round trip.

## Requirements

- TIA Portal V15.1 with TIA Portal Openness installed
- WinCC Advanced V15.1 (for the supplied TP1200 project)
- Visual Studio with .NET Framework 4.6.2 targeting pack
- Windows user in the local `Siemens TIA Openness` group
- TIA Portal V15.1 running with the project already open

Expected Siemens assemblies:

```
C:\Program Files\Siemens\Automation\Portal V15_1\PublicAPI\V15.1\Siemens.Engineering.dll
C:\Program Files\Siemens\Automation\Portal V15_1\PublicAPI\V15.1\Siemens.Engineering.Hmi.dll
```

The references use `Copy Local = False`; Siemens DLLs are not redistributed.

## Build

Open `TiaV15_1_HmiScreenTool.sln`, select x64, then Build.

If Visual Studio cannot find the Siemens assemblies, edit the two HintPath entries in `TiaV15_1_HmiScreenTool.csproj` to match your TIA V15.1 installation.

## Typical use

1. Start TIA Portal V15.1 and open the project.
2. Start this program.
3. Click **Refresh TIA**.
4. Select the correct process and click **Attach**.
5. Accept the Siemens Openness access prompt if shown.
6. Select the HMI target.

## Screen-only functions

- **Export selected**: exports one screen or a selected screen folder.
- **Export all screens**: recursively exports all normal screens.
- **Import screen XML...**: imports one or more screen XML files into the selected screen folder.

The source aliases the ambiguous Siemens screen type as:

```csharp
using HmiScreen = Siemens.Engineering.Hmi.Screen.Screen;
```

## Export Complete HMI

Creates a timestamped export folder with these categories:

```
ProjectGraphics\
Connections\
Cycles\
VBScripts\
TagTables\
TextLists\
GraphicLists\
ScreenTemplates\
PopupScreens\
SlideInScreens\
PermanentArea\
Screens\
ExportSummary.txt
```

Project graphics are each exported to their own subfolder because the Siemens XML may reference sidecar image files by relative path.

## Import Complete HMI

Select the root folder created by **Export Complete HMI**. The importer restores data in dependency-aware order:

1. Project graphics
2. Connections
3. Cycles (missing only; existing cycles are always skipped)
4. VB scripts
5. HMI tag tables
6. Text lists
7. Graphic lists
8. Screen templates
9. Pop-up screens
10. Slide-in screens
11. Permanent area
12. Normal screens

The folder hierarchy for tags, scripts, templates, popups and normal screens is recreated when needed.

### Overwrite behavior

The **Overwrite existing screen** checkbox is also used by Complete HMI import as the general overwrite switch:

- unchecked: `ImportOptions.None`
- checked: `ImportOptions.Override`

Cycles are the exception: existing cycles are never overwritten. Only missing cycles are imported with `ImportOptions.None`. Siemens warns that attempting to change non-editable attributes of standard cycles can cause a NonRecoverableException and close TIA Portal.

### Project save behavior

Imports do **not** automatically save the TIA project. After import:

1. inspect the modified HMI,
2. compile it in TIA Portal,
3. review warnings/errors,
4. click **Save project** only when satisfied.

The importer writes an `ImportSummary-YYYYMMDD-HHMMSS.txt` report into the selected export root.

## Dependency findings from the supplied TP1200 export

The supplied complete export contained:

- 100 normal screens
- 129 tag-table XML files
- 151 text lists
- 11 VB scripts
- 3 graphic lists
- 1 connection
- 20 cycles
- 1 screen template
- 6 pop-up screens
- 4 slide-in screens
- 1 permanent area

The HMI tag XML contains Open Links to `Connection_1` and acquisition cycles. Several HMI tag event definitions also reference exported VB scripts. Some text lists contain Open Links to HMI tags. Screens then reference tags, text lists, graphic lists/project graphics, the common template, and other screens. That is why the Complete HMI importer does not simply import the folders alphabetically.

## Important limitations

- This tool targets classic WinCC HMI (`Siemens.Engineering.Hmi.HmiTarget`), not WinCC Unified.
- Screen imports require a compatible/same HMI device type and valid screen dimensions/numbers.
- Integrated HMI connections are not exportable through this classic connection export mechanism.
- Complete HMI import is safest as a round trip into the same project/device family. Cross-project imports can still depend on external PLC objects, libraries, authorization configuration, styles, or other project-level resources not represented by an individual HMI XML file.
- Project text translation XLSX export/import is intentionally not included because Siemens restricts re-import of exported project texts to the project from which they originated.
- Always archive/backup the TIA project before doing bulk imports.
\n\n## Screens-only recovery import\n\nThis build adds **Import Screens Only** for cases where a Complete HMI import has already successfully imported the slow dependency categories (tag tables, text lists, scripts, graphics, etc.) but the normal Screens category did not finish.\n\n- Select either the Complete HMI export root or its `Screens` folder.\n- The utility imports **normal screens only**. It does not touch tags, text lists, scripts, project graphics, connections, cycles, templates, pop-ups, slide-ins, or the permanent area.\n- Screen folder hierarchy is recreated as needed.\n- Each screen XML is imported separately and logged as `OK` or `FAILED`; a failure/cancellation does not stop the remaining screens.\n- A `ScreenImportSummary-YYYYMMDD-HHMMSS.txt` report is written beside the export.\n- The existing **Overwrite existing objects** checkbox controls `ImportOptions.Override` vs `ImportOptions.None`. For restoring the merged CG2085 package into an existing Core project, leave **Overwrite existing objects checked**.\n- The project is never saved automatically.\n

## UI visibility fix

The action bar now wraps to a second row and scrolls if required. This prevents **Import Screens Only** from being pushed off-screen by Windows display/DPI scaling. The window title reads **TIA Portal V15.1 - HMI Tool - Screens Only Build** so this version is easy to identify.
