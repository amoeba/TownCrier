#####
# Installer for TownCrier


##### Globals
!define APPNAME "TownCrier"
!define GUID "{F86E1BAE-C2F8-4060-9B98-CD74EB41EB6E}"
!define SURROGATE "{71A69713-6593-47EC-0002-0000000DECA1}"
!define DLL "TownCrier.dll"
!define NewtonsoftJSON "Newtonsoft.Json.dll"


##### Settings
OutFile "InstallTownCrier_v1.3.1.exe"
InstallDir "$PROGRAMFILES\${APPNAME}"
 

##### Pages
page directory
page instfiles


##### Installer section
# default section start
Section
 
SetOutPath $INSTDIR
File "bin\Release\${DLL}"
File "bin\Release\${NewtonsoftJSON}"
WriteUninstaller $INSTDIR\Uninstall.exe
 
# Registry
WriteRegStr HKLM "SOFTWARE\Decal\Plugins\${GUID}" "" "${APPNAME}"
WriteRegStr HKLM "SOFTWARE\Decal\Plugins\${GUID}" "Assembly" "${DLL}"
WriteRegDWORD HKLM "SOFTWARE\Decal\Plugins\${GUID}" "Enabled" 0x01
WriteRegStr HKLM "SOFTWARE\Decal\Plugins\${GUID}" "Object" "${APPNAME}.PluginCore"
WriteRegStr HKLM "SOFTWARE\Decal\Plugins\${GUID}" "Path" "$INSTDIR"
WriteRegStr HKLM "SOFTWARE\Decal\Plugins\${GUID}" "Surrogate" "${SURROGATE}"

SectionEnd


##### Uninstaller section
Section "un.Uninstall"
 
delete $INSTDIR\Uninstall.exe
delete "$INSTDIR\${DLL}"
delete "$INSTDIR\${NewtonsoftJSON}"
rmDir $INSTDIR
DeleteRegKey HKLM "SOFTWARE\Decal\Plugins\${GUID}"

SectionEnd