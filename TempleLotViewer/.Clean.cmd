@echo off
echo Do you want to clean all BIN and OBJ folders
pause
@echo Deleting all BIN and OBJ folders
for /d /r . %%d in (bin,obj) do @if exist "%%d" rd /s/q "%%d"
@echo BIN and OBJ folders successfully deleted
pause > nul