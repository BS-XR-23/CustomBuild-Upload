#!/bin/bash

# Constants
ACCESS_TOKEN="[access_token]"
PARENT_FOLDER_ID="[folder_id]"
BUILD_PATH="[build_path]"
BUILD_FOLDER="[build_folder]"
FILE_NAME=$(basename "$BUILD_PATH")
RELEASE_DATA_PATH="[release_data_path]"
RELEASE_DATA_FILE_NAME=$(basename "$RELEASE_DATA_PATH")


# Compress the build folder
echo "Build Compressing......"
tar -acf "$BUILD_PATH" -C "$BUILD_FOLDER" .

NEW_FILE_ID=""
FILE_ID=""

# Function to get file ID from Google Drive
get_fileid() {
  local file_name="$1"
  local folder_id="$2"

  FILE_ID=$(curl -s -X GET \
    "https://www.googleapis.com/drive/v3/files?q=name='${file_name}'%20and%20'${folder_id}'%20in%20parents%20and%20trashed=false" \
    -H "Authorization: Bearer $ACCESS_TOKEN" \
    -H "Content-Type: application/json" | grep '"id":' | sed 's/.*"id": "\(.*\)".*/\1/')

  if [[ -z "$FILE_ID" || "$FILE_ID" == "null" ]]; then
    echo "No file found with the given name and parent folder"
    FILE_ID=""
  else
    echo "File ID found: $FILE_ID"
  fi
}

# Function to upload a new file
upload_file() {
  local file_name="$1"
  local folder_id="$2"
  local build_path="$3"

  echo "Uploading file: $file_name ....."
  NEW_FILE_ID=$(curl -s -X POST \
    "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart" \
    -H "Authorization: Bearer $ACCESS_TOKEN" \
    -F "metadata={\"name\":\"$file_name\",\"parents\":[\"$folder_id\"]};type=application/json" \
    -F "file=@$build_path" | grep '"id":' | sed 's/.*"id": "\(.*\)".*/\1/')

  if [[ -z "$NEW_FILE_ID" || "$NEW_FILE_ID" == "null" ]]; then
    echo "Failed to upload file."
  else
    echo "New File ID: $NEW_FILE_ID"
  fi
}

# Function to update an existing file
update_file() {
  local file_name="$1"
  local file_id="$2"
  local folder_id="$3"
  local build_path="$4"

  echo "Updating existing file: $file_name ...."
  curl -s -X PATCH \
    "https://www.googleapis.com/upload/drive/v3/files/$file_id?uploadType=multipart&addParents=$folder_id&removeParents=$folder_id" \
    -H "Authorization: Bearer $ACCESS_TOKEN" \
    -F "metadata={\"name\":\"$file_name\"};type=application/json" \
    -F "file=@$build_path"

  NEW_FILE_ID="$FILE_ID"
}

# Update file ID in the release data file
update_file_id() {
  sed -i '' "s/\[file_id\]/$NEW_FILE_ID/" "$RELEASE_DATA_PATH"
}

# Function to upload or update release data
upload_or_update_release_data() {
  RELEASE_FILE_ID=""
  get_fileid "$RELEASE_DATA_FILE_NAME" "$PARENT_FOLDER_ID"
  RELEASE_FILE_ID="$FILE_ID"
  echo "RELEASE_FILE_ID: $RELEASE_FILE_ID"

  if [[ -z "$RELEASE_FILE_ID" ]]; then
    upload_file "$RELEASE_DATA_FILE_NAME" "$PARENT_FOLDER_ID" "$RELEASE_DATA_PATH"
  else
    update_file "$RELEASE_DATA_FILE_NAME" "$RELEASE_FILE_ID" "$PARENT_FOLDER_ID" "$RELEASE_DATA_PATH"
  fi
}

# Main execution
echo "Checking Existing File...."
get_fileid "$FILE_NAME" "$PARENT_FOLDER_ID"

echo "Build Uploading...."
if [[ -z "$FILE_ID" ]]; then
  upload_file "$FILE_NAME" "$PARENT_FOLDER_ID" "$BUILD_PATH"
else
  update_file "$FILE_NAME" "$FILE_ID" "$PARENT_FOLDER_ID" "$BUILD_PATH"
fi

echo "Build Uploaded. New File Id: $NEW_FILE_ID"
update_file_id
upload_or_update_release_data

echo "==============================Process Done========================================="