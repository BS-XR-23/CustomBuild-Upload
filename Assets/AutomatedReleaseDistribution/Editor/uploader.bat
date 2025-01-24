@echo off
setlocal enabledelayedexpansion

:: Constants
set "ACCESS_TOKEN=[access_token]"
set "PARENT_FOLDER_ID=[folder_id]"
set "BUILD_PATH=[build_path]"
set "BUILD_FOLDER=[build_folder]"
for %%i in ("%BUILD_PATH%") do set "FILE_NAME=%%~nxi"
set "RELEASE_DATA_PATH=[release_data_path]"
for %%i in ("%RELEASE_DATA_PATH%") do set "RELEASE_DATA_FILE_NAME=%%~nxi"

echo =============================Build Compressing......==============================
pushd "%BUILD_FOLDER%"
tar.exe -a -cf "%BUILD_PATH%" *
popd
set NEW_FILE_ID=""
set FILE_ID=""

echo =============================Checking Existing File==============================
:: Function to get file ID from Google Drive
call:get_fileid %FILE_NAME% %PARENT_FOLDER_ID%

echo =============================Build Uploading====================================
  :: Main Script Execution
if %FILE_ID%=="" (
  set NEW_FILE_ID=""
  call :upload_file %FILE_NAME% %PARENT_FOLDER_ID% \"%BUILD_PATH%\"
  echo ==================Uploaded File Id:%NEW_FILE_ID%=========================
) else (
  call :update_file %FILE_NAME% %FILE_ID% %PARENT_FOLDER_ID% \"%BUILD_PATH%\"
  set NEW_FILE_ID=%FILE_ID%
)
echo ==================Build Uploaded. New File Id:%NEW_FILE_ID%=========================

call :update_file_id
call :upload_or_update_release_data
echo ==============================Process Done=========================================
exit /b

:get_fileid
  set file_name=%1
  set folder_id=%2
  set FILE_ID=""
  for /f "tokens=2 delims=:," %%i in ('curl  -s -X GET ^
    "https://www.googleapis.com/drive/v3/files?q=name%%3D%%27%file_name%%%27%%20and%%20%%27%folder_id%%%27%%20in%%20parents%%20and%%20trashed%%3Dfalse" ^
    -H "Authorization: Bearer %ACCESS_TOKEN%" ^
    -H "Content-Type: application/json"  ^ 
    ^| findstr /i "id"') do ( set FILE_ID=%%i )
  :: Check if the FILE_ID is empty, meaning no file was found
  if %FILE_ID%=="" (
      echo ======================No file found with the given name and parent folder======================
  ) else (
      :: Clean up the FILE_ID by removing any extra quotes or characters
      set FILE_ID=%FILE_ID:"=%
      set FILE_ID=%FILE_ID: =%
      echo File ID found: %FILE_ID%
  )
goto :eof



@REM :: Function to upload a new file
:upload_file
  set file_name=%1
  set folder_id=%2
  set build_path=%3
  echo =============================Uploading file: %FILE_NAME% =======================================
  set NEW_FILE_ID=
  for /f "tokens=2 delims=:," %%i in ('curl -v -s -X POST ^
  "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart" --progress-bar ^
  -H "Authorization: Bearer %ACCESS_TOKEN%" ^
  -F "metadata={\"name\":\"%file_name%\",\"parents\":[\"%folder_id%\"]};type=application/json" ^
  -F "file=@%build_path%"  ^ 
  ^| findstr /i "id"')  do ( set NEW_FILE_ID=%%i )

    if %NEW_FILE_ID%=="" (
      echo ======================No file found with the given name in parent folder======================
  ) else (
      :: Clean up the FILE_ID by removing any extra quotes or characters
      set NEW_FILE_ID=%NEW_FILE_ID:"=%
      set NEW_FILE_ID=%NEW_FILE_ID: =%
      echo New File ID: %NEW_FILE_ID%
  )
  
goto :eof

:: Function to update an existing file
:update_file
  set file_name=%1
  set file_id=%2
  set folder_id=%3
  set build_path=%4
  echo =============================Updating existing file with Id:%file_id%=======================================
  curl -v -s -X  PATCH ^
    "https://www.googleapis.com/upload/drive/v3/files/%file_id%?uploadType=multipart&addParents=%folder_id%&removeParents=%folder_id%" ^
    -H "Authorization: Bearer %ACCESS_TOKEN%" ^
    -F "metadata={\"name\":\"%file_name%\"};type=application/json" ^
    -F "file=@%build_path%"
    set NEW_FILE_ID=%FILE_ID%
goto :eof

:update_file_id
  (for /f "delims=" %%i in ('type "%RELEASE_DATA_PATH%"') do (
      set "line=%%i"
      setlocal enabledelayedexpansion
      echo !line:"[file_id]"=%NEW_FILE_ID%!
      endlocal
  )) > "%RELEASE_DATA_PATH%.tmp"

  :: Overwrite the original file with the updated file
  move /y "%RELEASE_DATA_PATH%.tmp" "%RELEASE_DATA_PATH%"
goto :eof


:: Function to upload or update release data
:upload_or_update_release_data
  :: Create the release data JSON file
  set RELEASE_FILE_ID=""
  call:get_fileid %RELEASE_DATA_FILE_NAME% %PARENT_FOLDER_ID%
  set RELEASE_FILE_ID=%FILE_ID%
  echo RELEASE_FILE_ID: %RELEASE_FILE_ID%
  echo =============================Uploading Release Metadata==============================
  if %RELEASE_FILE_ID%=="" (
    call:upload_file %RELEASE_DATA_FILE_NAME% %PARENT_FOLDER_ID% \"%RELEASE_DATA_PATH%\"
  )
  else (
    call:update_file %RELEASE_DATA_FILE_NAME% %RELEASE_FILE_ID% %PARENT_FOLDER_ID% \"%RELEASE_DATA_PATH%\"
  )
goto :eof



