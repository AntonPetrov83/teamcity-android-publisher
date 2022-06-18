# teamcity-android-publisher
A TeamCity C# script to upload Android builds to Google Play.

## Usage

Script parameters:
* `--package-name` - a reverse domain name like `com.acme.example`;
* `--apk-file` - an APK file to upload;
* `--obb-file` - an OBB file to upload (optional);

The script requires a Google Service Account key passed via `GOOGLE_API_JSON` environment variable able to access [Google Play Developer API](https://developers.google.com/android-publisher).
